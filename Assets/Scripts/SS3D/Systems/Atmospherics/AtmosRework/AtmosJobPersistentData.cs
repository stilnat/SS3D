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
        private struct TileChanges
        {
            public bool Add;
            public int X;
            public int Y;
            public int2 ChunkKey;
            public float4 Moles;
        }
        
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
        
        public NativeList<int> ActiveEnvironmentIndexes;

        public NativeList<int> SemiActiveEnvironmentIndexes;

        public NativeList<int> ActiveLeftPipeIndexes;

        public NativeList<int> SemiActiveLeftPipeIndexes;

        // Keeps track of changed atmos objects 
        private readonly List<TileChanges> _atmosObjectsToChange;

        // Keeps track of changed atmos objects 
        private readonly List<TileChanges> _pipeAtmosObjectsToChange;

        public AtmosJobPersistentData(AtmosMap map, List<AtmosContainer> atmosTiles, List<AtmosContainer> atmosPipesleft)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            AtmosLeftPipes = atmosPipesleft;

            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
            NeighbourTileIndexes = new(atmosTiles.Count, Allocator.Persistent);
            NativeAtmosPipesLeft = new(atmosTiles.Count, Allocator.Persistent);
            PipeMoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
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
        
        public void RemoveGasses(AtmosContainer tile, float4 amount)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }

            AtmosObject atmosObject = tile.AtmosObject;

            TileChanges tileChanges = new()
            {
                Add = false,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Moles = amount, 
            };


            switch (tile.Layer)
            {
                case TileLayer.Turf:
                    _atmosObjectsToChange.Add(tileChanges);
                    break;
                case TileLayer.PipeLeft:
                    _pipeAtmosObjectsToChange.Add(tileChanges);
                    break;
            }
        }
        
        public void AddGasses(AtmosContainer tile, float4 amount)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }
            
            AtmosObject atmosObject = tile.AtmosObject;

            TileChanges tileChanges = new()
            {
                Add = true,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Moles = amount, 
            };
            
            switch (tile.Layer)
            {
                case TileLayer.Turf:
                    _atmosObjectsToChange.Add(tileChanges);
                    break;
                case TileLayer.PipeLeft:
                    _pipeAtmosObjectsToChange.Add(tileChanges);
                    break;
            }
        }

        public void RandomizeAllGasses(float maxAmount)
        {
            foreach (AtmosChunk atmosChunk in Map.GetAtmosChunks())
            {
                foreach (AtmosContainer tile in atmosChunk.GetAllTileAtmosObjects())
                {
                    AtmosObject atmosObject = tile.AtmosObject;
                    float4 moles = UnityEngine.Random.Range(0, maxAmount);

                    TileChanges tileChanges = new()
                    {
                        Add = true,
                        X = tile.X,
                        Y = tile.Y,
                        ChunkKey = atmosObject.ChunkKey,
                        Moles = moles, 
                    };
                    
                    _atmosObjectsToChange.Add(tileChanges);
                }
            }
        }

        public void ClearAllGasses()
        {
            foreach (AtmosChunk atmosChunk in Map.GetAtmosChunks())
            {
                foreach (AtmosContainer tile in atmosChunk.GetAllAtmosObjects())
                {
                    AtmosObject atmosObject = tile.AtmosObject;
                    
                    TileChanges tileChanges = new()
                    {
                        Add = false,
                        X = tile.X,
                        Y = tile.Y,
                        ChunkKey = atmosObject.ChunkKey,
                        Moles = atmosObject.CoreGasses, 
                    };

                    switch (tile.Layer)
                    {
                        case TileLayer.Turf:
                            _atmosObjectsToChange.Add(tileChanges);
                            break;
                        case TileLayer.PipeLeft:
                            _pipeAtmosObjectsToChange.Add(tileChanges);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the calculation array. Must be called when gas is added/removed from the system.
        /// </summary>
        public void Refresh()
        {
            foreach (TileChanges change in _atmosObjectsToChange)
            {
                int indexInNativeArray = IndexOfTileAtmosObject(change.ChunkKey, change.X, change.Y);

                if (indexInNativeArray == -1)
                {
                    continue;
                }

                AtmosObject atmosObject = NativeAtmosTiles[indexInNativeArray];

                if (change.Add)
                {
                    atmosObject.AddCoreGasses(change.Moles); 
                }
                else
                {
                    atmosObject.RemoveCoreGasses(change.Moles); 
                }
                    
                NativeAtmosTiles[indexInNativeArray] = atmosObject;
            }
            
            foreach (TileChanges change in _pipeAtmosObjectsToChange)
            {
                int indexInNativeArray = IndexOfTileAtmosObject(change.ChunkKey, change.X, change.Y);

                if (indexInNativeArray == -1)
                {
                    continue;
                }

                AtmosObject atmosObject = NativeAtmosPipesLeft[indexInNativeArray];

                if (change.Add)
                {
                    atmosObject.AddCoreGasses(change.Moles); 
                }
                else
                {
                    atmosObject.RemoveCoreGasses(change.Moles); 
                }
                    
                NativeAtmosPipesLeft[indexInNativeArray] = atmosObject;
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
            NativeAtmosPipesLeft.Dispose();
            NeighbourTileIndexes.Dispose();
            MoleTransferArray.Dispose();
            PipeMoleTransferArray.Dispose();
        }

        /// <summary>
        /// Writes back the results from the NativeContainers to the lists.
        /// </summary>
        public void WriteResultsToList()
        {
            WriteResults(ActiveEnvironmentIndexes, NativeAtmosTiles, AtmosTiles);
            WriteResults(SemiActiveEnvironmentIndexes, NativeAtmosTiles, AtmosTiles);

            WriteResults(ActiveLeftPipeIndexes, NativeAtmosPipesLeft, AtmosLeftPipes);
            WriteResults(SemiActiveLeftPipeIndexes, NativeAtmosPipesLeft, AtmosLeftPipes);
        }

        private void WriteResults(NativeList<int> activeIndexes, NativeArray<AtmosObject> nativeAtmosObjects, List<AtmosContainer> atmosObjects)
        {
            for (int i = 0; i < activeIndexes.Length; i++)
            {
                int activeIndex = activeIndexes[i];
                atmosObjects[activeIndex].AtmosObject = nativeAtmosObjects[activeIndex];
            }
        }

        private int IndexOfTileAtmosObject(int2 chunkKey, int x, int y)
        {
            if (!ChunkKeyHashMap.TryGetValue(chunkKey, out int indexChunk))
            {
                return -1;
            }

            return (indexChunk * 16 * 16) + x + (16 * y);
        }
    }

}