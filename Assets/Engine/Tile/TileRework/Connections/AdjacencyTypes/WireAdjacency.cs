using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    /// <summary>
    /// Adjacency type used for objects that do not require complex connections.
    /// </summary>
    [Serializable]
    public struct WireConnector
    {
        
        public Mesh o;
        public Mesh u1;
        public Mesh u2;
        public Mesh broken1;
        public Mesh broken2;
        public Mesh v;
        public Mesh i1;
        public Mesh i2;
        public Mesh l1;
        public Mesh l2;
        public Mesh l3;
        public Mesh l4;
        public Mesh l5;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyBitmap adjacents)
        {
            // Count number of connections along cardinal (which is all that we use atm)
            var cardinalInfo = adjacents.GetCardinalInfo();

            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            if (cardinalInfo.IsO())
                mesh = o;
            else if (cardinalInfo.IsU())
            {
                mesh = u1;
                rotation = TileHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i1;
                rotation = TileHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                mesh = l1;
                rotation = TileHelper.AngleBetween(Direction.NorthEast, cardinalInfo.GetCornerDirection());
            }
            else
            {
                mesh = l2;
            }

            return new MeshDirectionInfo { mesh = mesh, rotation = rotation };
        }
    }
}