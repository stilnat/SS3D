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

            for (int i = 0; i < 4; i++)
            {
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
                float4 molesToTransfer = partialPressureDifference * 1000f * atmos.atmosObject.Container.GetVolume() /
                    (atmos.atmosObject.Container.GetTemperature() * GasConstants.gasConstant);

                // Cannot transfer all moles at once
                molesToTransfer *= GasConstants.simSpeed * dt;

                // Cannot transfer more gasses then there are and no one below zero.
                molesToTransfer = math.clamp(molesToTransfer, 0, atmos.atmosObject.Container.GetCoreGasses());

                // Calculate wind velocity
                neighbourFlux[i] = math.csum(molesToTransfer * GasConstants.coreGasDensity);

                // We need to pass the minimum threshold
                //if ((math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)) || (pressure - neighbourPressure) > GasConstants.pressureEpsilon)
                //    && math.any(molesToTransfer > 0f))

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon || math.all(molesToTransfer <= 0f))
                    continue;

                if (atmos.GetNeighbour(i).State != AtmosState.Vacuum)
                {
                    AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                    neighbour.Container.AddCoreGasses(molesToTransfer);
                    neighbour.State = AtmosState.Active;
                    atmos.SetNeighbour(neighbour, i);
                }
                else
                {
                    atmos.activeDirection[i] = false;
                }

                atmos.atmosObject.Container.RemoveCoreGasses(molesToTransfer);
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