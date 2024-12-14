using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class AtmosFilterAdjacencyConnector : AbstractHorizontalConnector
    {
        protected override IMeshAndDirectionResolver AdjacencyResolver { get; }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (!neighbourObject)
            {
                return false;
            }

            bool isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
            isConnected &= neighbourObject.GenericType == TileObjectGenericType.Pipe;
            isConnected &= neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin + new Vector2Int((int)gameObject.transform.forward.x, (int)gameObject.transform.forward.z) || neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin - new Vector2Int((int)gameObject.transform.forward.x, (int)gameObject.transform.forward.z) || neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin + new Vector2Int((int)gameObject.transform.right.x, (int)gameObject.transform.right.z);

            return isConnected;
        }
    }
}
