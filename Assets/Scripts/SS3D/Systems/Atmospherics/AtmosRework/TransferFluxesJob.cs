using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Engine.AtmosphericsRework
{

    //[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct TransferFluxesJob : IJob
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;
        
        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        private NativeHashSet<int> _activeTransferIndex;

        private NativeArray<AtmosObject> _tileObjectBuffer;

        private NativeList<int> _activeIndexes;

        private readonly bool _diffusion;

        public TransferFluxesJob(NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes, NativeHashSet<int> activeTransferIndex, NativeList<int> activeIndexes, bool diffusion)
        {
            _moleTransfers = moleTransfers;
            _tileObjectBuffer = tileObjectBuffer;
            _diffusion = diffusion;
            _activeTransferIndex = activeTransferIndex;
            _neighboursIndexes = neighboursIndexes;
            _activeIndexes = activeIndexes;
        }

        public void Execute()
        {
            for (int i = 0; i < _activeIndexes.Length; i++)
            {
                int activeIndex = _activeIndexes[i];
                MoleTransferToNeighbours transfer = _moleTransfers[activeIndex];
                int atmosObjectFromIndex = transfer.IndexFrom;
                AtmosObject atmosObject = _tileObjectBuffer[atmosObjectFromIndex];
                atmosObject.RemoveCoreGasses(transfer.TransferMolesNorth, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesSouth, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesEast, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesWest, _diffusion);

                if (math.any(atmosObject.CoreGasses - _tileObjectBuffer[atmosObjectFromIndex].CoreGasses != 0))
                {
                    _activeTransferIndex.Add(atmosObjectFromIndex);
                }
                
                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;
                
                int neighbourNorthIndex = _neighboursIndexes[activeIndex].NorthNeighbour;
                int neighbourSouthIndex = _neighboursIndexes[activeIndex].SouthNeighbour;
                int neighbourEastIndex = _neighboursIndexes[activeIndex].EastNeighbour;
                int neighbourWestIndex = _neighboursIndexes[activeIndex].WestNeighbour;

                TransferToNeighbour(neighbourNorthIndex, transfer.TransferMolesNorth);
                TransferToNeighbour(neighbourSouthIndex, transfer.TransferMolesSouth);
                TransferToNeighbour(neighbourEastIndex, transfer.TransferMolesEast);
                TransferToNeighbour(neighbourWestIndex, transfer.TransferMolesWest);
            }
        }

        private void TransferToNeighbour(int neighbourIndex, float4 transfer)
        {
            if (neighbourIndex != -1)
            {
                AtmosObject neighbour = _tileObjectBuffer[neighbourIndex];
                neighbour.AddCoreGasses(transfer, _diffusion);
                if (math.any(transfer != 0))
                {
                    _activeTransferIndex.Add(neighbourIndex);
                }
                
                _tileObjectBuffer[neighbourIndex] = neighbour;
            }
        }
    }
} 