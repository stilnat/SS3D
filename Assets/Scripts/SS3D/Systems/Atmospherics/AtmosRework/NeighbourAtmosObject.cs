using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct NeighbourAtmosObject
    {
        public IAtmosLoop AtmosObject { get;}
        public Direction Direction { get; }
        public NeighbourAtmosObject(IAtmosLoop atmosObject, Direction direction)
        {
            AtmosObject = atmosObject;
            Direction = direction;
        }
    }
}