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

        [WriteOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private readonly float _deltaTime;

        private readonly bool _activeFlux;

        public ComputeFluxesJob(NativeArray<AtmosObject> tileObjectBuffer, NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes, float deltaTime, bool activeFlux)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _deltaTime = deltaTime;
            _moleTransfers = moleTransfers;
            _activeFlux = activeFlux;
            _neighboursIndexes = neighboursIndexes;
        }

        public void Execute(int index)
        {
            if (_tileObjectBuffer[index].State != AtmosState.Active && _tileObjectBuffer[index].State != AtmosState.Semiactive)
            {
                _moleTransfers[index] = default;
                return;
            }

            AtmosObject defaultAtmos = default;

            AtmosObject northNeighbour = _neighboursIndexes[index].NorthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[index].NorthNeighbour];
            AtmosObject southNeighbour = _neighboursIndexes[index].SouthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[index].SouthNeighbour]; 
            AtmosObject eastNeighbour = _neighboursIndexes[index].EastNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[index].EastNeighbour]; 
            AtmosObject westNeighbour = _neighboursIndexes[index].WestNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[index].WestNeighbour]; 

            bool4 hasNeighbours = new(_neighboursIndexes[index].NorthNeighbour != -1,
                _neighboursIndexes[index].SouthNeighbour != -1,
                _neighboursIndexes[index].EastNeighbour != -1,
            _neighboursIndexes[index].WestNeighbour != -1);

            // Do actual work
            _moleTransfers[index] = AtmosCalculator.SimulateGasTransfers(
                _tileObjectBuffer[index], 
                index,
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