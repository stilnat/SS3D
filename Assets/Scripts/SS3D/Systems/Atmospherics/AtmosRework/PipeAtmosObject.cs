using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class PipeAtmosObject : MonoBehaviour, IAtmosLoop
    {
        private AtmosObject _internalAtmosObject;

        [SerializeField]
        private PipeAdjacencyConnector _connector;

        private List<IAtmosLoop> _neighbours;

        private void Start()
        {
            _internalAtmosObject = new (default);
        }

        public AtmosObject GetAtmosObject()
        {
            return _internalAtmosObject;
        }

        public List<IAtmosLoop> GetNeighbours()
        {
            if (_neighbours == null)
            {
                _neighbours = _connector.GetNeighbours()
                    .Where(x => x.Connector.IsConnected(_connector.PlacedObject))
                    .Select(x => x.GetComponent<IAtmosLoop>()).ToList();
            }

            return _neighbours;
        }

        public void SetAtmosObject(AtmosObject atmos)
        {
            _internalAtmosObject = atmos;
        }

        public void Initialize()
        {
            _internalAtmosObject = default;
        }

        public GameObject GameObject => gameObject;
    }
}