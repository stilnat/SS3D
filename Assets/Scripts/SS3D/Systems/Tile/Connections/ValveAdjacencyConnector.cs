using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveAdjacencyConnector : AbstractHorizontalConnector
{
    
    [SerializeField]
    private OffsetConnector _connector;
    
    
    protected override IMeshAndDirectionResolver AdjacencyResolver { get; }
    
    public override bool IsConnected(PlacedTileObject neighbourObject)
    { 
        bool isConnected = false;
        if (neighbourObject != null)
        {
            isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
            isConnected &= neighbourObject.GenericType == _genericType || _genericType == TileObjectGenericType.None;
            isConnected &= neighbourObject.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;
        }
        return isConnected;
    }
}
