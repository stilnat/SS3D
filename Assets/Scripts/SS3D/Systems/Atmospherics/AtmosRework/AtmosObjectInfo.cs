using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct AtmosObjectInfo
    {
        public AtmosContainer Container;
        public AtmosState State;
        public float2 Velocity;
        public int BufferIndex;

        public AtmosObjectInfo(AtmosState state, AtmosContainer container, float2 velocity, int bufferIndex)
        {
            State = state;
            Container = container;
            Velocity = velocity;
            BufferIndex = bufferIndex;
        }
    }
}