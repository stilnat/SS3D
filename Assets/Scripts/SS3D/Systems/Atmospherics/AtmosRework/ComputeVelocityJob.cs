using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ComputeVelocityJob : IJobParallelFor
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private NativeArray<AtmosObject> _tileObjectBuffer;
        
        public ComputeVelocityJob(NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<MoleTransferToNeighbours> moleTransfers)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _moleTransfers = moleTransfers;
        }

        public void Execute(int index)
        {
            AtmosObject atmosObject = _tileObjectBuffer[index];
            atmosObject.VelocityNorth = math.csum(_moleTransfers[index].TransferMolesNorth * GasConstants.coreGasDensity);
            atmosObject.VelocitySouth = math.csum(_moleTransfers[index].TransferMolesSouth * GasConstants.coreGasDensity);
            atmosObject.VelocityEast = math.csum(_moleTransfers[index].TransferMolesEast * GasConstants.coreGasDensity);
            atmosObject.VelocityWest = math.csum(_moleTransfers[index].TransferMolesWest * GasConstants.coreGasDensity);
            _tileObjectBuffer[index] = atmosObject;
        }
    }
}
