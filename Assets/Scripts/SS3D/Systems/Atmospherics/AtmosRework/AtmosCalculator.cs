using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct AtmosObjectInfo
    {
        public AtmosState state;
        public AtmosContainer container;
        public float2 velocity;
        public int bufferIndex;
    }

   

    public static class AtmosCalculator
    {
        public static AtmosObject SimulateFlux(AtmosObject atmos, float dt)
        {
            if (atmos.atmosObject.state == AtmosState.Active)
            {
                atmos = SimulateFluxActive(atmos, dt);
            }

            if (atmos.atmosObject.state == AtmosState.Semiactive ||
                atmos.atmosObject.state == AtmosState.Active)
            {
                atmos = SimulateMixing(atmos, dt);
                atmos = SimulateTemperature(atmos, dt);
            }

            return atmos;
        }

        private static AtmosObject SimulateFluxActive(AtmosObject atmos, float dt)
        {
            float pressure = atmos.atmosObject.container.GetPressure();

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            for (int i = 0; i < 4; i++)
            {
                if (atmos.GetNeighbour(i).state == AtmosState.Blocked)
                    continue;

                float neighbourPressure = atmos.GetNeighbour(i).container.GetPressure();

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon)
                {
                    if (!atmos.temperatureSetting)
                        atmos.atmosObject.state = AtmosState.Semiactive;
                    else
                        atmos.temperatureSetting = false;
                    continue;
                }

                atmos.activeDirection[i] = true;

                // Use partial pressures to determine how much of each gas to move.
                float4 partialPressureDifference =  atmos.atmosObject.container.GetAllPartialPressures() - atmos.GetNeighbour(i).container.GetAllPartialPressures();

                // Determine the amount of moles by applying the ideal gas law.
                float4 molesToTransfer = partialPressureDifference * 1000f * atmos.atmosObject.container.GetVolume() /
                    (atmos.atmosObject.container.GetTemperature() * GasConstants.gasConstant);

                // Cannot transfer all moles at once
                molesToTransfer *= GasConstants.simSpeed * dt;

                // Cannot transfer more gasses then there are and no one below zero.
                molesToTransfer = math.clamp(molesToTransfer, 0, atmos.atmosObject.container.GetCoreGasses());

                // Calculate wind velocity
                neighbourFlux[i] = math.csum(molesToTransfer * GasConstants.coreGasDensity);

                // We need to pass the minimum threshold
                //if ((math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)) || (pressure - neighbourPressure) > GasConstants.pressureEpsilon)
                //    && math.any(molesToTransfer > 0f))

                if (pressure - neighbourPressure <= GasConstants.pressureEpsilon || math.all(molesToTransfer <= 0f))
                    continue;

                if (atmos.GetNeighbour(i).state != AtmosState.Vacuum)
                {
                    AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                    neighbour.container.AddCoreGasses(molesToTransfer);
                    neighbour.state = AtmosState.Active;
                    atmos.SetNeighbour(neighbour, i);
                }
                else
                {
                    atmos.activeDirection[i] = false;
                }

                atmos.atmosObject.container.RemoveCoreGasses(molesToTransfer);
            }

            // Finally, calculate the 2d wind vector based on neighbour flux.
            float velHorizontal = neighbourFlux[3] - neighbourFlux[2];
            float velVertical = neighbourFlux[0] - neighbourFlux[1];
            atmos.atmosObject.velocity.x = velHorizontal;
            atmos.atmosObject.velocity.y = velVertical;


            return atmos;
        }

        private static AtmosObject SimulateMixing(AtmosObject atmos, float dt)
        {
            bool mixed = false;

            if (math.all(atmos.atmosObject.container.GetCoreGasses() <= 0f))
            {
                return atmos;
            }

            for (int i = 0; i < 4; i++)
            {
                if (atmos.GetNeighbour(i).state == AtmosState.Blocked || atmos.GetNeighbour(i).state == AtmosState.Vacuum)
                {
                    continue;
                }

                AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                float4 molesToTransfer = (atmos.atmosObject.container.GetCoreGasses() - atmos.GetNeighbour(i).container.GetCoreGasses())
                    * GasConstants.gasDiffusionRate;

                molesToTransfer *= GasConstants.simSpeed * dt;

                if (math.any(molesToTransfer > (GasConstants.fluxEpsilon * GasConstants.simSpeed * dt)))
                {
                    molesToTransfer = math.max(molesToTransfer, 0);
                    neighbour.container.AddCoreGasses(molesToTransfer);
                    atmos.atmosObject.container.RemoveCoreGasses(molesToTransfer);
                    mixed = true;
                }

                // Remain active if there is still a pressure difference
                if (math.abs(neighbour.container.GetPressure() - atmos.atmosObject.container.GetPressure()) > GasConstants.pressureEpsilon)
                {
                    neighbour.state = AtmosState.Active;
                }

                atmos.SetNeighbour(neighbour, i);
            }


            if (!mixed && atmos.atmosObject.state == AtmosState.Semiactive)
            {
                atmos.atmosObject.state = AtmosState.Inactive;
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

                float difference = atmos.atmosObject.container.GetTemperature() - atmos.GetNeighbour(i).container.GetTemperature();

                if (difference <= GasConstants.thermalEpsilon)
                {
                    continue;
                }

                temperatureFlux[i] = (atmos.atmosObject.container.GetTemperature() - atmos.GetNeighbour(i).container.GetTemperature()) *
                    GasConstants.thermalBase * atmos.atmosObject.container.GetVolume();

                // Set neighbour
                AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                neighbour.container.SetTemperature(neighbour.container.GetTemperature() + temperatureFlux[i]);
                atmos.SetNeighbour(neighbour, i);

                // Set self
                atmos.atmosObject.container.SetTemperature(atmos.atmosObject.container.GetTemperature() - temperatureFlux[i]);
                atmos.temperatureSetting = true;
            }   
            
            return atmos;
        }
    }
}