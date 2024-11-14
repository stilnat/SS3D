using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class TileAtmosObject
    {
        public enum AtmosSaveState
        {
            Air = 0,    // Default air mixture
            Vacuum = 1, // Default vacuum emtpy tile
            Mix = 2,    // Custom mix of gasses is present
        }

        public struct MixSaveState
        {
            float4 gasses;
            float temperature;

            public MixSaveState(float4 gasses, float temperature)
            {
                this.gasses = gasses;
                this.temperature = temperature;
            }
        }

        /// <summary>
        /// Save object used for reconstructing a TileObject.
        /// </summary>
        [Serializable]
        public class AtmosSaveObject
        {
            public int x;
            public int y;
            public AtmosSaveState state;
            public MixSaveState mix;
        }

        public AtmosObject AtmosObject;
        public AtmosMap Map { get; }

        public int X { get; }

        public int Y { get; }

        public AtmosChunk Chunk { get; }

        public TileAtmosObject(AtmosMap map, AtmosChunk chunk, int x, int y)
        {
            Map = map;
            Chunk = chunk;
            X = x;
            Y = y;

            AtmosObject = new(new(chunk.GetKey().x, chunk.GetKey().y));
        }

        public void Initialize(TileMap tileMap)
        {
            // Set blocked or vacuum if there is a wall or there is no plenum
            ITileLocation plenumLayerTile = tileMap.GetTileLocation(TileLayer.Plenum, GetWorldPosition());

            ITileLocation turfLayerTile = tileMap.GetTileLocation(TileLayer.Turf, GetWorldPosition());

            // Set air if there's Plenum and no walls on top.
            // todo : should make a difference between fully build walls and stuff like girders
            if (!plenumLayerTile.IsFullyEmpty() && (turfLayerTile.IsFullyEmpty() || turfLayerTile.TryGetPlacedObject(out PlacedTileObject placedObject) && placedObject.GenericType != TileObjectGenericType.Wall))
            {
                // Set to default air mixture
                AtmosObject.MakeEmpty();
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

        public Vector3 GetWorldPosition()
        {
            return Chunk.GetWorldPosition(X, Y);
        }

        public AtmosSaveObject Save()
        {
            AtmosSaveState saveState = AtmosSaveState.Air;
            MixSaveState mixState = new MixSaveState(0, 0);

            if (AtmosObject.State == AtmosState.Vacuum)
            {
                saveState = AtmosSaveState.Vacuum;
            }
            else if (AtmosObject.IsAir())   // TODO: Skip if just a regular air tile
            {
                saveState = AtmosSaveState.Air;
            }
            else if (!AtmosObject.IsEmpty)
            {
                saveState = AtmosSaveState.Mix;
                mixState = new MixSaveState(AtmosObject.CoreGasses, AtmosObject.Temperature);
            }
            else
            {
                Debug.LogError("Empty atmos tile found that is not marked as vacuum. Initialization error?");
            }

            return new AtmosSaveObject
            {
                x = X,
                y = Y,
                state = saveState,
                mix = mixState
            };
        }
    }
}