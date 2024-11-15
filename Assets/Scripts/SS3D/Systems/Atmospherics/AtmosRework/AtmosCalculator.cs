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
                //atmos = SimulateMixing(atmos, dt, neigbours);
                atmos = SimulateTemperature(atmos, dt, neigbours);
            }

            return atmos;
        }

        public static MoleTransferToNeighbours SimulateGasTransfers(int atmosIndex, int northIndex, int southIndex,
            int eastIndex, int westIndex, NativeArray<AtmosObject> tileObjectBuffer, float dt, bool activeFlux)
        {
            AtmosObject atmos = tileObjectBuffer[atmosIndex];
            
            // moles of each gaz from each neighbour to transfer.
            float4x4 molesToTransfer = 0;

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            molesToTransfer[0] = MolesToTransfer(tileObjectBuffer, northIndex, ref atmos, activeFlux, dt, atmos.VelocityNorth, tileObjectBuffer[northIndex].VelocitySouth);
            molesToTransfer[1] = MolesToTransfer(tileObjectBuffer, southIndex, ref atmos, activeFlux, dt, atmos.VelocitySouth, tileObjectBuffer[northIndex].VelocityNorth);
            molesToTransfer[2] = MolesToTransfer(tileObjectBuffer, eastIndex, ref atmos, activeFlux, dt, atmos.VelocityEast, tileObjectBuffer[northIndex].VelocityWest);
            molesToTransfer[3] = MolesToTransfer(tileObjectBuffer, westIndex, ref atmos, activeFlux, dt, atmos.VelocityWest, tileObjectBuffer[northIndex].VelocityEast);


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

            MoleTransferToNeighbours moleTransferToNeighbours = new(
                atmosIndex, molesToTransfer[0], molesToTransfer[1], molesToTransfer[2],molesToTransfer[3]);

            return moleTransferToNeighbours;
        }

        private static float4 MolesToTransfer(NativeArray<AtmosObject> tileObjectBuffer, int neighbourIndex,
            ref AtmosObject atmos, bool activeFlux, float dt, float atmosVelocity, float oppositeVelocity)
        {
            float4 molesToTransfer = 0;

            if (neighbourIndex == -1)
            {
                return molesToTransfer;
            }
            
            AtmosObject neighbour = tileObjectBuffer[neighbourIndex];
            
            if (neighbour.State == AtmosState.Blocked)
            {
                return molesToTransfer;
            }

            molesToTransfer = activeFlux ? ComputeActiveFluxMoles(ref atmos, neighbour, atmosVelocity, oppositeVelocity) : ComputeDiffusionMoles(ref atmos, neighbour);

            molesToTransfer *= GasConstants.simSpeed * dt;

            // We only care about what we transfer here, not what we receive
            molesToTransfer = math.max(0f, molesToTransfer);

            return molesToTransfer;
        }

        private static float4 ComputeActiveFluxMoles(ref AtmosObject atmos, AtmosObject neighbour, float atmosVelocity, float oppositeVelocity)
        {
            float neighbourPressure = neighbour.Pressure;

            if (math.abs(atmos.Pressure - neighbourPressure) <= GasConstants.pressureEpsilon)
            {
                return new(0);
            }

            // Use partial pressures to determine how much of each gas to move.
            float4 partialPressureDifference = atmos.GetAllPartialPressures() - neighbour.GetAllPartialPressures();

            // Determine the amount of moles by applying the ideal gas law and taking wind into account.
            return (1 + (0.1f * math.max(0f, atmosVelocity - oppositeVelocity))) * partialPressureDifference * 1000f * atmos.GetVolume() /
                (atmos.Temperature * GasConstants.gasConstant);
        }

        private static float4 ComputeDiffusionMoles(ref AtmosObject atmos, AtmosObject neighbour)
        {
            float4 molesToTransfer = (atmos.CoreGasses - neighbour.CoreGasses) * GasConstants.gasDiffusionRate;
            if (math.any(molesToTransfer > GasConstants.fluxEpsilon))
            {
                return molesToTransfer;
            }

            return new(0);
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