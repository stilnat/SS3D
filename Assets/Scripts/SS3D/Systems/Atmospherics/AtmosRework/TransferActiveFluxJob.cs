using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SS3D.Engine.AtmosphericsRework
{

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct TransferActiveFluxJob : IJob
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;


        private NativeArray<AtmosObject> _tileObjectBuffer;

        public TransferActiveFluxJob(NativeArray<MoleTransferToNeighbours> moleTransfers, NativeArray<AtmosObject> tileObjectBuffer)
        {
            _moleTransfers = moleTransfers;
            _tileObjectBuffer = tileObjectBuffer;
        }

        public void Execute()
        {
            NativeArray<AtmosObject> copyAtmosTiles = _tileObjectBuffer;

            for (int i = 0; i < _moleTransfers.Length; i++)
            {
                MoleTransferToNeighbours transfer = _moleTransfers[i];
                int atmosObjectFromIndex = transfer.IndexFrom;

                // check the copy, as the active state might be changed by the adding and removal of gasses 
                if (copyAtmosTiles[atmosObjectFromIndex].State != AtmosState.Active)
                    continue;

                AtmosObject atmosObject = _tileObjectBuffer[atmosObjectFromIndex];
                atmosObject.RemoveCoreGasses(transfer.TransferOne.Moles, true);
                atmosObject.RemoveCoreGasses(transfer.TransferTwo.Moles, true);
                atmosObject.RemoveCoreGasses(transfer.TransferThree.Moles, true);
                atmosObject.RemoveCoreGasses(transfer.TransferFour.Moles, true);
                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;

                AtmosObject neighbourOne = _tileObjectBuffer[transfer.TransferOne.IndexTo];
                AtmosObject neighbourTwo = _tileObjectBuffer[transfer.TransferTwo.IndexTo];
                AtmosObject neighbourThree = _tileObjectBuffer[transfer.TransferThree.IndexTo];
                AtmosObject neighbourFour = _tileObjectBuffer[transfer.TransferFour.IndexTo];

                neighbourOne.AddCoreGasses(transfer.TransferOne.Moles, true);
                neighbourTwo.AddCoreGasses(transfer.TransferTwo.Moles, true);
                neighbourThree.AddCoreGasses(transfer.TransferThree.Moles, true);
                neighbourFour.AddCoreGasses(transfer.TransferFour.Moles, true);

                _tileObjectBuffer[transfer.TransferOne.IndexTo] = neighbourOne;
                _tileObjectBuffer[transfer.TransferTwo.IndexTo] = neighbourTwo;
                _tileObjectBuffer[transfer.TransferThree.IndexTo] = neighbourThree;
                _tileObjectBuffer[transfer.TransferFour.IndexTo] = neighbourFour;
            }
        }
    }
} 