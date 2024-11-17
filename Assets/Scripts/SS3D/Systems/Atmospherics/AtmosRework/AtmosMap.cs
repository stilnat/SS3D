using SS3D.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SS3D.Systems.Tile;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosMap
    {
        /// <summary>
        /// Number of TileAtmosObjects that should go in a chunk. 16 x 16
        /// </summary>
        public const int CHUNK_SIZE = 16;

        /// <summary>
        /// The size of each tile.
        /// </summary>
        private const float TILE_SIZE = 1.0f;

        private Dictionary<Vector2Int, AtmosChunk> atmosChunks;
        public int ChunkCount { get => atmosChunks.Count; }

        private TileMap tileMap;
        private string mapName;
        private AtmosManager atmosManager;

        public AtmosMap(TileMap tileMap, string name)
        {
            atmosChunks = new Dictionary<Vector2Int, AtmosChunk>();
            atmosManager = Subsystems.Get<AtmosManager>();
            mapName = name;
            this.tileMap = tileMap;
        }

        public AtmosContainer GetTileAtmosObject(Vector3 worldPosition, TileLayer layer)
        {
            /*
            AtmosChunk chunk = GetOrCreateAtmosChunk(worldPosition);
            return chunk.GetTileAtmosObject(worldPosition);
            */

            Vector2Int key = GetKey(worldPosition);
            AtmosChunk chunk;

            // 
            if (!atmosChunks.TryGetValue(key, out chunk))
            {
                return null;
            }

            return chunk.GetTileAtmosObject(worldPosition, layer);
        }

        public string GetName()
        {
            return mapName;
        }

        public void SetName(string name)
        {
            mapName = name;
        }

        public void CreateChunkFromTileChunk(Vector2Int chunkKey, Vector3 origin)
        {
            atmosChunks[chunkKey] = CreateChunk(chunkKey, origin);
        }

        public TileMap GetLinkedTileMap()
        {
            return tileMap;
        }

        /// <summary>
        /// Create a new atmos chunk.
        /// </summary>
        /// <param name="chunkKey">Unique key to use</param>
        /// <param name="origin">Origin position of the chunk</param>
        /// <returns></returns>
        private AtmosChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            AtmosChunk chunk = new(this, chunkKey, CHUNK_SIZE, CHUNK_SIZE, TILE_SIZE, origin);

            return chunk;
        }

        /// <summary>
        /// Clears the entire map.
        /// </summary>
        public void Clear()
        {
            atmosChunks.Clear();
        }

        /// <summary>
        /// Returns the chunk key to used based on an X and Y offset.
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkY"></param>
        /// <returns></returns>
        private Vector2Int GetKey(int chunkX, int chunkY)
        {
            return new Vector2Int(chunkX, chunkY);
        }

        /// <summary>
        /// Returns the chunk key to be used based on a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / CHUNK_SIZE);
            int y = (int)Math.Floor(worldPosition.z / CHUNK_SIZE);

            return (GetKey(x, y));
        }

        /// <summary>
        /// Returns the tile chunk based the world position. Will create a new chunk if it doesn't exist.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private AtmosChunk GetOrCreateAtmosChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);
            AtmosChunk chunk;

            // Create a new chunk if there is none
            if (!atmosChunks.TryGetValue(key, out chunk))
            {
                Vector3 origin = new Vector3 { x = key.x * CHUNK_SIZE, z = key.y * CHUNK_SIZE };
                atmosChunks[key] = CreateChunk(key, origin);
            }

            return atmosChunks[key];
        }
        
        public AtmosChunk[] GetAtmosChunks()
        {
            return atmosChunks?.Values.ToArray();
        }
    }
}