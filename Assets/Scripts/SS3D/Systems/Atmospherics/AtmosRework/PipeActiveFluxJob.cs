using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct PipeActiveFluxJob : IJob
    {
        [ReadOnly]
        private readonly NativeArray<AtmosObject> _nativeAtmospipes;
        public PipeActiveFluxJob(NativeArray<AtmosObject> nativeAtmosTiles)
        {
            //_nativeAtmosTiles = nativeAtmosTiles;
            _nativeAtmospipes = nativeAtmosTiles;
        }
        public void Execute()
        {
            int sum = 0;
            for(int i = 0; i < 10000; i++)
            {
               // if (_nativeAtmosTiles[0].State == AtmosState.Blocked)
              //  {
             //       continue;
             //   }

                sum += 1;
            }
        }
    }
}