
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;

namespace SS3D.Engine.AtmosphericsRework
{
 public struct AtmosObject
 {

        public AtmosContainer Container;
        public AtmosState State;
        public float2 Velocity;
        public int2 ChunkKey;

        public bool temperatureSetting;
        public bool4 activeDirection;

        public AtmosObject(int2 chunkKey)
        {
            ChunkKey = chunkKey;
            State = AtmosState.Inactive;
            Container = default;
            Velocity = default;
            temperatureSetting = false;
            activeDirection = default;
        }

        public void ClearCoreGasses()
        {
            Container = new();
            Container.Setup();

            if (State == AtmosState.Active || State == AtmosState.Semiactive)
            {
                State = AtmosState.Inactive;
            }
        }

        public void AddCoreGasses(float4 amount)
        {
            Container.AddCoreGasses(amount);
            if (math.any(amount != float4.zero))
            {
                State = AtmosState.Active;
            }
        }

        public void RemoveCoreGasses(float4 amount)
        {
            Container.RemoveCoreGasses(amount);

            if (math.any(amount != float4.zero))
            {
                State = AtmosState.Active;
            }
        }

        public void AddGas(CoreAtmosGasses gas, float amount)
        {
            Container.AddCoreGas(gas, amount);

            if (amount != 0)
            {
                State = AtmosState.Active;
            }
        }

        public void RemoveGas(CoreAtmosGasses gas, float amount)
        {
            Container.RemoveCoreGas(gas, amount);
            State = AtmosState.Active;
        }

        public void AddHeat(float amount)
        {
            Container.SetTemperature(amount); // TODO change back to heat when a proper setting is found.
            State = AtmosState.Active;
        }

        public void RemoveHeat(float amount)
        {
            Container.RemoveHeat(amount);
            State = AtmosState.Active;
        }


        public bool IsEmpty()
        {
            return Container.IsEmpty();
        }

        public bool IsAir()
        {
            return Container.IsAir();
        }

        public override string ToString()
        {
            string text = $"State: {State}, Pressure: {Container.GetPressure()}, Gasses: {Container.GetCoreGasses()}\n";
            text += $"Temperature: {Container.GetTemperature()} Kelvin, Compressibility factor: {Container.GetCompressionFactor()}\n";
            return text;
        }
    }
}