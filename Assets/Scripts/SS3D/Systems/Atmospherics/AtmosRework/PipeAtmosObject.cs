using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class PipeAtmosObject : MonoBehaviour, IAtmosLoop
    {
        private AtmosObject atmosObject;
        private MultiAdjacencyConnector connector;

        public AtmosObject GetAtmosObject()
        {
            return atmosObject;
        }

        public void SetAtmosObject(AtmosObject atmos)
        {
            atmosObject = atmos;
        }

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        private void LoadNeighbours()
        {
            
        }

        
        //private IAtmosLoop[] GetNeighbours()
        //{

        //}
    }
}