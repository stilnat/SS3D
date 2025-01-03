using Serilog;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Simple connector for pipes with a possible offset, such as atmos pipes.
    /// </summary>
    public class PipeAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        [FormerlySerializedAs("_connector")]
        [SerializeField]
        private OffsetPipeConnector _pipeConnector;

        /// <summary>
        /// The Placed tile object linked to this connector. It's this placed object that this
        /// connector update.
        /// </summary>
        private PlacedTileObject _placedObject;

        /// <summary>
        /// A structure containing data regarding connection of this PlacedTileObject with all 8
        /// adjacent neighbours (cardinal and diagonal connections).
        /// </summary>
        private AdjacencyMap _pipeLayerConnections;

        private AdjacencyMap _machineryLayerConnections;

        /// <summary>
        /// The specific mesh this connectable has.
        /// </summary>
        private MeshFilter _filter;

        /// <summary>
        /// Upon Setup, this should stay true.
        /// </summary>
        private bool _initialized;

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool isUpdated = false;

            // If neighbour is a atmos machinery and this pipe is connected to it
            if (isConnected && neighbourObject.TryGetComponent(out FilterAtmosObject _))
            {
                isUpdated = _machineryLayerConnections.SetConnection(dir, true);
            }

            if (isConnected && neighbourObject.TryGetComponent(out PipeAdjacencyConnector _))
            {
                isUpdated = _pipeLayerConnections.SetConnection(dir, true);
            }

            if (isUpdated)
            {
                if (neighbourObject)
                {
                    neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
                }

                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        public void UpdateAllConnections()
        {
            Setup();

            List<PlacedTileObject> neighbourObjects = GetNeighbours();

            foreach (PlacedTileObject neighbourObject in neighbourObjects)
            {
                _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction dir);
                UpdateSingleConnection(dir, neighbourObject, true);
            }
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            bool isConnected = true;
            PlacedTileObject tileObject = GetComponent<PlacedTileObject>();

            if (neighbourObject == null || tileObject == null)
            {
                return false;
            }

            isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;

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

                // Can't connect to machinery if already connected to a pipe
                _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction direction);
                isConnected &= !_pipeLayerConnections.HasConnection(direction);
            }

            // Pipes connect only between themselves on the same layer.
            isConnected &= valve != null || filter || neighbourObject.Layer == _placedObject.Layer;

            return isConnected;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            TileMap map = tileSystem.CurrentMap;
            List<PlacedTileObject> neighboursPipe = map.GetCardinalNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position).ToList();
            List<PlacedTileObject> neighboursMachinery = map.GetCardinalNeighbourPlacedObjects(TileLayer.Turf, _placedObject.transform.position).ToList();
            neighboursPipe.RemoveAll(x => !x);
            neighboursMachinery.RemoveAll(x => !x);
            neighboursPipe.AddRange(neighboursMachinery);
            return neighboursPipe;
        }

        public List<PlacedTileObject> GetConnectedNeighbours()
        {
            return GetNeighbours().Where(IsConnected).ToList();
        }

        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = _pipeConnector.GetMeshAndDirection(_pipeLayerConnections);
            _filter.mesh = info.Mesh;
            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);
            transform.localRotation = localRotation;
        }

        private void Setup()
        {
            if (_initialized)
            {
                return;
            }

            _filter = GetComponent<MeshFilter>();
            _placedObject = GetComponentInParent<PlacedTileObject>();
            _pipeLayerConnections = new AdjacencyMap();
            _machineryLayerConnections = new AdjacencyMap();
            _initialized = true;
        }
    }
}
