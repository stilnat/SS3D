﻿using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for disposal furniture. e.g disposal outlets and disposal bins.
    /// It doesn't do much, except checking for an available pipe just below itself, and
    /// signaling this pipe to update itself upon adding or clearing the disposal furniture.
    /// </summary>
    public class DisposalFurnitureConnector : NetworkActor, IAdjacencyConnector
    {
        private PlacedTileObject _placedObject;

        private bool _connectedToPipe;

        public List<PlacedTileObject> GetConnectedNeighbours()
        {
            return GetNeighbours().Where(IsConnected).ToList();
        }

        /// <summary>
        /// Disposal furnitures are connected when they have a disposal pipe just below them
        /// and this pipe has less than two connections already.
        /// </summary>
        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (!neighbourObject || !neighbourObject.TryGetComponent<DisposalPipeAdjacencyConnector>(out DisposalPipeAdjacencyConnector pipeConnector))
            {
                return false;
            }

            Vector2Int neighbourPosition = neighbourObject.Origin;

            return neighbourPosition == _placedObject.Origin && pipeConnector.HorizontalConnectionCount < 2;
        }

        public void UpdateAllConnections()
        {
            Setup();

            // update pipe just below.
            if (TryGetPipeBelow(out PlacedTileObject pipe))
            {
                UpdateSingleConnection(Direction.North, pipe, true);
            }
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool updated = _connectedToPipe != isConnected;

            if (updated)
            {
                neighbourObject.UpdateAdjacencies();
            }

            return updated;
        }

        /// <summary>
        /// The only neighbour of a disposal furniture is the eventual disposal pipe just below it.
        /// </summary>
        public List<PlacedTileObject> GetNeighbours()
        {
            bool hasNeighbour = TryGetPipeBelow(out PlacedTileObject pipe);

            if (hasNeighbour)
            {
                return new() { pipe };
            }

            return new();
        }

        private void Setup()
        {
            _placedObject = GetComponent<PlacedTileObject>();
        }

        /// <summary>
        /// Just get the pipe below the disposal furniture if it exists.
        /// </summary>
        private bool TryGetPipeBelow(out PlacedTileObject pipe)
        {
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            TileMap map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            SingleTileLocation pipeLocation = (SingleTileLocation)currentChunk.GetTileLocation(TileLayer.Disposal, _placedObject.Origin.x, _placedObject.Origin.y);
            pipe = pipeLocation.PlacedObject;

            return pipe != null;
        }
    }
}
