
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;

namespace SS3D.Engine.AtmosphericsRework
{
    /*
    public struct AtmosObjectf
    {
        public AtmosState state;
        private AtmosContainer container;
        private bool temperatureSetting;

        // private AtmosObject neighbour;
        private bool4 activeDirection;

        private float4 tileFlux;
        private float4 neighbourFlux;
        private float4 difference;

        /// <summary>
        /// Unfortunately we cannot use arrays over here
        /// </summary>
        public AtmosObjectInfo neighbour1;
        public AtmosObjectInfo neighbour2;
        public AtmosObjectInfo neighbour3;
        public AtmosObjectInfo neighbour4;
        private bool4 neighbourUpdate;
        private float4 neighbourPressure;

        /// <summary>
        /// Helper function due to the lack of arrays
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
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

        public bool IsEmpty()
        {
            return container.IsEmpty();
        }

        // Performance makers
        static ProfilerMarker s_CalculateFluxPerfMarker = new ProfilerMarker("AtmosObject.CalculateFlux");
        // static ProfilerMarker s_CalculateFluxOnePerfMarker = new ProfilerMarker("AtmosObject.CalculateFlux.One");
        static ProfilerMarker s_SimulateFluxPerfMarker = new ProfilerMarker("AtmosObject.SimulateFlux");
        static ProfilerMarker s_SimlateMixingPerfMarker = new ProfilerMarker("AtmosObject.SimulateMixing");

        public void Setup()
        {
            container = new AtmosContainer();
            container.Setup();

            state = AtmosState.Active;
            temperatureSetting = false;
            // neighbours = new NativeArray<AtmosObject>(4, Allocator.Persistent);
            // neighbours = new AtmosObject();
            MakeEmpty();
        }


        public AtmosState GetState()
        {
            return state;
        }

        public void SetState(AtmosState state)
        {
            this.state = state;
        }

        public AtmosContainer GetContainer()
        {
            return container;
        }

        public void SetContainer(AtmosContainer container)
        {
            this.container = container;
        }

        public void SetNeighbours(AtmosObjectInfo info, int index)
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

        public void SetBlocked(bool isBlocked)
        {
            if (isBlocked)
                state = AtmosState.Blocked;
            else
                state = AtmosState.Active;
        }

        public void MakeAir()
        {
            container.MakeEmpty();

            container.AddCoreGas(CoreAtmosGasses.Oxygen, 20.79f);
            container.AddCoreGas(CoreAtmosGasses.Nitrogen, 83.17f);
            container.SetTemperature(293f); ;
        }

        public void MakeRandom()
        {
            container.MakeEmpty();

            for (int i = 0; i < 4; i++)
            {
                container.AddCoreGas((CoreAtmosGasses)i, UnityEngine.Random.Range(0, 300f));
            }
            container.SetTemperature(UnityEngine.Random.Range(0, 300f));
        }

        public void MakeEmpty()
        {
            container.MakeEmpty();
            activeDirection = false;
            tileFlux = 0f;
            neighbourFlux = 0f;
            difference = 0f;
            neighbourUpdate = false;
            neighbourPressure = 0f;
        }

        public void CalculateFlux()
        {
            s_CalculateFluxPerfMarker.Begin();

            float pressure = container.GetPressure();
            // tileFlux = 0f;

            for (int i = 0; i < 4; i++)
            {
                if ((!GetNeighbour(i).Equals(default(AtmosObjectInfo))) && GetNeighbour(i).state != AtmosState.Blocked)
                {
                    neighbourPressure[i] = GetNeighbour(i).container.GetPressure();
                    neighbourFlux[i] = math.min(tileFlux[i] * GasConstants.drag + (pressure - neighbourPressure[i]) * GasConstants.dt, 1000f);
                    activeDirection[i] = true;


                    if (neighbourFlux[i] < 0f)
                    {
                        AtmosObjectInfo neighbour = GetNeighbour(i);
                        neighbour.state = AtmosState.Active;
                        neighbourFlux[i] = 0f;
                        SetNeighbours(neighbour, i);
                    }
                }
                
            }

            /// Testing
            for (int i = 0; i < 4; i++)
            {
                if ((!GetNeighbour(i).Equals(default(AtmosObjectInfo))) && GetNeighbour(i).state != AtmosState.Blocked)
                {
                    neighbourPressure[i] = GetNeighbour(i).container.GetPressure();
                    neighbourUpdate[i] = true;
                }
                else
                {
                    neighbourPressure[i] = 0f;
                    neighbourUpdate[i] = false;
                }
            }

            neighbourFlux = math.min(tileFlux * GasConstants.drag + (pressure - neighbourPressure) * GasConstants.dt, 1000f);

            for (int i = 0; i < 4; i++)
            {
                if (neighbourUpdate[i] && neighbourFlux[i] < 0f)
                {
                    AtmosObjectInfo neighbour = GetNeighbour(i);
                    neighbour.state = AtmosState.Active;
                    neighbourFlux[i] = 0f;
                }
            }


            if (math.any(neighbourFlux > GasConstants.fluxEpsilon))
            {
                float scalingFactor = math.min(1f, pressure / math.csum(neighbourFlux) / GasConstants.dt);

                neighbourFlux *= scalingFactor;
                tileFlux = neighbourFlux;
            }
            else
            {
                tileFlux = 0f;
                if (!temperatureSetting)
                    state = AtmosState.Semiactive;
                else
                    temperatureSetting = false;
            }

            if (state == AtmosState.Semiactive || state == AtmosState.Active)
            {
                SimulateMixing();
            }

            s_CalculateFluxPerfMarker.End();
        }

        public void SimulateFlux()
        {
            s_SimulateFluxPerfMarker.Begin();

            if (state == AtmosState.Active)
            {
                SimulateFluxActive();
            }
            else if (state == AtmosState.Semiactive)
            {
                SimulateMixing();
            }

            s_SimulateFluxPerfMarker.End();
        }

        private void SimulateFluxActive()
        {
            float pressure = container.GetPressure();

            // for each neighbour
            if (math.any(tileFlux > 0f))
            {
                float4 factor = container.GetCoreGasses() * (tileFlux / pressure);

                for (int i = 0; i < 4; i++)
                {
                    if ((!GetNeighbour(i).Equals(default(AtmosObjectInfo))) && GetNeighbour(i).state != AtmosState.Vacuum)
                    {
                        AtmosObjectInfo neighbour = GetNeighbour(i);
                        neighbour.container.AddCoreGasses(factor);
                        SetNeighbours(neighbour, i);
                    }
                    else
                    {
                        activeDirection[i] = false;
                    }
                    container.RemoveCoreGasses(factor);
                }
            }

            float difference = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (activeDirection[i])
                {
                    difference = (container.GetTemperature() - GetNeighbour(i).container.GetTemperature())
                        * GasConstants.thermalBase * container.GetVolume();

                    if (difference > GasConstants.thermalEpsilon)
                    {
                        AtmosContainer neighbourContainer = GetNeighbour(i).container;
                        neighbourContainer.SetTemperature(neighbourContainer.GetTemperature() + difference);
                        container.SetTemperature(container.GetTemperature() - difference);
                        temperatureSetting = true;
                    }
                }
            }
        }

        public void SimulateMixing()
        {
            s_SimlateMixingPerfMarker.Begin();

            difference = 0f;
            bool mixed = false;
            if (math.any(container.GetCoreGasses() > 0f))
            {
                for (int i = 0; i < 4; i++)
                {
                    if ((!GetNeighbour(i).Equals(default(AtmosObjectInfo))) && GetNeighbour(i).state != AtmosState.Blocked)
                    {
                        AtmosObjectInfo neighbour = GetNeighbour(i);
                        // AtmosContainer neighbourContainer = GetNeighbour(i).container;
                        difference = (container.GetCoreGasses() - neighbour.container.GetCoreGasses()) * GasConstants.mixRate;
                        if (math.any(difference > GasConstants.minMoleTransfer))
                        {
                            // Increase neighbouring tiles moles and decrease ours
                            AtmosContainer neighbourContainer = neighbour.container;
                            neighbourContainer.AddCoreGasses(difference);
                            neighbour.container = neighbourContainer;
                            container.RemoveCoreGasses(difference);
                            mixed = true;


                            // Remain active if there is still a pressure difference
                            if (math.abs(neighbourContainer.GetPressure() - container.GetPressure()) > GasConstants.minMoleTransfer)
                            {
                                // AtmosObjectInfo neighbour = GetNeighbour(i);
                                neighbour.state = AtmosState.Active;
                                // SetNeighbours(neighbour, i);
                            }
                            else
                            {
                                // AtmosObjectInfo neighbour = GetNeighbour(i);
                                neighbour.state = AtmosState.Semiactive;
                                // SetNeighbours(neighbour, i);
                            }

                            SetNeighbours(neighbour, i);
                        }
                    }
                }
            }

            if (!mixed && state == AtmosState.Semiactive)
            {
                state = AtmosState.Inactive;
            }
            

            s_SimlateMixingPerfMarker.End();
        }
    }
    */
}