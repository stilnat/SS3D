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
        public Vector2 velocity;
        public int bufferIndex;
    }

    public struct AtmosObject
    {
        public AtmosObjectInfo atmosObject;
        public AtmosObjectInfo neighbour1;
        public AtmosObjectInfo neighbour2;
        public AtmosObjectInfo neighbour3;
        public AtmosObjectInfo neighbour4;

        public bool temperatureSetting;
        public bool4 activeDirection;

        public void Setup()
        {
            atmosObject.container = new AtmosContainer();
            atmosObject.container.Setup();

            for (int i = 0; i < 4; i++)
            {
                AtmosObjectInfo info = new AtmosObjectInfo
                {
                    bufferIndex = -1,
                    container = new AtmosContainer(),
                    state = AtmosState.Blocked
                };

                info.container.Setup();
                SetNeighbour(info, i);
            }
        }

        /// Testing
        public float GetTotalGasInNeighbours()
        {
            float gasAmount = 0f;
            gasAmount += math.csum(atmosObject.container.GetCoreGasses());
            gasAmount += math.csum(neighbour1.container.GetCoreGasses());
            gasAmount += math.csum(neighbour2.container.GetCoreGasses());
            gasAmount += math.csum(neighbour3.container.GetCoreGasses());
            gasAmount += math.csum(neighbour4.container.GetCoreGasses());

            return gasAmount;
        }

        public AtmosObjectInfo GetNeighbour(int index)
        {
            switch (index)
            {
                case 0:
                    return neighbour1;
                case 1:
                    return neighbour2;
                case 2:
                    return neighbour3;
                case 3:
                    return neighbour4;
            }

            return default;
        }

        public int GetNeighbourIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return neighbour1.bufferIndex;
                case 1:
                    return neighbour2.bufferIndex;
                case 2:
                    return neighbour3.bufferIndex;
                case 3:
                    return neighbour4.bufferIndex;
            }

            return default;
        }

        public void SetNeighbourIndex(int index, int bufferIndex)
        {
            switch (index)
            {
                case 0:
                    neighbour1.bufferIndex = bufferIndex;
                    break;
                case 1:
                    neighbour2.bufferIndex = bufferIndex;
                    break;
                case 2:
                    neighbour3.bufferIndex = bufferIndex;
                    break;
                case 3:
                    neighbour4.bufferIndex = bufferIndex;
                    break;
            }
        }

        public void SetNeighbour(AtmosObjectInfo info, int index)
        {
            switch (index)
            {
                case 0:
                    neighbour1 = info;
                    break;
                case 1:
                    neighbour2 = info;
                    break;
                case 2:
                    neighbour3 = info;
                    break;
                case 3:
                    neighbour4 = info;
                    break;
            }
        }

        public void AddGas(CoreAtmosGasses gas, float amount)
        {
            atmosObject.container.AddCoreGas(gas, amount);
            atmosObject.state = AtmosState.Active;
        }

        public void RemoveGas(CoreAtmosGasses gas, float amount)
        {
            atmosObject.container.RemoveCoreGas(gas, amount);
            atmosObject.state = AtmosState.Active;
        }

        public void AddHeat(float amount)
        {
            atmosObject.container.SetTemperature(amount); // TODO change back to heat when a proper setting is found.
            atmosObject.state = AtmosState.Active;
        }

        public void RemoveHeat(float amount)
        {
            atmosObject.container.RemoveHeat(amount);
            atmosObject.state = AtmosState.Active;
        }


        public bool IsEmpty()
        {
            return atmosObject.container.IsEmpty();
        }

        public bool IsAir()
        {
            return atmosObject.container.IsAir();
        }

        public override string ToString()
        {
            string text = $"State: {atmosObject.state}, Pressure: {atmosObject.container.GetPressure()}, Gasses: {atmosObject.container.GetCoreGasses()}, RealPressure: {atmosObject.container.GetRealPressure()}\n";
            text += $"Temperature: {atmosObject.container.GetTemperature()} Kelvin, Compressibility factor: {atmosObject.container.GetCompressionFactor()}\n";
            for (int i = 0; i < 8; i++)
            {
                AtmosObjectInfo info = GetNeighbour(i);
                text += $"neighbour{i}: State: {info.state}, Pressure: {info.container.GetPressure()}, Temperature: {info.container.GetTemperature()}\n";
            }

            return text;
        }
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
            float pressure = 0f;

            // Holds the weight of gas that passes to the neighbours. Used for calculating wind strength.
            float4 neighbourFlux = 0f;

            if (GasConstants.useRealisticGasLaw)
                pressure = atmos.atmosObject.container.GetRealPressure();
            else
                pressure = atmos.atmosObject.container.GetPressure();

            for (int i = 0; i < 4; i++)
            {
                if (atmos.GetNeighbour(i).state == AtmosState.Blocked)
                    continue;

                float neighbourPressure = 0;
                if (GasConstants.useRealisticGasLaw)
                    neighbourPressure = atmos.GetNeighbour(i).container.GetRealPressure();
                else
                    neighbourPressure = atmos.GetNeighbour(i).container.GetPressure();

                if ((pressure - neighbourPressure) > GasConstants.pressureEpsilon)
                {
                    atmos.activeDirection[i] = true;

                    // Use partial pressures to determine how much of each gas to move.
                    float4 partialPressureDifference = 0f;
                    if (GasConstants.useRealisticGasLaw)
                        partialPressureDifference = atmos.atmosObject.container.GetAllRealPartialPressures() - atmos.GetNeighbour(i).container.GetAllRealPartialPressures();
                    else
                        partialPressureDifference = atmos.atmosObject.container.GetAllPartialPressures() - atmos.GetNeighbour(i).container.GetAllPartialPressures();

                    // Determine the amount of moles by applying the ideal gas law.
                    float4 molesToTransfer = 0f;
                    if (GasConstants.useRealisticGasLaw)
                        molesToTransfer = partialPressureDifference * 1000f * atmos.atmosObject.container.GetRealVolume() /
                        (atmos.atmosObject.container.GetTemperature() * GasConstants.gasConstant);
                    else
                        molesToTransfer = partialPressureDifference * 1000f * atmos.atmosObject.container.GetVolume() /
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

                    if ((pressure - neighbourPressure) > GasConstants.pressureEpsilon && math.any(molesToTransfer > 0f))
                    {
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
                }
                else
                {
                    if (!atmos.temperatureSetting)
                        atmos.atmosObject.state = AtmosState.Semiactive;
                    else
                        atmos.temperatureSetting = false;
                }
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
            if (math.any(atmos.atmosObject.container.GetCoreGasses() > 0f))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (atmos.GetNeighbour(i).state != AtmosState.Blocked && atmos.GetNeighbour(i).state != AtmosState.Vacuum)
                    {
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
                        if (GasConstants.useRealisticGasLaw)
                        {
                            if (math.abs(neighbour.container.GetRealPressure() - atmos.atmosObject.container.GetRealPressure()) > GasConstants.pressureEpsilon
                                )
                            {
                                neighbour.state = AtmosState.Active;
                            }
                        }
                        else
                        {
                            if (math.abs(neighbour.container.GetPressure() - atmos.atmosObject.container.GetPressure()) > GasConstants.pressureEpsilon
                                )
                            {
                                neighbour.state = AtmosState.Active;
                            }
                        }

                        atmos.SetNeighbour(neighbour, i);
                    }
                }
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
                if (atmos.activeDirection[i] == true)
                {
                    float difference = (atmos.atmosObject.container.GetTemperature() - atmos.GetNeighbour(i).container.GetTemperature());
                    temperatureFlux[i] = 0f;
                    if (GasConstants.useRealisticGasLaw)
                    {
                        temperatureFlux[i] = (atmos.atmosObject.container.GetTemperature() - atmos.GetNeighbour(i).container.GetTemperature()) *
                        GasConstants.thermalBase * atmos.atmosObject.container.GetRealVolume() * dt;
                    }
                    else
                    {
                        temperatureFlux[i] = (atmos.atmosObject.container.GetTemperature() - atmos.GetNeighbour(i).container.GetTemperature()) *
                        GasConstants.thermalBase * atmos.atmosObject.container.GetVolume();
                    }

                    if (difference > GasConstants.thermalEpsilon)
                    {
                        // Set neighbour
                        AtmosObjectInfo neighbour = atmos.GetNeighbour(i);
                        // neighbour.container.AddHeat(temperatureFlux[i]);
                        neighbour.container.SetTemperature(neighbour.container.GetTemperature() + temperatureFlux[i]);
                        atmos.SetNeighbour(neighbour, i);

                        // Set self
                        // atmos.atmosObject.container.RemoveHeat(temperatureFlux[i]);
                        atmos.atmosObject.container.SetTemperature(atmos.atmosObject.container.GetTemperature() - temperatureFlux[i]);
                        atmos.temperatureSetting = true;
                    }
                }
            }
            
            return atmos;
        }
    }
}