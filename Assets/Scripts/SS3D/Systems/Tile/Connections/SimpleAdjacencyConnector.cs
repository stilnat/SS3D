﻿using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Basic connector using the simple connector struct for resolving shape and direction.
    /// Things do not need special connections in corners.
    /// The only condition to connect to a neighbour is that they share generic and specific type.
    /// </summary>
    public class SimpleAdjacencyConnector : AbstractHorizontalConnector
    {
        [FormerlySerializedAs("simpleAdjacency")]
        [SerializeField]
        private SimpleConnector _simpleAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _simpleAdjacency;

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            bool isConnected = false;
            if (neighbourObject != null)
            {
                isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
                isConnected &= neighbourObject.GenericType == GenericType || GenericType == TileObjectGenericType.None;
                isConnected &= neighbourObject.SpecificType == SpecificType || SpecificType == TileObjectSpecificType.None;
            }

            return isConnected;
        }
    }
}
