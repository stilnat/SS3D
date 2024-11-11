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
            float pressure = atmos.Pressure;

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            // moles of each gaz from each neighbour to transfer.
            float4x4 molesToTransfer = 0;

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            for (int i = 0; i < neighbourCount; i++)
            {
                if (neighbours[i].State == AtmosState.Blocked)
                    continue;

                float neighbourPressure = neighbours[i].Pressure;

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon)
                {
                    if (!atmos.TemperatureSetting)
                        atmos.State = AtmosState.Semiactive;
                    else
                        atmos.TemperatureSetting = false;
                    continue;
                }

                atmos.ActiveDirection[i] = true;

                // Use partial pressures to determine how much of each gas to move.
                float4 partialPressureDifference =  atmos.GetAllPartialPressures() - neighbours[i].GetAllPartialPressures();

                // Determine the amount of moles by applying the ideal gas law.
                molesToTransfer[i] = partialPressureDifference * 1000f * atmos.GetVolume() /
                    (atmos.Temperature * GasConstants.gasConstant);

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
            


            float4 totalMolesInContainer = atmos.CoreGasses;
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

            if (math.all(atmos.CoreGasses <= 0f))
            {
                return atmos;
            }

            for (int i = 0; i < neighbourCount; i++)
            {
                if (neighbours[i].State == AtmosState.Blocked || neighbours[i].State == AtmosState.Vacuum)
                {
                    continue;
                }

                float4 molesToTransfer = (atmos.CoreGasses - neighbours[i].CoreGasses)
                    * GasConstants.gasDiffusionRate;

                molesToTransfer *= GasConstants.simSpeed * dt;

                if (math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)))
                {
                    molesToTransfer = math.max(molesToTransfer, 0);
                    neighbours[i].AddCoreGasses(molesToTransfer, false);
                    atmos.RemoveCoreGasses(molesToTransfer, false);
                    mixed = true;
                }

                // Remain active if there is still a pressure difference
                if (math.abs(neighbours[i].Pressure) - atmos.Pressure > GasConstants.pressureEpsilon)
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
                if (!atmos.ActiveDirection[i])
                {
                    continue;
                }

                float difference = atmos.Temperature - neighbours[i].Temperature;

                if (difference <= GasConstants.thermalEpsilon)
                {
                    continue;
                }

                temperatureFlux[i] = (atmos.Temperature - neighbours[i].Temperature) *
                    GasConstants.thermalBase * atmos.Volume;

                // Set neighbour
                neighbours[i].SetTemperature(neighbours[i].Temperature + temperatureFlux[i]);

                // Set self
                atmos.SetTemperature(atmos.Temperature - temperatureFlux[i]);
                atmos.TemperatureSetting = true;
            }   
            
            return atmos;
        }
    }
}