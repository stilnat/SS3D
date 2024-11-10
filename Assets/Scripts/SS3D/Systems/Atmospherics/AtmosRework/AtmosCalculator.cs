using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public static class AtmosCalculator
    {
        public static AtmosObject SimulateFlux(AtmosObject atmos, float dt,  AtmosObject[] neigbours)
        {
           // if (atmos.State == AtmosState.Active)
           // {
           //     atmos = SimulateFluxActive(atmos, dt, neigbours);
           // }

            if (atmos.State == AtmosState.Semiactive ||
                atmos.State == AtmosState.Active)
            {
                atmos = SimulateMixing(atmos, dt, neigbours);
                atmos = SimulateTemperature(atmos, dt, neigbours);
            }

            return atmos;
        }

        public static MoleTransferToNeighbours SimulateGasTransfers(AtmosObject atmos, float dt, AtmosObject[] neighbours,int atmosIndex, int[] neighboursIndexes)
        {
            float pressure = atmos.Container.GetPressure();
            int neighbourCount = neighbours.Length; 

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            // moles of each gaz from each neighbour to transfer.
            float4[] molesToTransfer = new float4[neighbourCount];

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            for (int i = 0; i < neighbourCount; i++)
            {
                molesToTransfer[i] = 0;

                if (neighbours[i].State == AtmosState.Blocked)
                    continue;

                float neighbourPressure = neighbours[i].Container.GetPressure();

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon)
                {
                    if (!atmos.temperatureSetting)
                        atmos.State = AtmosState.Semiactive;
                    else
                        atmos.temperatureSetting = false;
                    continue;
                }

                atmos.activeDirection[i] = true;

                // Use partial pressures to determine how much of each gas to move.
                float4 partialPressureDifference =  atmos.Container.GetAllPartialPressures() - neighbours[i].Container.GetAllPartialPressures();

                // Determine the amount of moles by applying the ideal gas law.
                molesToTransfer[i] = partialPressureDifference * 1000f * atmos.Container.GetVolume() /
                    (atmos.Container.GetTemperature() * GasConstants.gasConstant);

                molesToTransfer[i] *= GasConstants.simSpeed * dt;

                // We only care about what we transfer here, not what we receive
                molesToTransfer[i] = math.max(0f, molesToTransfer[i]);
            }


            // elements represent the total amount of gas to transfer for core gasses  
            float4 totalMolesToTransfer = 0;
             
            for (int i = 0; i < neighbourCount; i++)
            {
                totalMolesToTransfer += molesToTransfer[i];
            }


            float4 totalMolesInContainer = atmos.Container.GetCoreGasses();
            totalMolesInContainer += 0.000001f;

            // It's not immediately obvious what this does. If there's enough moles of the given gas in container, then the full amount of previously computed moles are transferred.
            // Otherwise, adapt the transferred amount so that it transfer to neighbours no more that the amount present in container.
            // it's written like that to avoid using conditions branching for Burst.
            for (int i = 0; i < neighbourCount; i++)
            {
                molesToTransfer[i] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            }

            /*for (int i = 0; i < neighbourCount; i++)
            {
                neighbourFlux[i] =  math.csum(molesToTransfer[i] * GasConstants.coreGasDensity);
                if (neighbours[i].State == AtmosState.Vacuum || neighbourFlux[i] <= 0)
                {
                    atmos.activeDirection[i] = false;
                }

                atmos.Container.ChangeCoreGasses(molesToTransfer[i]);
            } */

            // Finally, calculate the 2d wind vector based on neighbour flux.
            //float velHorizontal = neighbourFlux[3] - neighbourFlux[2];
            //float velVertical = neighbourFlux[0] - neighbourFlux[1];
            //atmos.Velocity.x = velHorizontal;
            //atmos.Velocity.y = velVertical;

            MoleTransferToNeighbours moleTransferToNeighbours = new();
            MoleTransfer[] moleTransfers = new MoleTransfer[4];
            for (int i = 0; i < neighbourCount; i++)
            {
                moleTransfers[i].Moles = molesToTransfer[i];
                moleTransfers[i].IndexTo = neighboursIndexes[i];
            }

            for (int i = neighbourCount; i < 4; i++)
            {
                moleTransfers[i].Moles = 0;
                moleTransfers[i].IndexTo = 0;
            }

            moleTransferToNeighbours.TransferOne = moleTransfers[0];
            moleTransferToNeighbours.TransferTwo = moleTransfers[1];
            moleTransferToNeighbours.TransferThree = moleTransfers[2];
            moleTransferToNeighbours.TransferFour = moleTransfers[3];
            moleTransferToNeighbours.IndexFrom = atmosIndex;
           

            return moleTransferToNeighbours;
        }

        private static AtmosObject SimulateMixing(AtmosObject atmos, float dt, AtmosObject[] neighbours)
        {
            bool mixed = false;
            int neighbourCount = neighbours.Length; 

            if (math.all(atmos.Container.GetCoreGasses() <= 0f))
            {
                return atmos;
            }

            for (int i = 0; i < neighbourCount; i++)
            {
                if (neighbours[i].State == AtmosState.Blocked || neighbours[i].State == AtmosState.Vacuum)
                {
                    continue;
                }

                float4 molesToTransfer = (atmos.Container.GetCoreGasses() - neighbours[i].Container.GetCoreGasses())
                    * GasConstants.gasDiffusionRate;

                molesToTransfer *= GasConstants.simSpeed * dt;

                if (math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)))
                {
                    molesToTransfer = math.max(molesToTransfer, 0);
                    neighbours[i].Container.AddCoreGasses(molesToTransfer);
                    atmos.Container.RemoveCoreGasses(molesToTransfer);
                    mixed = true;
                }

                // Remain active if there is still a pressure difference
                if (math.abs(neighbours[i].Container.GetPressure() - atmos.Container.GetPressure()) > GasConstants.pressureEpsilon)
                {
                    neighbours[i].State = AtmosState.Active;
                }
            }


            if (!mixed && atmos.State == AtmosState.Semiactive)
            {
                atmos.State = AtmosState.Inactive;
            }

            return atmos;
        }

        private static AtmosObject SimulateTemperature(AtmosObject atmos, float dt, AtmosObject[] neighbours)
        {
            float4 temperatureFlux = 0f;
            int neighbourCount = neighbours.Length; 
            for (int i = 0; i < neighbourCount; i++)
            {
                if (!atmos.activeDirection[i])
                {
                    continue;
                }

                float difference = atmos.Container.GetTemperature() - neighbours[i].Container.GetTemperature();

                if (difference <= GasConstants.thermalEpsilon)
                {
                    continue;
                }

                temperatureFlux[i] = (atmos.Container.GetTemperature() - neighbours[i].Container.GetTemperature()) *
                    GasConstants.thermalBase * atmos.Container.GetVolume();

                // Set neighbour
                neighbours[i].Container.SetTemperature(neighbours[i].Container.GetTemperature() + temperatureFlux[i]);

                // Set self
                atmos.Container.SetTemperature(atmos.Container.GetTemperature() - temperatureFlux[i]);
                atmos.temperatureSetting = true;
            }   
            
            return atmos;
        }
    }
}