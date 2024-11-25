using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosChunk
    {
        /// <summary>
        /// Unique key for each chunk
        /// </summary>
        private readonly Vector2Int _chunkKey;

        private readonly int _width;
        private readonly int _height;
        private readonly float _tileSize;
        private readonly Vector3 _originPosition;
        private readonly AtmosMap _map;
        private AtmosContainer[] _atmosGridList;
        private AtmosContainer[] _atmosPipeLeftList;

        public AtmosChunk(AtmosMap map, Vector2Int chunkKey, int width, 
            int height, float tileSize, Vector3 originPosition)
        {
            _map = map;
            _chunkKey = chunkKey;
            _width = width;
            _height = height;
            _tileSize = tileSize;
            _originPosition = originPosition;

        }
        
    }
}