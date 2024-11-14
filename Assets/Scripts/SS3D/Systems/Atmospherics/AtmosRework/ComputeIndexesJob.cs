using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct ComputeIndexesJob : IJobParallelFor
    {
        private NativeArray<AtmosObjectNeighboursIndexes> _neighbourIndexes;

        [ReadOnly]
        private NativeArray<AtmosObject> _nativeAtmosTiles;

        /// <summary>
        /// Contains Chunk keys and the order in which they were created on the tilemap, used for efficient look up for neighbour tiles in jobs.
        /// </summary>
        [ReadOnly]
        private readonly NativeHashMap<int2, int> _chunkKeyHashMap;

        private int _chunkSize;

        public ComputeIndexesJob(NativeArray<AtmosObjectNeighboursIndexes> neighbourIndexes, NativeArray<AtmosObject> nativeAtmosTiles, NativeHashMap<int2, int> chunkKeyHashMap, int chunkSize)
        {
            _neighbourIndexes = neighbourIndexes;
            _nativeAtmosTiles = nativeAtmosTiles;
            _chunkKeyHashMap = chunkKeyHashMap;
            _chunkSize = chunkSize;
        }

        public void Execute(int index)
        {
            int neighbourNorth = GetNorthNeighbourIndex(index, _nativeAtmosTiles[index].ChunkKey);
            int neighbourSouth = GetSouthNeighbourIndex(index, _nativeAtmosTiles[index].ChunkKey);
            int neighbourWest = GetWestNeighbourIndex(index, _nativeAtmosTiles[index].ChunkKey);
            int neighbourEast = GetEastNeighbourIndex(index, _nativeAtmosTiles[index].ChunkKey);
            AtmosObjectNeighboursIndexes indexes = new(neighbourNorth, neighbourSouth, neighbourEast, neighbourWest);
            _neighbourIndexes[index] = indexes;
        }

          // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetWestNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on first column 
            if (ownIndex % _chunkSize > 0)
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
            return westChunkFirstElementIndex + positionInChunk + (_chunkSize - 1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetEastNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on last column 
            if (ownIndex % _chunkSize < _chunkSize - 1)
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
            return eastChunkFirstElementIndex + positionInChunk - (_chunkSize - 1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetNorthNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on last row 
            if (ownIndex % (_chunkSize * _chunkSize) < _chunkSize * (_chunkSize - 1))
            {
                return ownIndex + _chunkSize;
            }

            bool hasNorthChunkKey = TryGetNorthChunkKey(ownChunkKey, out int2 northChunkKey);

            if (!hasNorthChunkKey)
            {
                return -1;
            }

            int northChunkFirstElementIndex = GetFirstElementIndexOfChunk(northChunkKey);

            // Return the element adjacent in north Chunk
            return northChunkFirstElementIndex + positionInChunk - _chunkSize * (_chunkSize - 1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetSouthNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on first row 
            if (ownIndex % (_chunkSize * _chunkSize) >= _chunkSize)
            {
                return ownIndex - _chunkSize;
            }

            bool hasSouthChunkKey = TryGetSouthChunkKey(ownChunkKey, out int2 southChunkKey);

            if (!hasSouthChunkKey)
            {
                return -1;
            }

            int southChunkFirstElementIndex = GetFirstElementIndexOfChunk(southChunkKey);

            // Return the element adjacent in south Chunk
            return southChunkFirstElementIndex + _chunkSize * (_chunkSize - 1) + positionInChunk;
        }

        private int GetFirstElementIndexOfChunk(int2 chunkKey)
        {
            if (!_chunkKeyHashMap.TryGetValue(chunkKey, out int index))
                return -1;

            return _chunkSize * _chunkSize * index;
        }

        private bool TryGetChunkKey(int2 chunkKey, out int2 offsetChunkKey, int xOffset, int yOffset)
        {
            offsetChunkKey = default;

            if (!_chunkKeyHashMap.TryGetValue(chunkKey + new int2(xOffset, yOffset), out int index))
                return false;

            offsetChunkKey = new int2(chunkKey.x + xOffset, chunkKey.y + yOffset);
            ;

            return true;

        }

        private bool TryGetSouthChunkKey(int2 chunkKey, out int2 southChunkKey) => TryGetChunkKey(chunkKey, out southChunkKey, 0, -1);

        private bool TryGetNorthChunkKey(int2 chunkKey, out int2 northChunkKey) => TryGetChunkKey(chunkKey, out northChunkKey, 0, 1);

        private bool TryGetEastChunkKey(int2 chunkKey, out int2 eastChunkKey) => TryGetChunkKey(chunkKey, out eastChunkKey, 1, 0);

        private bool TryGetWestChunkKey(int2 chunkKey, out int2 westChunkKey) => TryGetChunkKey(chunkKey, out westChunkKey, -1, 0);
    }
}