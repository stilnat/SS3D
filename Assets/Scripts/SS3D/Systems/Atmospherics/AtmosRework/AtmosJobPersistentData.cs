using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace SS3D.Engine.AtmosphericsRework
{
    /// <summary>
    /// Structure used to interface between the atmos map, and the jobs that do the computation for the simulation.
    /// </summary>
    public struct AtmosJobPersistentData
    {
        public readonly AtmosMap Map;

        public readonly List<TileAtmosObject> AtmosTiles;

        public NativeArray<AtmosObjectNeighboursIndexes> NeighbourIndexes;

        public NativeArray<AtmosObject> NativeAtmosTiles;

        public NativeArray<MoleTransferToNeighbours> MoleTransferArray;
        
        /// <summary>
        /// Contains Chunk keys and the order in which they were created on the tilemap, used for efficient look up for neighbour tiles in jobs.
        /// TODO : update when chunk added
        /// </summary>
        public NativeHashMap<int2, int> ChunkKeyHashMap;

        private readonly List<TileAtmosObject> _atmosObjectsToChange;
        

        public AtmosJobPersistentData(AtmosMap map, List<TileAtmosObject> atmosTiles, List<IAtmosLoop> atmosDevices)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
            NeighbourIndexes = new(atmosTiles.Count, Allocator.Persistent);
            List<int2> chunkKeyBuffer = Map.GetAtmosChunks().Select(x => new int2(x.GetKey().x, x.GetKey().y)).ToList();
            ChunkKeyHashMap = new(chunkKeyBuffer.Count, Allocator.Persistent);
            for (int i = 0; i < chunkKeyBuffer.Count; i++)
            {
                ChunkKeyHashMap.Add(chunkKeyBuffer[i], i);
            }

            _atmosObjectsToChange = new();
            LoadNativeArrays();
        }

        public void AddGas(TileAtmosObject tile, CoreAtmosGasses gas, float amount)
        {
            AtmosObject atmosObject = tile.AtmosObject;
            atmosObject.AddCoreGas(gas, amount, true);
            tile.AtmosObject = atmosObject;
            _atmosObjectsToChange.Add(tile);
        }

        public void RemoveGas(TileAtmosObject tile, CoreAtmosGasses gas, float amount)
        {
            AtmosObject atmosObject = tile.AtmosObject;
            atmosObject.RemoveCoreGas(gas, amount, true);
            tile.AtmosObject = atmosObject;
            _atmosObjectsToChange.Add(tile);
        }

        public void RandomizeAllGasses(float maxAmount)
        {
            foreach (AtmosChunk atmosChunk in Map.GetAtmosChunks())
            {
                foreach (TileAtmosObject tile in atmosChunk.GetAllTileAtmosObjects())
                {
                    AtmosObject atmosObject = tile.AtmosObject;
                    atmosObject.AddCoreGasses(UnityEngine.Random.Range(0, maxAmount) * maxAmount, true);
                    tile.AtmosObject = atmosObject;
                    _atmosObjectsToChange.Add(tile);
                }
            }
        }

        public void ClearAllGasses()
        {
            foreach (AtmosChunk atmosChunk in Map.GetAtmosChunks())
            {
                foreach (TileAtmosObject tile in atmosChunk.GetAllTileAtmosObjects())
                {
                    AtmosObject atmosObject = tile.AtmosObject;
                    atmosObject.ClearCoreGasses();
                    tile.AtmosObject = atmosObject;
                    _atmosObjectsToChange.Add(tile);
                }
            }
        }

        /// <summary>
        /// Refreshes the calculation array. Must be called when gas is added/removed from the system.
        /// </summary>
        public void Refresh()
        {
            foreach (TileAtmosObject atmosObject in _atmosObjectsToChange)
            {
                int indexInNativeArray = IndexOfTileAtmosObject(atmosObject);

                if (indexInNativeArray != -1)
                {
                    NativeAtmosTiles[indexInNativeArray] = atmosObject.AtmosObject;
                }
            }
            _atmosObjectsToChange.Clear();
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < AtmosTiles.Count; i++)
            {
                NativeAtmosTiles[i] = AtmosTiles[i].AtmosObject;
            }
        }

        public void Destroy()
        {
            NativeAtmosTiles.Dispose();
        }

        public int CountActive()
        {
            return AtmosTiles.Count(atmosObject => atmosObject.AtmosObject.State == AtmosState.Active || atmosObject.AtmosObject.State == AtmosState.Semiactive);
        }

        /// <summary>
        /// Writes back the results from the NativeContainers to the lists.
        /// </summary>
        public void WriteResultsToList()
        {
            for (int i = 0; i < NativeAtmosTiles.Length; i++)
            {
                AtmosTiles[i].AtmosObject = NativeAtmosTiles[i];
            }
        }

        private int IndexOfTileAtmosObject(TileAtmosObject atmosObject)
        {
            if (!ChunkKeyHashMap.TryGetValue(atmosObject.AtmosObject.ChunkKey, out int indexChunk))
                return -1;

            return indexChunk * 16 * 16 + atmosObject.X + 16 * atmosObject.Y;
        }
    }

}