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

        private readonly bool _diffusion;

        public TransferActiveFluxJob(NativeArray<MoleTransferToNeighbours> moleTransfers, NativeArray<AtmosObject> tileObjectBuffer, bool diffusion)
        {
            _moleTransfers = moleTransfers;
            _tileObjectBuffer = tileObjectBuffer;
            _diffusion = diffusion;
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
                atmosObject.RemoveCoreGasses(transfer.TransferOne.Moles, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferTwo.Moles, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferThree.Moles, _diffusion);
                atmosObject.RemoveCoreGasses(transfer.TransferFour.Moles, _diffusion);
                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;

                AtmosObject neighbourOne = _tileObjectBuffer[transfer.TransferOne.IndexTo];
                AtmosObject neighbourTwo = _tileObjectBuffer[transfer.TransferTwo.IndexTo];
                AtmosObject neighbourThree = _tileObjectBuffer[transfer.TransferThree.IndexTo];
                AtmosObject neighbourFour = _tileObjectBuffer[transfer.TransferFour.IndexTo];

                neighbourOne.AddCoreGasses(transfer.TransferOne.Moles, _diffusion);
                neighbourTwo.AddCoreGasses(transfer.TransferTwo.Moles, _diffusion);
                neighbourThree.AddCoreGasses(transfer.TransferThree.Moles, _diffusion);
                neighbourFour.AddCoreGasses(transfer.TransferFour.Moles, _diffusion);

                _tileObjectBuffer[transfer.TransferOne.IndexTo] = neighbourOne;
                _tileObjectBuffer[transfer.TransferTwo.IndexTo] = neighbourTwo;
                _tileObjectBuffer[transfer.TransferThree.IndexTo] = neighbourThree;
                _tileObjectBuffer[transfer.TransferFour.IndexTo] = neighbourFour;
            }
        }
    }
} 