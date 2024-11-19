using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace SS3D.Engine.AtmosphericsRework
{
    /// <summary>
    /// Structure used to interface between the atmos map, and the jobs that do the computation for the simulation.
    /// </summary>
    public struct AtmosJobPersistentData
    {
        public readonly AtmosMap Map;

        public readonly List<AtmosContainer> AtmosTiles;
        public readonly List<AtmosContainer> AtmosLeftPipes;

        /// <summary>
        ///  For a given index in this array, return the indexes of all its neighbours, used by the atmos tiles. 
        /// </summary>
        public NativeArray<AtmosObjectNeighboursIndexes> NeighbourTileIndexes;

        /// <summary>
        /// Contains all atmos objects for all tiles on the map.
        /// </summary>
        public NativeArray<AtmosObject> NativeAtmosTiles;

        /// <summary>
        /// Contains all atmos objects for all pipe left on the map
        /// </summary>
        public NativeArray<AtmosObject> NativeAtmosPipesLeft;

        /// <summary>
        /// Array that contains data about tile indexes (as in NativeAtmosTiles) and how much moles they give to their neighbours.
        /// </summary>
        public NativeArray<MoleTransferToNeighbours> MoleTransferArray;

        /// <summary>
        /// Array that contains data about tile indexes (as in NativeAtmosTiles) and how much moles they give to their neighbours.
        /// </summary>
        public NativeArray<MoleTransferToNeighbours> PipeMoleTransferArray;
        
        /// <summary>
        /// Contains Chunk keys and the order in which they were created on the tilemap, used for efficient look up for neighbour tiles in jobs.
        /// TODO : update when chunk added
        /// </summary>
        public NativeHashMap<int2, int> ChunkKeyHashMap;


        public  NativeHashSet<int> ActiveTransferIndex ;


        public NativeHashSet<int> PipeActiveTransferIndex;

        
        public NativeList<int> ActiveEnvironmentIndexes;

        public NativeList<int> SemiActiveEnvironmentIndexes;

        public NativeList<int> ActiveLeftPipeIndexes;

        public NativeList<int> SemiActiveLeftPipeIndexes;

        // Keeps track of changed atmos objects 
        private readonly List<AtmosContainer> _atmosObjectsToChange;

        // Keeps track of changed atmos objects 
        private readonly List<AtmosContainer> _pipeAtmosObjectsToChange;

        public AtmosJobPersistentData(AtmosMap map, List<AtmosContainer> atmosTiles, List<AtmosContainer> atmosPipesleft)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            AtmosLeftPipes = atmosPipesleft;

            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
            NeighbourTileIndexes = new(atmosTiles.Count, Allocator.Persistent);
            NativeAtmosPipesLeft = new(atmosTiles.Count, Allocator.Persistent);
            ActiveTransferIndex = new(atmosTiles.Count, Allocator.Persistent);
            PipeMoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
            PipeActiveTransferIndex = new(atmosTiles.Count, Allocator.Persistent);
            ActiveEnvironmentIndexes = new(atmosTiles.Count, Allocator.Persistent);
            SemiActiveEnvironmentIndexes = new(atmosTiles.Count, Allocator.Persistent);
            ActiveLeftPipeIndexes = new(atmosTiles.Count, Allocator.Persistent);
            SemiActiveLeftPipeIndexes = new(atmosTiles.Count, Allocator.Persistent);

            // Fill the chunk key hash map in order of chunks created in the map
            List<int2> chunkKeyBuffer = Map.GetAtmosChunks().Select(x => new int2(x.GetKey().x, x.GetKey().y)).ToList();
            ChunkKeyHashMap = new(chunkKeyBuffer.Count, Allocator.Persistent);
            for (int i = 0; i < chunkKeyBuffer.Count; i++)
            {
                ChunkKeyHashMap.Add(chunkKeyBuffer[i], i);
            }

           
            _atmosObjectsToChange = new();
            _pipeAtmosObjectsToChange = new();
            LoadNativeArrays();
        }

        public void AddGas(AtmosContainer tile, CoreAtmosGasses gas, float amount)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }

            AtmosObject atmosObject = tile.AtmosObject;
            atmosObject.AddCoreGas(gas, amount, true);
            tile.AtmosObject = atmosObject;

            switch (tile.Layer)
            {
                   case TileLayer.Turf:
                       _atmosObjectsToChange.Add(tile);
                       break;
                   case TileLayer.PipeLeft:
                       _pipeAtmosObjectsToChange.Add(tile);
                       break;
            }
           
        }

        public void RemoveGas(AtmosContainer tile, CoreAtmosGasses gas, float amount)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }

            AtmosObject atmosObject = tile.AtmosObject;
            atmosObject.RemoveCoreGas(gas, amount, true);
            tile.AtmosObject = atmosObject;
            _atmosObjectsToChange.Add(tile);
        }

        public void RandomizeAllGasses(float maxAmount)
        {
            foreach (AtmosChunk atmosChunk in Map.GetAtmosChunks())
            {
                foreach (AtmosContainer tile in atmosChunk.GetAllTileAtmosObjects())
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
                foreach (AtmosContainer tile in atmosChunk.GetAllTileAtmosObjects())
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
            
            foreach (AtmosContainer atmosObject in _atmosObjectsToChange)
            {
                int indexInNativeArray = IndexOfTileAtmosObject(atmosObject);

                if (indexInNativeArray != -1)
                {
                    NativeAtmosTiles[indexInNativeArray] = atmosObject.AtmosObject;
                }
            }
            foreach (AtmosContainer atmosObject in _pipeAtmosObjectsToChange)
            {
                int indexInNativeArray = IndexOfTileAtmosObject(atmosObject);

                if (indexInNativeArray != -1)
                {
                    NativeAtmosPipesLeft[indexInNativeArray] = atmosObject.AtmosObject;
                }
            }
            _atmosObjectsToChange.Clear();
            _pipeAtmosObjectsToChange.Clear();
            ActiveEnvironmentIndexes.Clear();
            SemiActiveEnvironmentIndexes.Clear();
            ActiveLeftPipeIndexes.Clear();
            SemiActiveLeftPipeIndexes.Clear();
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < AtmosTiles.Count; i++)
            {
                NativeAtmosTiles[i] = AtmosTiles[i].AtmosObject;
                NativeAtmosPipesLeft[i] = AtmosLeftPipes[i].AtmosObject;
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
                AtmosLeftPipes[i].AtmosObject = NativeAtmosPipesLeft[i];
            }
        }

        private int IndexOfTileAtmosObject(AtmosContainer atmosObject)
        {
            if (!ChunkKeyHashMap.TryGetValue(atmosObject.AtmosObject.ChunkKey, out int indexChunk))
                return -1;

            return indexChunk * 16 * 16 + atmosObject.X + 16 * atmosObject.Y;
        }
    }

}