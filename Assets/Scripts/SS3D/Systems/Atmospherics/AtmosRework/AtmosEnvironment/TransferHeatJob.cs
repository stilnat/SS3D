using SS3D.Engine.AtmosphericsRework;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
public struct TransferHeatJob : IJob
{
        [ReadOnly]
        private NativeArray<HeatTransferToNeighbours> _heatTransfers;
        
        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        private NativeArray<AtmosObject> _tileObjectBuffer;

        [ReadOnly]
        private NativeArray<int> _activeIndexes;

        public TransferHeatJob(NativeArray<HeatTransferToNeighbours> heatTransfers,
            NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes, NativeArray<int> activeIndexes)
        {
            _heatTransfers = heatTransfers;
            _tileObjectBuffer = tileObjectBuffer;
            _activeIndexes = activeIndexes;
            _neighboursIndexes = neighboursIndexes;
        }

        public void Execute()
        {
            for (int i = 0; i < _activeIndexes.Length; i++)
            {
                int activeIndex = _activeIndexes[i];
                HeatTransferToNeighbours transfer = _heatTransfers[activeIndex];
                int atmosObjectFromIndex = transfer.IndexFrom;
                AtmosObject atmosObject = _tileObjectBuffer[atmosObjectFromIndex];

                atmosObject.RemoveHeat(transfer.TransferHeat[0]);
                atmosObject.RemoveHeat(transfer.TransferHeat[1]);
                atmosObject.RemoveHeat(transfer.TransferHeat[2]);
                atmosObject.RemoveHeat(transfer.TransferHeat[3]);
                
                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;
                
                int neighbourNorthIndex = _neighboursIndexes[activeIndex].NorthNeighbour;
                int neighbourSouthIndex = _neighboursIndexes[activeIndex].SouthNeighbour;
                int neighbourEastIndex = _neighboursIndexes[activeIndex].EastNeighbour;
                int neighbourWestIndex = _neighboursIndexes[activeIndex].WestNeighbour;

                TransferToNeighbour(neighbourNorthIndex, transfer.TransferHeat[0]);
                TransferToNeighbour(neighbourSouthIndex, transfer.TransferHeat[1]);
                TransferToNeighbour(neighbourEastIndex, transfer.TransferHeat[2]);
                TransferToNeighbour(neighbourWestIndex, transfer.TransferHeat[3]);
            }
        }

        private void TransferToNeighbour(int neighbourIndex, float transfer)
        {
            if (neighbourIndex == -1)
                return;

            AtmosObject neighbour = _tileObjectBuffer[neighbourIndex];
            neighbour.AddHeat(transfer);
            _tileObjectBuffer[neighbourIndex] = neighbour;
        }
}