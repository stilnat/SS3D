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
        private TileAtmosObject[] _atmosGridList;

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
            _atmosGridList = new TileAtmosObject[GetWidth() * GetHeight()];

            for (int x = 0; x < GetWidth(); x++)
            {
                for (int y = 0; y < GetHeight(); y++)
                {
                    _atmosGridList[y * GetWidth() + x] = new(_map, this, x, y);
                }
            }
        }

        public TileAtmosObject GetTileAtmosObject(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < GetWidth() && y < GetHeight())
            {
                return _atmosGridList[y * GetWidth() + x];
            }
            else
            {
                return default;
            }
        }

        public TileAtmosObject GetTileAtmosObject(Vector3 worldPosition)
        {
            Vector2Int vector = new Vector2Int();
            vector = GetXY(worldPosition);
            return GetTileAtmosObject(vector.x, vector.y);
        }

        public List<TileAtmosObject> GetAllTileAtmosObjects()
        {
            return new List<TileAtmosObject>(_atmosGridList);
        }

        /*
        /// <summary>
        /// Saves all the TileAtmosObjects in the chunk.
        /// </summary>
        /// <returns></returns>
        public ChunkSaveObject Save()
        {
            
            // Let's save the tile objects first
            List<TileAtmosObject.AtmosSaveObject> atmosObjectSaveList = new List<TileAtmosObject.AtmosSaveObject>();
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                for (int x = 0; x < GetWidth(); x++)
                {
                    for (int y = 0; y < GetHeight(); y++)
                    {
                        TileAtmosObject atmosObject = GetTileAtmosObject(x, y);
                        
                        atmosObjectSaveList.Add(atmosObject.Save());
                    }
                }
            }

            ChunkSaveObject<TileAtmosObject.AtmosSaveObject> saveObject = new ChunkSaveObject<TileAtmosObject.AtmosSaveObject>
            {
                height = GetHeight(),
                originPosition = GetOrigin(),
                tileSize = GetTileSize(),
                width = GetWidth(),
                chunkKey = GetKey(),
                saveArray = atmosObjectSaveList.ToArray()
            };

            return saveObject;
            

            return null;
        }

    */
    }
}