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

        public MoleTransfer(float4 moles, int indexTo)
        {
            Moles = moles;
            IndexTo = indexTo;
        }
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
    
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct SimulateFluxJob : IJobParallelFor
    {

        // todo : using NativeDisableParallelForRestriction is a dirty trick to avoid doing proper code. This might lead to race conditions.
        // The issue is that each jobs need to access atmos tile outside its set of indexes
        // Unity recommends using a so called double buffering methods.
        // https://github.com/korzen/Unity3D-JobsSystemAndBurstSamples/blob/master/Assets/JobsAndBurst/Scripts/DoubleBufferingBasics.cs
        
        [ReadOnly]
        private readonly NativeArray<AtmosObject> _tileObjectBuffer;

        // Hashmap of chunk keys, with values indicating order as they were created by the atmos map. Useful as two adjacent tiles objects might have very different positions if they're not in the same chunk.
        // Only adjacent tile objects in the same chunk can be retrieved without that, so its necessary for computing indexes in the TileObjectBuffer of neighbours on chunk edges.
        [ReadOnly]
        private readonly NativeHashMap<int2, int> _chunkKeyHashMap;

        [NativeDisableParallelForRestriction]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private readonly int _chunkSize;

        private readonly float _deltaTime;

        public SimulateFluxJob(NativeArray<AtmosObject> tileObjectBuffer,  NativeHashMap<int2, int> chunkKeyHashMap, NativeArray<MoleTransferToNeighbours> moleTransfers, int chunkSize, float deltaTime)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _chunkSize = chunkSize;
            _deltaTime = deltaTime;
            _moleTransfers = moleTransfers;
            _chunkKeyHashMap = chunkKeyHashMap;
        }

        public void Execute(int index)
        {
            // TODO : We might need to set velocity of inactive atmosObject to 0 here ? Or maybe elsewhere, but velocity stays stuck sometimes on inactive atmosObject
            if (_tileObjectBuffer[index].State != AtmosState.Active && _tileObjectBuffer[index].State != AtmosState.Semiactive)
            {
                return;
            }

            NativeArray<int> neighboursIndexes = new(4, Allocator.Temp);
            neighboursIndexes[0] = GetNorthNeighbourIndex(index, _tileObjectBuffer[index].ChunkKey);
            neighboursIndexes[1] = GetSouthNeighbourIndex(index, _tileObjectBuffer[index].ChunkKey);
            neighboursIndexes[2] = GetWestNeighbourIndex(index, _tileObjectBuffer[index].ChunkKey);
            neighboursIndexes[3] = GetEastNeighbourIndex(index, _tileObjectBuffer[index].ChunkKey);
            int neighbourCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (neighboursIndexes[i] != -1)
                {
                    neighbourCount++;
                }
            }

            NativeArray<int> realNeighboursIndexes = new(neighbourCount, Allocator.Temp);
            NativeArray<AtmosObject> neigbhours = new(neighbourCount, Allocator.Temp);

            int j = 0;
            for (int i = 0; i < 4; i++)
            {
                if (neighboursIndexes[i] != -1)
                {
                    realNeighboursIndexes[j] = neighboursIndexes[i];
                    neigbhours[j] = _tileObjectBuffer[neighboursIndexes[i]];
                    j++;
                }
            }
             
            // Do actual work
            _moleTransfers[index] = AtmosCalculator.SimulateGasTransfers(_tileObjectBuffer[index], _deltaTime, neigbhours, index, realNeighboursIndexes);
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
            return westChunkFirstElementIndex + positionInChunk + (_chunkSize-1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetEastNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on last column 
            if (ownIndex % _chunkSize < _chunkSize-1)
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
            return eastChunkFirstElementIndex + positionInChunk - (_chunkSize-1);
        }

        // Assumes first element of chunk is in the south-west corner, and last one in north east.
        private int GetNorthNeighbourIndex(int ownIndex, int2 ownChunkKey)
        {
            int positionInChunk = ownIndex % (_chunkSize * _chunkSize);

            // case where element is not on last row 
            if (ownIndex % (_chunkSize * _chunkSize) < _chunkSize * (_chunkSize-1))
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

            offsetChunkKey = new int2(chunkKey.x + xOffset, chunkKey.y + yOffset);;

            return true;

        }

        private bool TryGetSouthChunkKey(int2 chunkKey, out int2 southChunkKey) => TryGetChunkKey(chunkKey, out southChunkKey, 0, -1);

        private bool TryGetNorthChunkKey(int2 chunkKey, out int2 northChunkKey) => TryGetChunkKey(chunkKey, out northChunkKey, 0, 1);

        private bool TryGetEastChunkKey(int2 chunkKey, out int2 eastChunkKey) => TryGetChunkKey(chunkKey, out eastChunkKey, 1, 0);

        private bool TryGetWestChunkKey(int2 chunkKey, out int2 westChunkKey)=> TryGetChunkKey(chunkKey, out westChunkKey, -1, 0);

    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct TransferGasJob : IJob
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;


        private NativeArray<AtmosObject> _tileObjectBuffer;

        public TransferGasJob(NativeArray<MoleTransferToNeighbours> moleTransfers,  NativeArray<AtmosObject> tileObjectBuffer)
        {
            _moleTransfers = moleTransfers;
            _tileObjectBuffer = tileObjectBuffer;
        }

        public void Execute()
        {
            NativeArray<AtmosObject> copyAtmosTiles = _tileObjectBuffer;

            for (int i = 0; i < _moleTransfers.Length; i++)
            {
                int atmosObjectFromIndex = _moleTransfers[i].IndexFrom;

                // check the copy, as the active state might be changed by the adding and removal of gasses 
                if (copyAtmosTiles[atmosObjectFromIndex].State != AtmosState.Active)
                    continue;

                AtmosObject atmosObject = _tileObjectBuffer[atmosObjectFromIndex];
                atmosObject.RemoveCoreGasses(_moleTransfers[i].TransferOne.Moles);
                atmosObject.RemoveCoreGasses(_moleTransfers[i].TransferTwo.Moles); 
                atmosObject.RemoveCoreGasses(_moleTransfers[i].TransferThree.Moles);
                atmosObject.RemoveCoreGasses(_moleTransfers[i].TransferFour.Moles);
                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;

                AtmosObject neighbourOne = _tileObjectBuffer[_moleTransfers[i].TransferOne.IndexTo];
                AtmosObject neighbourTwo = _tileObjectBuffer[_moleTransfers[i].TransferTwo.IndexTo];
                AtmosObject neighbourThree = _tileObjectBuffer[_moleTransfers[i].TransferThree.IndexTo];
                AtmosObject neighbourFour = _tileObjectBuffer[_moleTransfers[i].TransferFour.IndexTo];

                neighbourOne.AddCoreGasses(_moleTransfers[i].TransferOne.Moles);
                neighbourTwo.AddCoreGasses(_moleTransfers[i].TransferTwo.Moles);
                neighbourThree.AddCoreGasses(_moleTransfers[i].TransferThree.Moles);
                neighbourFour.AddCoreGasses(_moleTransfers[i].TransferFour.Moles);

                _tileObjectBuffer[_moleTransfers[i].TransferOne.IndexTo] = neighbourOne;
                _tileObjectBuffer[_moleTransfers[i].TransferTwo.IndexTo] = neighbourTwo;
                _tileObjectBuffer[_moleTransfers[i].TransferThree.IndexTo] = neighbourThree;
                _tileObjectBuffer[_moleTransfers[i].TransferFour.IndexTo] = neighbourFour;
            }
        }
    } 
}