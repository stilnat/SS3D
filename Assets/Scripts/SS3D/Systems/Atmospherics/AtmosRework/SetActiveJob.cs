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
    //[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
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

            bool2 isActiveOrSemiActive = false;

            isActiveOrSemiActive |= TreatNeighbour(index, neighbourIndexes.NorthNeighbour);
            isActiveOrSemiActive |= TreatNeighbour(index, neighbourIndexes.SouthNeighbour);
            isActiveOrSemiActive |= TreatNeighbour(index, neighbourIndexes.EastNeighbour);
            isActiveOrSemiActive |= TreatNeighbour(index, neighbourIndexes.WestNeighbour);

            AtmosObject atmos = _tileObjectBuffer[index];

            if (isActiveOrSemiActive[0])
            {
                atmos.State = AtmosState.Active;
                _activeIndexes.Add(index);
            }
            else if(isActiveOrSemiActive[1])
            {
                atmos.State = AtmosState.Semiactive;
                _semiactiveIndexes.Add(index);
            }
            else
            {
                atmos.State = AtmosState.Inactive;
            }

            _tileObjectBuffer[index] = atmos;
        }

        private bool2 TreatNeighbour(int index, int neighbourIndex)
        {
            if (neighbourIndex == -1)
            {
                return false;
            }
            
            AtmosObject neighbour = _tileObjectBuffer[neighbourIndex];
            AtmosObject atmos = _tileObjectBuffer[index];

            if (neighbour.State == AtmosState.Blocked)
            {
                return false;
            }

            if (math.abs(atmos.Pressure - neighbour.Pressure) > GasConstants.pressureEpsilon)
            {
                return new(true, false);
            }
            
            float4 molesToTransfer = (atmos.CoreGasses - neighbour.CoreGasses) * GasConstants.gasDiffusionRate;
            if (math.any(molesToTransfer > GasConstants.fluxEpsilon))
            {
                return new(false, true);
            }

            return false;
        }
    }
}