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
    public readonly struct MoleTransfer
    {
        public readonly float4 Moles;
        public readonly int IndexTo;

        public MoleTransfer(float4 moles, int indexTo)
        {
            Moles = moles;
            IndexTo = indexTo;
        }
    }

    public readonly struct MoleTransferToNeighbours
    {
        public readonly int IndexFrom;
        public readonly MoleTransfer TransferOne;
        public readonly MoleTransfer TransferTwo;
        public readonly  MoleTransfer TransferThree;
        public readonly MoleTransfer TransferFour;

        public MoleTransferToNeighbours(int indexFrom, MoleTransfer transferOne, MoleTransfer transferTwo, MoleTransfer transferThree, MoleTransfer transferFour)
        {
            IndexFrom = indexFrom;
            TransferOne = transferOne;
            TransferTwo = transferTwo;
            TransferThree = transferThree;
            TransferFour = transferFour;
        }
    }

}