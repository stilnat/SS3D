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

        private NativeList<int> _activeIndexes;

        private NativeList<int> _semiactiveIndexes;
        
        public SetActiveJob(NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes,
            NativeList<int> activeIndexes,
            NativeList<int> semiactiveIndexes)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _neighboursIndexes = neighboursIndexes;
            _activeIndexes = activeIndexes;
            _semiactiveIndexes = semiactiveIndexes;
        }

        public void Execute(int index)
        {
            if (_tileObjectBuffer[index].State == AtmosState.Blocked)
            {
                return;
            }
            AtmosObjectNeighboursIndexes neighbourIndexes = _neighboursIndexes[index];

            if (_tileObjectBuffer[index].State == AtmosState.Active)
            {
                _activeIndexes.Add(index);
                return;
            }

            TreatNeighbour(index, neighbourIndexes.NorthNeighbour);
            if (_tileObjectBuffer[index].State == AtmosState.Active) return;

            TreatNeighbour(index, neighbourIndexes.SouthNeighbour);
            if (_tileObjectBuffer[index].State == AtmosState.Active) return;

            TreatNeighbour(index, neighbourIndexes.EastNeighbour);
            if (_tileObjectBuffer[index].State == AtmosState.Active) return;

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

            if (neighbour.State == AtmosState.Blocked || neighbour.State == AtmosState.Inactive)
            {
                return;
            }

            if (math.abs(atmos.Pressure - neighbour.Pressure) > GasConstants.pressureEpsilon)
            {
                atmos.State = AtmosState.Active;
                _tileObjectBuffer[index] = atmos;
                _activeIndexes.Add(index);
                return;
            }
            
            float4 molesToTransfer = (atmos.CoreGasses - neighbour.CoreGasses) * GasConstants.gasDiffusionRate;
            if (math.any(molesToTransfer > GasConstants.fluxEpsilon))
            {
                atmos.State = AtmosState.Semiactive;
                _tileObjectBuffer[index] = atmos;
                _semiactiveIndexes.Add(index);
            }
        }
    }
}