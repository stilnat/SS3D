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
        
        public static MoleTransferToNeighbours SimulateGasTransfers(AtmosObject atmos, int atmosIndex, AtmosObject northNeighbour, AtmosObject southNeighbour,
            AtmosObject eastNeighbour, AtmosObject westNeighbour, float dt, bool activeFlux, bool4 hasNeighbour)
        {
            
            // moles of each gaz from each neighbour to transfer.
            float4x4 molesToTransfer = 0;

            float4 enteringVelocity = float4.zero;
            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            if (hasNeighbour[0])
            {
                enteringVelocity = hasNeighbour[1] ? southNeighbour.VelocityNorth : 0;
                molesToTransfer[0] = MolesToTransfer(northNeighbour, atmos, activeFlux, dt, enteringVelocity, northNeighbour.VelocitySouth);
            }
            if (hasNeighbour[1])
            {
                enteringVelocity = hasNeighbour[0] ? northNeighbour.VelocitySouth : 0;
                molesToTransfer[1] = MolesToTransfer(southNeighbour,  atmos, activeFlux, dt, enteringVelocity, southNeighbour.VelocityNorth);
            }
            if (hasNeighbour[2])
            {
                enteringVelocity = hasNeighbour[3] ? westNeighbour.VelocityEast : 0;
                molesToTransfer[2] = MolesToTransfer(eastNeighbour, atmos, activeFlux, dt, enteringVelocity, eastNeighbour.VelocityWest);
            }
            if (hasNeighbour[3])
            {
                enteringVelocity = hasNeighbour[2] ? eastNeighbour.VelocityWest : 0;
                molesToTransfer[3] = MolesToTransfer(westNeighbour, atmos, activeFlux, dt, enteringVelocity, westNeighbour.VelocityEast);
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

            MoleTransferToNeighbours moleTransferToNeighbours = new(
                atmosIndex, molesToTransfer[0], molesToTransfer[1], molesToTransfer[2],molesToTransfer[3]);

            return moleTransferToNeighbours;
        }

        public static float4 MolesToTransfer(AtmosObject neighbour,
            AtmosObject atmos, bool activeFlux, float dt, float4 enteringVelocity, float4 neighbourOppositeVelocity)
        {
            float4 molesToTransfer = 0;
            
            if (neighbour.State == AtmosState.Blocked)
            {
                return molesToTransfer;
            }

            molesToTransfer = activeFlux ? ComputeActiveFluxMoles(atmos, neighbour, enteringVelocity, neighbourOppositeVelocity) : ComputeDiffusionMoles(ref atmos, neighbour);

            molesToTransfer *= dt * GasConstants.simSpeed;

            // We only care about what we transfer here, not what we receive
            molesToTransfer = math.max(0f, molesToTransfer);

            return molesToTransfer;
        }

        private static float4 ComputeActiveFluxMoles(AtmosObject atmos, AtmosObject neighbour, float4 enteringVelocity, float4 neighbourOppositeVelocity)
        {
            float neighbourPressure = neighbour.Pressure;
            float absolutePressureDif = math.abs(atmos.Pressure - neighbourPressure);

            // when adjacent to an almost empty neighbour and atmos being itself almost empty, don't transfer.
            if (absolutePressureDif <= GasConstants.pressureEpsilon && neighbourPressure <= GasConstants.pressureEpsilon)
            {
                return new(0);
            }
            
            if (absolutePressureDif <= GasConstants.fluxEpsilon)
            {
                return new(0);
            }
            
            // TODO : currently does transfer even with extremely small pressure differences.
            // We can't just say to stop transfer under a given pressure difference with neighbours because it creates "pressure plugs".
            // Instead, under a given pressure difference it should just fully equalize pressure.

            // Use partial pressures to determine how much of each gas to move.
            float4 partialPressureDifference = atmos.GetAllPartialPressures() - neighbour.GetAllPartialPressures();

            // Determine the amount of moles to transfer.
            return partialPressureDifference * 5;
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