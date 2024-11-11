using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

        public static MoleTransferToNeighbours SimulateGasTransfers(AtmosObject atmos, float dt, NativeArray<AtmosObject> neighbours,int atmosIndex, NativeArray<int> neighboursIndexes, int neighbourCount)
        {
            float pressure = atmos.Container.GetPressure();

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            // moles of each gaz from each neighbour to transfer.
            float4x4 molesToTransfer = 0;

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            for (int i = 0; i < neighbourCount; i++)
            {
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
             
            // unrolling for loop for vectorization
            totalMolesToTransfer += molesToTransfer[0];
            totalMolesToTransfer += molesToTransfer[1];
            totalMolesToTransfer += molesToTransfer[2];
            totalMolesToTransfer += molesToTransfer[3];
            


            float4 totalMolesInContainer = atmos.Container.GetCoreGasses();
            totalMolesInContainer += 0.000001f;

            // It's not immediately obvious what this does. If there's enough moles of the given gas in container, then the full amount of previously computed moles are transferred.
            // Otherwise, adapt the transferred amount so that it transfer to neighbours no more that the amount present in container.
            // it's written like that to avoid using conditions branching for Burst.
            // It works okay even if a neighbour is not present as the amount of moles to transfer will be zero.
            molesToTransfer[0] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[1] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[2] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[3] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);


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

            
            NativeArray<MoleTransfer> moleTransfers = new(4, Allocator.Temp);

            for (int i = 0; i < neighbourCount; i++)
            {
                moleTransfers[i] = new (molesToTransfer[i], neighboursIndexes[i]);
            }

            MoleTransferToNeighbours moleTransferToNeighbours = new(atmosIndex, moleTransfers[0], moleTransfers[1],moleTransfers[2],moleTransfers[3]);

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