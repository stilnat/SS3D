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

        private enum ChangeType
        {
            AddGas,
            RemoveGas,
            AddHeat,
            RemoveHeat,
            StateChange,
        }

        private struct TileChanges
        {
            public ChangeType ChangeType;
            public int X;
            public int Y;
            public int2 ChunkKey;
            public float4 Moles;
            public AtmosState State;
            public float Heat;
        }
        
        public readonly AtmosMap Map;

        public readonly List<AtmosContainer> AtmosTiles;

        /// <summary>
        ///  For a given index in this array, return the indexes of all its neighbours, used by the atmos tiles. 
        /// </summary>
        public NativeArray<AtmosObjectNeighboursIndexes> NeighbourTileIndexes;

        /// <summary>
        /// Contains all atmos objects for all tiles on the map.
        /// </summary>
        public NativeArray<AtmosObject> NativeAtmosTiles;

        /// <summary>
        /// Array that contains data about tile indexes (as in NativeAtmosTiles) and how much moles they give to their neighbours.
        /// </summary>
        public NativeArray<MoleTransferToNeighbours> MoleTransferArray;

        /// <summary>
        /// Array that contains data about tile indexes (as in NativeAtmosTiles) and how much heat they give to their neighbours.
        /// </summary>
        public NativeArray<HeatTransferToNeighbours> HeatTransferArray;
        
        /// <summary>
        /// Contains Chunk keys and the order in which they were created on the tilemap, used for efficient look up for neighbour tiles in jobs.
        /// TODO : update when chunk added
        /// </summary>
        public NativeHashMap<int2, int> ChunkKeyHashMap;
        
        public NativeList<int> ActiveEnvironmentIndexes;

        public NativeList<int> SemiActiveEnvironmentIndexes;

        // Keeps track of changed atmos objects 
        private readonly List<TileChanges> _atmosObjectsToChange;

        public AtmosJobPersistentData(AtmosMap map, List<AtmosContainer> atmosTiles)
        {
            Map = map;
            AtmosTiles = atmosTiles;

            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);
            NeighbourTileIndexes = new(atmosTiles.Count, Allocator.Persistent);
            ActiveEnvironmentIndexes = new(atmosTiles.Count, Allocator.Persistent);
            SemiActiveEnvironmentIndexes = new(atmosTiles.Count, Allocator.Persistent);
            HeatTransferArray = new(atmosTiles.Count, Allocator.Persistent);

            // Fill the chunk key hash map in order of chunks created in the map
            List<int2> chunkKeyBuffer = Map.GetAtmosChunks().Select(x => new int2(x.GetKey().x, x.GetKey().y)).ToList();
            ChunkKeyHashMap = new(chunkKeyBuffer.Count, Allocator.Persistent);
            for (int i = 0; i < chunkKeyBuffer.Count; i++)
            {
                ChunkKeyHashMap.Add(chunkKeyBuffer[i], i);
            }

            _atmosObjectsToChange = new();
            LoadNativeArrays();
        }

        public void ChangeState(AtmosContainer tile, AtmosState state)
        {
            AtmosObject atmosObject = tile.AtmosObject;

            TileChanges tileChanges = new()
            {
                ChangeType = ChangeType.StateChange,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Moles = 0, 
                State = state,
            };

            _atmosObjectsToChange.Add(tileChanges);
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
                ChangeType = ChangeType.RemoveGas,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Moles = amount, 
            };

            _atmosObjectsToChange.Add(tileChanges);
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
                ChangeType = ChangeType.AddGas,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Moles = amount, 
            };
            _atmosObjectsToChange.Add(tileChanges);
        }

        public void AddHeat(AtmosContainer tile, float heat)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }
            
            AtmosObject atmosObject = tile.AtmosObject;

            TileChanges tileChanges = new()
            {
                ChangeType = ChangeType.AddHeat,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Heat = heat,
            };
            _atmosObjectsToChange.Add(tileChanges);
        }

        public void RemoveHeat(AtmosContainer tile, float heat)
        {
            if (tile.AtmosObject.State == AtmosState.Blocked)
            {
                return;
            }
            
            AtmosObject atmosObject = tile.AtmosObject;

            TileChanges tileChanges = new()
            {
                ChangeType = ChangeType.RemoveHeat,
                X = tile.X,
                Y = tile.Y,
                ChunkKey = atmosObject.ChunkKey,
                Heat = heat,
            };
            _atmosObjectsToChange.Add(tileChanges);
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
                        ChangeType = ChangeType.AddGas,
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
                        ChangeType = ChangeType.RemoveGas,
                        X = tile.X,
                        Y = tile.Y,
                        ChunkKey = atmosObject.ChunkKey,
                        Moles = atmosObject.CoreGasses, 
                    };

                    _atmosObjectsToChange.Add(tileChanges);
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

                switch (change.ChangeType)
                {
                     case ChangeType.AddGas:
                         atmosObject.AddCoreGasses(change.Moles);
                         atmosObject.State = AtmosState.Active;
                         break;
                     case ChangeType.RemoveGas:
                         atmosObject.RemoveCoreGasses(change.Moles);
                         atmosObject.State = AtmosState.Active;
                         break;
                     case ChangeType.AddHeat:
                         atmosObject.AddHeat(change.Heat);
                         atmosObject.State = AtmosState.Active;
                         break;
                     case ChangeType.RemoveHeat:
                         atmosObject.RemoveHeat(change.Heat);
                         atmosObject.State = AtmosState.Active;
                         break;
                     case ChangeType.StateChange:
                         atmosObject.State = change.State;
                         break;
                }

                
                    
                NativeAtmosTiles[indexInNativeArray] = atmosObject;
            }
            
            _atmosObjectsToChange.Clear();
            ActiveEnvironmentIndexes.Clear();
            SemiActiveEnvironmentIndexes.Clear();
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
            NeighbourTileIndexes.Dispose();
            MoleTransferArray.Dispose();
        }

        /// <summary>
        /// Writes back the results from the NativeContainers to the lists.
        /// </summary>
        public void WriteResultsToList()
        {
            WriteResults(ActiveEnvironmentIndexes, NativeAtmosTiles, AtmosTiles);
            WriteResults(SemiActiveEnvironmentIndexes, NativeAtmosTiles, AtmosTiles);
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