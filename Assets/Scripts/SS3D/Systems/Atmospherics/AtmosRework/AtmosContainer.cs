using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosContainer
    {

        public AtmosObject AtmosObject;
        public AtmosMap Map { get; }

        public int X { get; }

        public int Y { get; }

        public AtmosChunk Chunk { get; }

        public TileLayer Layer { get; }

        public AtmosContainer(AtmosMap map, AtmosChunk chunk, int x, int y, TileLayer layer, float volume)
        {
            Map = map;
            Chunk = chunk;
            X = x;
            Y = y;
            Layer = layer;
            AtmosObject = new(new(chunk.GetKey().x, chunk.GetKey().y), volume);
        }

        public void Initialize()
        {
            if(Layer == TileLayer.Turf)
            {
                InitializeEnvironment();
            }
            else
            {
               InitializePipe(); 
            }
        }

        private void InitializeEnvironment()
        {
            // Set blocked or vacuum if there is a wall or there is no plenum
            ITileLocation plenumLayerTile = Map.GetLinkedTileMap().GetTileLocation(TileLayer.Plenum, GetWorldPosition());

            ITileLocation turfLayerTile = Map.GetLinkedTileMap().GetTileLocation(TileLayer.Turf, GetWorldPosition());

            // Set air if there's Plenum and no walls on top.
            // todo : should make a difference between fully build walls and stuff like girders
            if (!plenumLayerTile.IsFullyEmpty() && (turfLayerTile.IsFullyEmpty() || turfLayerTile.TryGetPlacedObject(out PlacedTileObject placedObject) && placedObject.GenericType != TileObjectGenericType.Wall))
            {
                // Set to default air mixture
                AtmosObject.MakeAir();
            }

            // if no plenum, then put vacuum
            if (plenumLayerTile.IsFullyEmpty())
            {
                AtmosObject.MakeEmpty();
                AtmosObject.SetVacuum();
                AtmosObject.SetTemperature(173); // -100 C for space
            }

            // Set blocked with a wall
            if (!turfLayerTile.IsFullyEmpty() && turfLayerTile.TryGetPlacedObject(out placedObject) && placedObject.GenericType == TileObjectGenericType.Wall)
            {
                AtmosObject.MakeEmpty();
                AtmosObject.SetBlocked();
            }
        }

        private void InitializePipe()
        {
            AtmosObject.MakeEmpty();
            AtmosObject.SetVacuum();
            AtmosObject.SetBlocked();

            if(Map.GetLinkedTileMap().GetTileLocation(Layer, GetWorldPosition()) is not SingleTileLocation singleTileLocation)
            {
                return;
            }

            if (!singleTileLocation.TryGetPlacedObject(out PlacedTileObject placedObject))
            {
                return;
            }

            // If here, should have found a pipe and then we can unblock it 
            AtmosObject.SetInactive();
        } 

        public Vector3 GetWorldPosition()
        {
            return Chunk.GetWorldPosition(X, Y);
        }
    }
}