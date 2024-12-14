using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that are not centred on a tile.
    /// Used by the non-centered pipe layers.
    /// </summary>
    [Serializable]
    public struct OffsetConnector : IMeshAndDirectionResolver
    {
        public enum OffsetOrientation
        {
            O = 0,
            UNorth = 1,
            USouth = 2,
            I = 3,
            LNe = 4,
            LNw = 5,
            LSe = 6,
            LSW = 7,
            TNEW = 8,
            TNSW = 9,
            TNSE = 10,
            TSWE = 11,
            X = 12,
        }

        [FormerlySerializedAs("o")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh O;

        [FormerlySerializedAs("uNorth")]
        [Tooltip("A mesh where the North edge is connected, can be rotated to the East")]
        public Mesh UNorth;

        [FormerlySerializedAs("uSouth")]
        [Tooltip("A mesh where the South edge is connected, can be rotated to the West")]
        public Mesh USouth;

        [FormerlySerializedAs("i")]
        [Tooltip("A mesh where North & South edges are connected")]
        public Mesh I;

        [FormerlySerializedAs("lNE")]
        [Tooltip("A mesh where the North & East edges are connected")]
        public Mesh LNe;

        [FormerlySerializedAs("lNW")]
        [Tooltip("A mesh where the North & West edges are connected")]
        public Mesh LNw;

        [FormerlySerializedAs("lSE")]
        [Tooltip("A mesh where the South & East edges are connected")]
        public Mesh LSe;

        [FormerlySerializedAs("lSW")]
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh LSW;

        [FormerlySerializedAs("tSWE")]
        [Tooltip("A mesh where the South, West, & East edges are connected")]
        public Mesh TSwe;

        [FormerlySerializedAs("tNEW")]
        [Tooltip("A mesh where the North, East, & West edges are connected")]
        public Mesh TNew;

        [FormerlySerializedAs("tNSW")]
        [Tooltip("A mesh where the North, South, & West edges are connected")]
        public Mesh TNsw;

        [FormerlySerializedAs("tNSE")]
        [Tooltip("A mesh where the North, South, & East edges are connected")]
        public Mesh TNse;

        [FormerlySerializedAs("x")]
        [Tooltip("A mesh where all edges are connected")]
        public Mesh X;

        private OffsetOrientation _orientation;

        public OffsetOrientation GetOrientation() => _orientation;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);
            switch (shape)
            {
                case AdjacencyShape.O:
                {
                    mesh = O;
                    _orientation = OffsetOrientation.O;
                    break;
                }

                case AdjacencyShape.UNorth:
                {
                    mesh = UNorth;
                    _orientation = OffsetOrientation.UNorth;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;
                }

                case AdjacencyShape.USouth:
                {
                    mesh = USouth;
                    _orientation = OffsetOrientation.USouth;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleConnection());
                    break;
                }

                case AdjacencyShape.I:
                {
                    mesh = I;
                    _orientation = OffsetOrientation.I;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                }

                case AdjacencyShape.LNorthWest:
                {
                    mesh = LNw;
                    _orientation = OffsetOrientation.LNw;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LNorthEast:
                {
                    mesh = LNe;
                    _orientation = OffsetOrientation.LSe;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LSouthEast:
                {
                    mesh = LSe;
                    _orientation = OffsetOrientation.LSW;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LSouthWest:
                {
                    mesh = LSW;
                    _orientation = OffsetOrientation.LNw;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthSouthEast:
                {
                    mesh = TNse;
                    _orientation = OffsetOrientation.TSWE;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TSouthWestEast:
                {
                    mesh = TSwe;
                    _orientation = OffsetOrientation.TNSW;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthSouthWest:
                {
                    mesh = TNsw;
                    _orientation = OffsetOrientation.TNEW;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthEastWest:
                {
                    mesh = TNew;
                    _orientation = OffsetOrientation.TNSE;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.X:
                {
                    mesh = X;
                    _orientation = OffsetOrientation.X;
                    rotation = 90;
                    break;
                }

                default:
                {
                    Debug.LogError($"Received unexpected shape from offset shape resolver: {shape}");
                    mesh = O;
                    break;
                }
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }
    }
}
