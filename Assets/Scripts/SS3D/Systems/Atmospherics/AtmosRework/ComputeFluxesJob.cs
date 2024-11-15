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

        // Hashmap of chunk keys, with values indicating order as they were created by the atmos map. Useful as two adjacent tiles objects might have very different positions if they're not in the same chunk.
        // Only adjacent tile objects in the same chunk can be retrieved without that, so its necessary for computing indexes in the TileObjectBuffer of neighbours on chunk edges.
        // todo : might be more efficient to use a sorter native array as burst doesn't like NativeHashMap
        [ReadOnly]
        private readonly NativeHashMap<int2, int> _chunkKeyHashMap;

        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private readonly float _deltaTime;

        private readonly bool _activeFlux;

        public ComputeFluxesJob(NativeArray<AtmosObject> tileObjectBuffer, NativeHashMap<int2, int> chunkKeyHashMap, NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes, float deltaTime, bool activeFlux)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _deltaTime = deltaTime;
            _moleTransfers = moleTransfers;
            _chunkKeyHashMap = chunkKeyHashMap;
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

            // Do actual work
            _moleTransfers[index] = AtmosCalculator.SimulateGasTransfers(
                index, 
                _neighboursIndexes[index].NorthNeighbour,
                _neighboursIndexes[index].SouthNeighbour, 
                _neighboursIndexes[index].EastNeighbour,
                _neighboursIndexes[index].WestNeighbour,
                _tileObjectBuffer,
                _deltaTime, 
                _activeFlux);
        }
    }
}