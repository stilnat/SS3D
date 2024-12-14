using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Simple connector for pipes with a possible offset, such as atmos pipes.
    /// </summary>
    public class PipeAdjacencyConnector : AbstractHorizontalConnector
    {
        [SerializeField]
        private OffsetConnector _connector;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _connector;

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            bool isConnected = true;
            PlacedTileObject tileObject = GetComponent<PlacedTileObject>();

            if (neighbourObject == null || tileObject == null)
            {
                return false;
            }

            isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
            isConnected &= neighbourObject.GenericType == GenericType || GenericType == TileObjectGenericType.None;
            isConnected &= neighbourObject.SpecificType == SpecificType || SpecificType == TileObjectSpecificType.None;
            isConnected &= tileObject.IsCardinalNeighbour(neighbourObject);

            if (neighbourObject.TryGetComponent(out IAtmosValve valve))
            {
                isConnected &= tileObject.WorldOrigin == neighbourObject.WorldOrigin + new Vector2Int((int)neighbourObject.transform.forward.x, (int)neighbourObject.transform.forward.z) ||
                    tileObject.WorldOrigin == neighbourObject.WorldOrigin - new Vector2Int((int)neighbourObject.transform.forward.x, (int)neighbourObject.transform.forward.z);

                isConnected &= valve.IsOpen;
            }

            if (neighbourObject.TryGetComponent(out FilterAtmosObject filter))
            {
                isConnected &= tileObject.WorldOrigin == neighbourObject.WorldOrigin + new Vector2Int((int)neighbourObject.transform.forward.x, (int)neighbourObject.transform.forward.z) ||
                    tileObject.WorldOrigin == neighbourObject.WorldOrigin - new Vector2Int((int)neighbourObject.transform.forward.x, (int)neighbourObject.transform.forward.z)
                    || tileObject.WorldOrigin == neighbourObject.WorldOrigin + new Vector2Int((int)gameObject.transform.right.x, (int)gameObject.transform.right.z);
            }

            return isConnected;
        }
    }
}
