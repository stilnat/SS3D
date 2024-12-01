using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct ComputeFluxesJob : IJobParallelFor
    {

        // todo : using NativeDisableParallelForRestriction is a dirty trick to avoid doing proper code. This might lead to race conditions.
        // The issue is that each jobs need to access atmos tile outside its set of indexes
        // Unity recommends using a so called double buffering methods.
        // https://github.com/korzen/Unity3D-JobsSystemAndBurstSamples/blob/master/Assets/JobsAndBurst/Scripts/DoubleBufferingBasics.cs

        [ReadOnly]
        private readonly NativeArray<AtmosObject> _tileObjectBuffer;

        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        [ReadOnly]
        private readonly NativeArray<int> _activeIndexes;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private readonly float _deltaTime;

        private readonly bool _activeFlux;



        public ComputeFluxesJob(NativeArray<AtmosObject> tileObjectBuffer, NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes, NativeArray<int> activeIndexes, float deltaTime, bool activeFlux)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _deltaTime = deltaTime;
            _moleTransfers = moleTransfers;
            _activeFlux = activeFlux;
            _neighboursIndexes = neighboursIndexes;
            _activeIndexes = activeIndexes;
        }

        public void Execute(int index)
        {
            int activeIndex = _activeIndexes[index];

            AtmosObject defaultAtmos = default;

            AtmosObject northNeighbour = _neighboursIndexes[activeIndex].NorthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].NorthNeighbour];
            AtmosObject southNeighbour = _neighboursIndexes[activeIndex].SouthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].SouthNeighbour]; 
            AtmosObject eastNeighbour = _neighboursIndexes[activeIndex].EastNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].EastNeighbour]; 
            AtmosObject westNeighbour = _neighboursIndexes[activeIndex].WestNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].WestNeighbour]; 

            bool4 hasNeighbours = new(_neighboursIndexes[activeIndex].NorthNeighbour != -1,
                _neighboursIndexes[activeIndex].SouthNeighbour != -1,
                _neighboursIndexes[activeIndex].EastNeighbour != -1,
            _neighboursIndexes[activeIndex].WestNeighbour != -1);

            // Do actual work
            _moleTransfers[activeIndex] = AtmosCalculator.SimulateGasTransfers(
                _tileObjectBuffer[activeIndex], 
                activeIndex,
                northNeighbour,
                southNeighbour, 
                eastNeighbour,
                westNeighbour,
                _deltaTime, 
                _activeFlux,
                hasNeighbours);
        }
    }
}