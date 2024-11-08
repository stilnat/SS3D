
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
            atmosObject.Container = new();
            atmosObject.Container.Setup();

            for (int i = 0; i < 4; i++)
            {
                AtmosObjectInfo info = new()
                {
                    BufferIndex = -1,
                    Container = new(),
                    State = AtmosState.Blocked
                };

                info.Container.Setup();
                SetNeighbour(info, i);
            }
        }

        /// Testing
        public float GetTotalGasInNeighbours()
        {
            float gasAmount = 0f;
            gasAmount += math.csum(atmosObject.Container.GetCoreGasses());
            gasAmount += math.csum(neighbour1.Container.GetCoreGasses());
            gasAmount += math.csum(neighbour2.Container.GetCoreGasses());
            gasAmount += math.csum(neighbour3.Container.GetCoreGasses());
            gasAmount += math.csum(neighbour4.Container.GetCoreGasses());

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
                    return neighbour1.BufferIndex;
                case 1:
                    return neighbour2.BufferIndex;
                case 2:
                    return neighbour3.BufferIndex;
                case 3:
                    return neighbour4.BufferIndex;
            }

            return default;
        }

        public void SetNeighbourIndex(int index, int bufferIndex)
        {
            switch (index)
            {
                case 0:
                    neighbour1.BufferIndex = bufferIndex;
                    break;
                case 1:
                    neighbour2.BufferIndex = bufferIndex;
                    break;
                case 2:
                    neighbour3.BufferIndex = bufferIndex;
                    break;
                case 3:
                    neighbour4.BufferIndex = bufferIndex;
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
            atmosObject.Container.AddCoreGas(gas, amount);
            atmosObject.State = AtmosState.Active;
        }

        public void RemoveGas(CoreAtmosGasses gas, float amount)
        {
            atmosObject.Container.RemoveCoreGas(gas, amount);
            atmosObject.State = AtmosState.Active;
        }

        public void AddHeat(float amount)
        {
            atmosObject.Container.SetTemperature(amount); // TODO change back to heat when a proper setting is found.
            atmosObject.State = AtmosState.Active;
        }

        public void RemoveHeat(float amount)
        {
            atmosObject.Container.RemoveHeat(amount);
            atmosObject.State = AtmosState.Active;
        }


        public bool IsEmpty()
        {
            return atmosObject.Container.IsEmpty();
        }

        public bool IsAir()
        {
            return atmosObject.Container.IsAir();
        }

        public override string ToString()
        {
            string text = $"State: {atmosObject.State}, Pressure: {atmosObject.Container.GetPressure()}, Gasses: {atmosObject.Container.GetCoreGasses()}\n";
            text += $"Temperature: {atmosObject.Container.GetTemperature()} Kelvin, Compressibility factor: {atmosObject.Container.GetCompressionFactor()}\n";
            for (int i = 0; i < 8; i++)
            {
                AtmosObjectInfo info = GetNeighbour(i);
                text += $"neighbour{i}: State: {info.State}, Pressure: {info.Container.GetPressure()}, Temperature: {info.Container.GetTemperature()}\n";
            }

            return text;
        }
    }
}