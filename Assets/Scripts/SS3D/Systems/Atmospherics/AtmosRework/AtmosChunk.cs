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

            CreateAllGrids();
        }

        public int GetWidth()
        {
            return _width;
        }

        public int GetHeight()
        {
            return _height;
        }

        public Vector2Int GetKey()
        {
            return _chunkKey;
        }

        /// <summary>
        /// Returns the worldposition for a given x and y offset.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * _tileSize + _originPosition;
        }

        /// <summary>
        /// Returns the x and y offset for a given chunk position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetXY(Vector3 worldPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - _originPosition.x), (int)Math.Round(worldPosition.z - _originPosition.z));
        }

        protected void CreateAllGrids()
        {
            _atmosGridList = new AtmosContainer[GetWidth() * GetHeight()];
            _atmosPipeLeftList =  new AtmosContainer[GetWidth() * GetHeight()];

            for (int x = 0; x < GetWidth(); x++)
            {
                for (int y = 0; y < GetHeight(); y++)
                {
                    _atmosGridList[y * GetWidth() + x] = new(_map, this, x, y, TileLayer.Turf, 2.5f);
                    _atmosPipeLeftList[y * GetWidth() + x] = new(_map, this, x, y, TileLayer.PipeLeft, 0.25f);
                }
            }
        }

        public AtmosContainer GetTileAtmosObject(int x, int y, TileLayer layer)
        {
            if (x >= 0 && y >= 0 && x < GetWidth() && y < GetHeight())
            {
                switch (layer)
                {
                    case TileLayer.Turf:
                        return _atmosGridList[y * GetWidth() + x];
                    case TileLayer.PipeLeft:
                        return _atmosPipeLeftList[y * GetWidth() + x];
                }
            }

            return default;
        }

        public AtmosContainer GetTileAtmosObject(Vector3 worldPosition, TileLayer layer)
        {
            Vector2Int vector = new Vector2Int();
            vector = GetXY(worldPosition);
            return GetTileAtmosObject(vector.x, vector.y, layer);
        }

        public List<AtmosContainer> GetAllTileAtmosObjects()
        {
            return new(_atmosGridList);
        }

        public List<AtmosContainer> GetAllPipeLeftAtmosObjects()
        {
            return new(_atmosPipeLeftList);
        }
    }
}