
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;

namespace SS3D.Engine.AtmosphericsRework
{
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
            atmosObject.container = new();
            atmosObject.container.Setup();

            for (int i = 0; i < 4; i++)
            {
                AtmosObjectInfo info = new()
                {
                    bufferIndex = -1,
                    container = new(),
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
            string text = $"State: {atmosObject.state}, Pressure: {atmosObject.container.GetPressure()}, Gasses: {atmosObject.container.GetCoreGasses()}\n";
            text += $"Temperature: {atmosObject.container.GetTemperature()} Kelvin, Compressibility factor: {atmosObject.container.GetCompressionFactor()}\n";
            for (int i = 0; i < 8; i++)
            {
                AtmosObjectInfo info = GetNeighbour(i);
                text += $"neighbour{i}: State: {info.state}, Pressure: {info.container.GetPressure()}, Temperature: {info.container.GetTemperature()}\n";
            }

            return text;
        }
    }
}