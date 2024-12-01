using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    //[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ComputeVelocityJob : IJobParallelFor
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        [NativeDisableParallelForRestriction]
        private NativeArray<AtmosObject> _tileObjectBuffer;
        
        [ReadOnly]
        private NativeArray<int> _activeIndexes;
        
        public ComputeVelocityJob(NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<MoleTransferToNeighbours> moleTransfers, NativeArray<int> activeIndexes)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _moleTransfers = moleTransfers;
            _activeIndexes = activeIndexes;
        }

        public void Execute(int index)
        {
            int activeIndex = _activeIndexes[index];
            AtmosObject atmosObject = _tileObjectBuffer[activeIndex];
            atmosObject.VelocityNorth = _moleTransfers[activeIndex].TransferMolesNorth;
            atmosObject.VelocitySouth = _moleTransfers[activeIndex].TransferMolesSouth;
            atmosObject.VelocityEast = _moleTransfers[activeIndex].TransferMolesEast;
            atmosObject.VelocityWest = _moleTransfers[activeIndex].TransferMolesWest;
            _tileObjectBuffer[activeIndex] = atmosObject;
        }
    }
}