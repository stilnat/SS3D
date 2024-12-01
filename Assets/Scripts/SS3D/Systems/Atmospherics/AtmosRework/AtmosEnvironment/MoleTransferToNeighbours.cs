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
    public readonly struct MoleTransferToNeighbours
    {
        public readonly int IndexFrom;
        public readonly float4 TransferMolesNorth;
        public readonly float4 TransferMolesSouth;
        public readonly float4 TransferMolesEast;
        public readonly float4 TransferMolesWest;

        public MoleTransferToNeighbours(int indexFrom, float4 transferMolesNorth, float4 transferMolesSouth, float4 transferMolesEast, float4 transferMolesWest)
        {
            IndexFrom = indexFrom;
            TransferMolesNorth = transferMolesNorth;
            TransferMolesSouth = transferMolesSouth;
            TransferMolesEast = transferMolesEast;
            TransferMolesWest = transferMolesWest;
        }
    }
}