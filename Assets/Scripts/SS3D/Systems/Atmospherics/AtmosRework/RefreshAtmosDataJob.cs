using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct RefreshAtmosDataJob : IJobParallelFor
    {
        [ReadOnly]
        private readonly List<TileAtmosObject> _atmosTiles;

        private NativeList<AtmosObject> _nativeAtmosTiles;

        public RefreshAtmosDataJob(List<TileAtmosObject> atmosTiles,  NativeList<AtmosObject> nativeAtmosTiles)
        {
            _atmosTiles = atmosTiles;
            _nativeAtmosTiles = nativeAtmosTiles;
        }
        public void Execute(int index)
        {
            _nativeAtmosTiles[index] = _atmosTiles[index].AtmosObject;
        }
    }
}