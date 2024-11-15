using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct SetInactiveJob : IJobParallelFor
    {
        private NativeArray<AtmosObject> _tileObjectBuffer;

        [ReadOnly]
        private NativeHashSet<int> _activeTransferIndex;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileObjectBuffer"> The array containing all atmos objects of a given atmos map.</param>
        /// <param name="activeTransferIndex"> A hashset containing the indexes from _tileObjectBuffer that did some
        /// transfer of gas moles to any of their neighbours in the current atmos simulation iteration.</param>
        public SetInactiveJob(NativeArray<AtmosObject> tileObjectBuffer, NativeHashSet<int> activeTransferIndex)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _activeTransferIndex = activeTransferIndex;
        }

        public void Execute(int index)
        {
            if (_activeTransferIndex.Contains(index) || _tileObjectBuffer[index].State == AtmosState.Blocked || _tileObjectBuffer[index].State == AtmosState.Vacuum)
            {
                return;
            }

            AtmosObject inactiveObject = _tileObjectBuffer[index];
            inactiveObject.SetInactive();
            _tileObjectBuffer[index] = inactiveObject;
        }
    }
}