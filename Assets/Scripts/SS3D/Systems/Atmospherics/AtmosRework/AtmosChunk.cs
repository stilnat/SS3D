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
        private Vector2Int chunkKey;

        private int width;
        private int height;
        private float tileSize = 1f;
        private Vector3 originPosition;
        protected AtmosMap map;
        private TileAtmosObject[] atmosGridList;

        public AtmosChunk(AtmosMap map, Vector2Int chunkKey, int width, 
            int height, float tileSize, Vector3 originPosition)
        {
            this.map = map;
            this.chunkKey = chunkKey;
            this.width = width;
            this.height = height;
            this.tileSize = tileSize;
            this.originPosition = originPosition;


            CreateAllGrids();
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public Vector2Int GetKey()
        {
            return chunkKey;
        }

        /// <summary>
        /// Returns the worldposition for a given x and y offset.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * tileSize + originPosition;
        }

        /// <summary>
        /// Returns the x and y offset for a given chunk position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetXY(Vector3 worldPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - originPosition.x), (int)Math.Round(worldPosition.z - originPosition.z));
        }

        protected void CreateAllGrids()
        {
            atmosGridList = new TileAtmosObject[GetWidth() * GetHeight()];

            for (int x = 0; x < GetWidth(); x++)
            {
                for (int y = 0; y < GetHeight(); y++)
                {
                    atmosGridList[y * GetWidth() + x] = new TileAtmosObject(map, this, x, y);
                }
            }
        }

        public TileAtmosObject GetTileAtmosObject(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < GetWidth() && y < GetHeight())
            {
                return atmosGridList[y * GetWidth() + x];
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
            return new List<TileAtmosObject>(atmosGridList);
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