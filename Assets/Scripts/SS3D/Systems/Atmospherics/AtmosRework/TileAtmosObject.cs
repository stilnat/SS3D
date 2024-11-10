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

        private TileAtmosObject[] neighbours;
        private AtmosObject atmosObject;
        private AtmosMap map;
        private AtmosChunk chunk;
        private int x;
        private int y;

        public TileAtmosObject(AtmosMap map, AtmosChunk chunk, int x, int y)
        {
            this.map = map;
            this.chunk = chunk;
            this.x = x;
            this.y = y;

            atmosObject = new(new(chunk.GetKey().x, chunk.GetKey().y));
        }

        public AtmosObject GetAtmosObject()
        {
            return atmosObject;
        }

        public void SetAtmosObject(AtmosObject atmosObject)
        {
            this.atmosObject = atmosObject;
        }

        public void LoadNeighbours()
        {
            // Get neighbours
            TileAtmosObject[] neighbours = new TileAtmosObject[4];
            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction += 2)
            {
                var vector = TileHelper.ToCardinalVector(direction);

                TileAtmosObject tileAtmosObject = map.GetTileAtmosObject(chunk.GetWorldPosition(x + vector.Item1, y + vector.Item2));
                if (tileAtmosObject != null)
                    neighbours[TileHelper.GetCardinalDirectionIndex(direction)] = tileAtmosObject;
            }

            this.neighbours = neighbours;
        }

        public TileAtmosObject[] GetNeighbours()
        {
            return neighbours;
        }

        public void Initialize(TileMap tileMap)
        {
            LoadNeighbours();

            // Set blocked or vacuum if there is a wall or there is no plenum
            ITileLocation plenumLayerTile = tileMap.GetTileLocation(TileLayer.Plenum, GetWorldPosition());

            ITileLocation turfLayerTile = tileMap.GetTileLocation(TileLayer.Turf, GetWorldPosition());

            // Set air if there's Plenum and no walls on top.
            // todo : should make a difference between fully build walls and stuff like girders
            if (!plenumLayerTile.IsFullyEmpty() && (turfLayerTile.IsFullyEmpty() || turfLayerTile.TryGetPlacedObject(out PlacedTileObject placedObject) && placedObject.GenericType != TileObjectGenericType.Wall))
            {
                // Set to default air mixture
                atmosObject.Container.MakeEmpty();
            }

            // if no plenum, then put vacuum
            if (plenumLayerTile.IsFullyEmpty())
            {
                atmosObject.Container.MakeEmpty();
                atmosObject.State = AtmosState.Vacuum;
                atmosObject.Container.SetTemperature(173); // -100 C for space
            }

            // Set blocked with a wall
            if (!turfLayerTile.IsFullyEmpty() && turfLayerTile.TryGetPlacedObject(out placedObject) && placedObject.GenericType == TileObjectGenericType.Wall)
            {
                atmosObject.Container.MakeEmpty();
                atmosObject.State = AtmosState.Blocked;
            }
            
        }

        public Vector3 GetWorldPosition()
        {
            return chunk.GetWorldPosition(x, y);
        }

        public AtmosSaveObject Save()
        {
            AtmosSaveState saveState = AtmosSaveState.Air;
            MixSaveState mixState = new MixSaveState(0, 0);

            if (atmosObject.State == AtmosState.Vacuum)
            {
                saveState = AtmosSaveState.Vacuum;
            }
            else if (atmosObject.IsAir())   // TODO: Skip if just a regular air tile
            {
                saveState = AtmosSaveState.Air;
            }
            else if (!atmosObject.IsEmpty())
            {
                saveState = AtmosSaveState.Mix;
                mixState = new MixSaveState(atmosObject.Container.GetCoreGasses(), atmosObject.Container.GetTemperature());
            }
            else
            {
                Debug.LogError("Empty atmos tile found that is not marked as vacuum. Initialization error?");
            }

            return new AtmosSaveObject
            {
                x = x,
                y = y,
                state = saveState,
                mix = mixState
            };
        }
    }
}