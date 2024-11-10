using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct MoleTransfer
    {
        public float4 Moles;
        public int IndexTo;
    }

    public struct MoleTransferToNeighbours
    {
        public int IndexFrom;
        public MoleTransfer TransferOne;
        public MoleTransfer TransferTwo;
        public MoleTransfer TransferThree;
        public MoleTransfer TransferFour;
    }

    public struct AtmosJob
    {
        public AtmosMap Map;

        public List<TileAtmosObject> AtmosTiles;

        public NativeArray<AtmosObject> NativeAtmosTiles;

        public NativeArray<MoleTransferToNeighbours> MoleTransferArray;

        //public NativeArray<AtmosObject> ResultNativeAtmosTiles;

        public AtmosJob(AtmosMap map, List<TileAtmosObject> atmosTiles, List<IAtmosLoop> atmosDevices)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);

            LoadNativeArrays();
        }

        public void Destroy()
        {
            NativeAtmosTiles.Dispose();
        }

        public int CountActive()
        {
            return NativeAtmosTiles.Count(atmosObject => atmosObject.State == AtmosState.Active || atmosObject.State == AtmosState.Semiactive);
        }

        /// <summary>
        /// Refreshes the calculation array. Must be called when gas is added/removed from the system.
        /// </summary>
        public void Refresh()
        {
            LoadNativeArrays();
        }

        /// <summary>
        /// Writes back the results from the NativeContainers to the lists.
        /// </summary>
        public void WriteResultsToList()
        {
            for (int i = 0; i < NativeAtmosTiles.Length; i++)
            {
                AtmosTiles[i].SetAtmosObject(NativeAtmosTiles[i]);
            }
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < AtmosTiles.Count; i++)
            {
                NativeAtmosTiles[i] = AtmosTiles[i].GetAtmosObject();
            }
        }
    }
    
    //[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct SimulateFluxJob : IJobParallelFor
    {

        // todo : using NativeDisableParallelForRestriction is a dirty trick to avoid doing proper code. This might lead to race conditions.
        // The issue is that each jobs need to access atmos tile outside its set of indexes
        // Unity recommends using a so called double buffering methods.
        // https://github.com/korzen/Unity3D-JobsSystemAndBurstSamples/blob/master/Assets/JobsAndBurst/Scripts/DoubleBufferingBasics.cs

        
        [ReadOnly]
        public NativeArray<AtmosObject> TileObjectBuffer;

        // Unordered array of chunk keys, as they were created by the atmos map. Useful as two adjacent tiles objects might have very different positions if they're not in the same chunk.
        // Only adjacent tile objects in the same chunk can be retrieved without that, so its necessary for computing indexes in the TileObjectBuffer of neighbours on chunk edges.
        [ReadOnly]
        public NativeArray<int2> ChunkKeyBuffer;

        [NativeDisableParallelForRestriction]
        public NativeArray<MoleTransferToNeighbours> MoleTransfers;

        public int ChunkSize;

        public float DeltaTime;

        public void Execute(int index)
        {
            // TODO : We might need to set velocity of inactive atmosObject to 0 here ? Or maybe elsewhere, but velocity stays stuck sometimes on inactive atmosObject
            if (TileObjectBuffer[index].State != AtmosState.Active && TileObjectBuffer[index].State != AtmosState.Semiactive)
            {
                return;
            }

            List<AtmosObject> neigbhours = new();
            int northNeighbourIndex = GetNorthNeighbourIndex(index, TileObjectBuffer[index].ChunkKey);
            int southNeighbourIndex = GetSouthNeighbourIndex(index, TileObjectBuffer[index].ChunkKey);
            int westNeighbourIndex = GetWestNeighbourIndex(index, TileObjectBuffer[index].ChunkKey);
            int eastNeighbourIndex = GetEastNeighbourIndex(index, TileObjectBuffer[index].ChunkKey);

            List<int> neighboursIndexes = new();

            if (northNeighbourIndex != -1)
            {
                neighboursIndexes.Add(northNeighbourIndex);
                neigbhours.Add(TileObjectBuffer[northNeighbourIndex]);
            }
            if (southNeighbourIndex != -1)
            {
                neighboursIndexes.Add(southNeighbourIndex);
                neigbhours.Add(TileObjectBuffer[southNeighbourIndex]);
            }
            if (westNeighbourIndex != -1)
            {
                neighboursIndexes.Add(westNeighbourIndex);
                neigbhours.Add(TileObjectBuffer[westNeighbourIndex]);
            }
            if (eastNeighbourIndex != -1)
            {
                neighboursIndexes.Add(eastNeighbourIndex);
                neigbhours.Add(TileObjectBuffer[eastNeighbourIndex]);
            }
             
            // Do actual work
            MoleTransfers[index] = AtmosCalculator.SimulateGasTransfers(TileObjectBuffer[index], DeltaTime, neigbhours.ToArray(), index, neighboursIndexes.ToArray());

        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetWestNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (ChunkSize * ChunkSize);

            // case where element is not on first column 
            if (ownIndex % ChunkSize > 0)
            {
                return ownIndex - 1;
            }

            bool hasWestChunkKey = TryGetWestChunkKey(ownChunkKey, out int2 westChunkKey);

            if (!hasWestChunkKey)
            {
                return -1;
            }

            int westChunkFirstElementIndex = GetFirstElementIndexOfChunk(westChunkKey);

            // Return the element adjacent in west Chunk
            return westChunkFirstElementIndex + positionInChunk + (ChunkSize-1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetEastNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (ChunkSize * ChunkSize);

            // case where element is not on last column 
            if (ownIndex % ChunkSize < ChunkSize-1)
            {
                return ownIndex + 1;
            }

            bool hasEastChunkKey = TryGetEastChunkKey(ownChunkKey, out int2 eastChunkKey);

            if (!hasEastChunkKey)
            {
                return -1;
            }

            int eastChunkFirstElementIndex = GetFirstElementIndexOfChunk(eastChunkKey);

            // Return the element adjacent in east Chunk
            return eastChunkFirstElementIndex + positionInChunk - (ChunkSize-1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetNorthNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (ChunkSize * ChunkSize);

            // case where element is not on last row 
            if (ownIndex % (ChunkSize * ChunkSize) < ChunkSize * (ChunkSize-1))
            {
                return ownIndex + ChunkSize;
            }

            bool hasNorthChunkKey = TryGetNorthChunkKey(ownChunkKey, out int2 northChunkKey);

            if (!hasNorthChunkKey)
            {
                return -1;
            }

            int northChunkFirstElementIndex = GetFirstElementIndexOfChunk(northChunkKey);

            // Return the element adjacent in north Chunk
            return northChunkFirstElementIndex + positionInChunk - ChunkSize * (ChunkSize - 1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetSouthNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (ChunkSize * ChunkSize);

            // case where element is not on first row 
            if (ownIndex % (ChunkSize * ChunkSize) >= ChunkSize)
            {
                return ownIndex - ChunkSize;
            }

            bool hasSouthChunkKey = TryGetSouthChunkKey(ownChunkKey, out int2 southChunkKey);

            if (!hasSouthChunkKey)
            {
                return -1;
            }

            int southChunkFirstElementIndex = GetFirstElementIndexOfChunk(southChunkKey);

            // Return the element adjacent in south Chunk
            return southChunkFirstElementIndex + ChunkSize * (ChunkSize - 1) + positionInChunk;
        }

        private int GetFirstElementIndexOfChunk(int2 chunkKey)
        {
            for (int i = 0; i < ChunkKeyBuffer.Length; i++)
            {
                if (ChunkKeyBuffer[i].x == chunkKey.x && ChunkKeyBuffer[i].y == chunkKey.y)
                {
                    return ChunkSize * ChunkSize * i;
                }
            }

            return -1;
        }

        private bool TryGetChunkKey(int2 chunkKey, out int2 offsetChunkKey, int xOffset, int yOffset)
        {
            offsetChunkKey = default;
            for (int i = 0; i < ChunkKeyBuffer.Length; i++)
            {
                if (ChunkKeyBuffer[i].x == chunkKey.x + xOffset && ChunkKeyBuffer[i].y == chunkKey.y + yOffset)
                {
                    offsetChunkKey = new int2(chunkKey.x + xOffset, chunkKey.y + yOffset);
                    return true;
                }
            }
            return false;
        }

        private bool TryGetSouthChunkKey(int2 chunkKey, out int2 southChunkKey) => TryGetChunkKey(chunkKey, out southChunkKey, 0, -1);

        private bool TryGetNorthChunkKey(int2 chunkKey, out int2 northChunkKey) => TryGetChunkKey(chunkKey, out northChunkKey, 0, 1);

        private bool TryGetEastChunkKey(int2 chunkKey, out int2 eastChunkKey) => TryGetChunkKey(chunkKey, out eastChunkKey, 1, 0);

        private bool TryGetWestChunkKey(int2 chunkKey, out int2 westChunkKey)=> TryGetChunkKey(chunkKey, out westChunkKey, -1, 0);

    }
}