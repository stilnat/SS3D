using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public static class AtmosCalculator
    {
        public static AtmosObject SimulateFlux(AtmosObject atmos, float dt)
        {
            if (atmos.atmosObject.State == AtmosState.Active)
            {
                atmos = SimulateFluxActive(atmos, dt);
            }

            if (atmos.atmosObject.State == AtmosState.Semiactive ||
                atmos.atmosObject.State == AtmosState.Active)
            {
                atmos = SimulateMixing(atmos, dt);
                atmos = SimulateTemperature(atmos, dt);
            }

            return atmos;
        }

        private static AtmosObject SimulateFluxActive(AtmosObject atmos, float dt)
        {
            float pressure = atmos.atmosObject.Container.GetPressure();

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            float4[] molesToTransfer = new float4[4];

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            for (int i = 0; i < 4; i++)
            {
                molesToTransfer[i] = 0;

                if (atmos.GetNeighbour(i).State == AtmosState.Blocked)
                    continue;

                float neighbourPressure = atmos.GetNeighbour(i).Container.GetPressure();

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon)
                {
                    if (!atmos.temperatureSetting)
                        atmos.atmosObject.State = AtmosState.Semiactive;
                    else
                        atmos.temperatureSetting = false;
                    continue;
                }

                atmos.activeDirection[i] = true;

                // Use partial pressures to determine how much of each gas to move.
                float4 partialPressureDifference =  atmos.atmosObject.Container.GetAllPartialPressures() - atmos.GetNeighbour(i).Container.GetAllPartialPressures();

                // Determine the amount of moles by applying the ideal gas law.
                molesToTransfer[i] = partialPressureDifference * 1000f * atmos.atmosObject.Container.GetVolume() /
                    (atmos.atmosObject.Container.GetTemperature() * GasConstants.gasConstant);

                // Can't transfer negative amounts of moles
                molesToTransfer[i] = math.max(molesToTransfer[i], 0f);

                // Cannot transfer all moles at once
                molesToTransfer[i] *= GasConstants.simSpeed * dt;
            }

            // elements represent the total amount of gas to transfer for core gasses                                   
            float4 totalMolesToTransfer = molesToTransfer[0] +  molesToTransfer[1] + molesToTransfer[2] + molesToTransfer[3];

            float4 totalMolesInContainer = atmos.atmosObject.Container.GetCoreGasses();
            totalMolesInContainer += 0.000001f;
            

            // It's not immediatly obvious what this does. If there's enough moles of the given gas in container, then the full amount of previously computed moles are transferred.
            // Otherwise, adapt the transferred amount so that it transfer to neighbours no more that the amount present in container.
            // it's written like that to avoid using conditions branching for Burst.
            for (int i = 0; i < 4; i++)
            {
                molesToTransfer[i] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            }

            for (int i = 0; i < 4; i++)
            {
                neighbourFlux[i] =  math.csum(molesToTransfer[i] * GasConstants.coreGasDensity);
                if (atmos.GetNeighbour(i).State != AtmosState.Vacuum && neighbourFlux[i] > 0)
                {
                    AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                    neighbour.Container.AddCoreGasses(molesToTransfer[i]);
                    neighbour.State = AtmosState.Active;
                    atmos.SetNeighbour(neighbour, i);
                }
                else
                {
                    atmos.activeDirection[i] = false;
                }

                atmos.atmosObject.Container.RemoveCoreGasses(molesToTransfer[i]);
            }

            // Finally, calculate the 2d wind vector based on neighbour flux.
            float velHorizontal = neighbourFlux[3] - neighbourFlux[2];
            float velVertical = neighbourFlux[0] - neighbourFlux[1];
            atmos.atmosObject.Velocity.x = velHorizontal;
            atmos.atmosObject.Velocity.y = velVertical;


            return atmos;
        }

        private static AtmosObject SimulateMixing(AtmosObject atmos, float dt)
        {
            bool mixed = false;

            if (math.all(atmos.atmosObject.Container.GetCoreGasses() <= 0f))
            {
                return atmos;
            }

            for (int i = 0; i < 4; i++)
            {
                if (atmos.GetNeighbour(i).State == AtmosState.Blocked || atmos.GetNeighbour(i).State == AtmosState.Vacuum)
                {
                    continue;
                }

                AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                float4 molesToTransfer = (atmos.atmosObject.Container.GetCoreGasses() - atmos.GetNeighbour(i).Container.GetCoreGasses())
                    * GasConstants.gasDiffusionRate;

                molesToTransfer *= GasConstants.simSpeed * dt;

                if (math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)))
                {
                    molesToTransfer = math.max(molesToTransfer, 0);
                    neighbour.Container.AddCoreGasses(molesToTransfer);
                    atmos.atmosObject.Container.RemoveCoreGasses(molesToTransfer);
                    mixed = true;
                }

                // Remain active if there is still a pressure difference
                if (math.abs(neighbour.Container.GetPressure() - atmos.atmosObject.Container.GetPressure()) > GasConstants.pressureEpsilon)
                {
                    neighbour.State = AtmosState.Active;
                }

                atmos.SetNeighbour(neighbour, i);
            }


            if (!mixed && atmos.atmosObject.State == AtmosState.Semiactive)
            {
                atmos.atmosObject.State = AtmosState.Inactive;
            }

            return atmos;
        }

        private static AtmosObject SimulateTemperature(AtmosObject atmos, float dt)
        {
            float4 temperatureFlux = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (!atmos.activeDirection[i])
                {
                    continue;
                }

                float difference = atmos.atmosObject.Container.GetTemperature() - atmos.GetNeighbour(i).Container.GetTemperature();

                if (difference <= GasConstants.thermalEpsilon)
                {
                    continue;
                }

                temperatureFlux[i] = (atmos.atmosObject.Container.GetTemperature() - atmos.GetNeighbour(i).Container.GetTemperature()) *
                    GasConstants.thermalBase * atmos.atmosObject.Container.GetVolume();

                // Set neighbour
                AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                neighbour.Container.SetTemperature(neighbour.Container.GetTemperature() + temperatureFlux[i]);
                atmos.SetNeighbour(neighbour, i);

                // Set self
                atmos.atmosObject.Container.SetTemperature(atmos.atmosObject.Container.GetTemperature() - temperatureFlux[i]);
                atmos.temperatureSetting = true;
            }   
            
            return atmos;
        }
    }
}