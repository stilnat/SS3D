using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct SetActiveJob : IJobParallelFor
    {
                
        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;
        
        [NativeDisableParallelForRestriction]
        private NativeArray<AtmosObject> _tileObjectBuffer;
        
        public SetActiveJob(NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _neighboursIndexes = neighboursIndexes;
        }

        public void Execute(int index)
        {
            AtmosObjectNeighboursIndexes neighbourIndexes = _neighboursIndexes[index];

            TreatNeighbour(index, neighbourIndexes.NorthNeighbour);
            TreatNeighbour(index, neighbourIndexes.SouthNeighbour);
            TreatNeighbour(index, neighbourIndexes.EastNeighbour);
            TreatNeighbour(index, neighbourIndexes.WestNeighbour);
        }

        private void TreatNeighbour(int index, int neighbourIndex)
        {
            if (neighbourIndex == -1)
            {
                return;
            }
            
            AtmosObject neighbour = _tileObjectBuffer[neighbourIndex];
            AtmosObject atmos = _tileObjectBuffer[index];

            if (neighbour.State == AtmosState.Blocked || neighbour.State == AtmosState.Inactive
            || atmos.State == AtmosState.Active || atmos.State == AtmosState.Blocked)
            {
                return;
            }

            if (math.abs(atmos.Pressure - neighbour.Pressure) > GasConstants.pressureEpsilon)
            {
                atmos.State = AtmosState.Active;
                _tileObjectBuffer[index] = atmos;
                return;
            }
            
            float4 molesToTransfer = (atmos.CoreGasses - neighbour.CoreGasses) * GasConstants.gasDiffusionRate;
            if (math.any(molesToTransfer > GasConstants.fluxEpsilon))
            {
                atmos.State = AtmosState.Semiactive;
                _tileObjectBuffer[index] = atmos;
            }
        }
    }
}
