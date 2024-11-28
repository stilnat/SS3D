using QuikGraph;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeSystem : MonoBehaviour
{
    private record PipeVertice(short X, short Y, byte Layer);

    private bool _leftPipeGraphIsDirty;

    /// <summary>
    /// Graph representing all electric devices and connections on the map. Each electric device is a vertice in this graph,
    /// and each electric connection is an edge.
    /// </summary>
    private UndirectedGraph<PipeVertice, Edge<PipeVertice>> _leftPipesGraph;

    private void List<PipeNet>

    public void AddPipe(AtmosPipe pipe)
    {
        PlacedTileObject tileObject = pipe.TileObject;
        PipeVertice pipeVertice = new ((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y,
            (byte)tileObject.Layer);

        UndirectedGraph<PipeVertice, Edge<PipeVertice>> pipeGraph = GetPipeGraphForLayer(tileObject.Layer);
        pipeGraph.AddVertex(pipeVertice);
        List<PlacedTileObject> neighbours = tileObject.Connector?.GetNeighbours();
        foreach(PlacedTileObject neighbour in neighbours)
        {
            PipeVertice neighbourPipe = new ((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                (byte)neighbour.Layer);
                
            pipeGraph.AddVertex(neighbourPipe);
            pipeGraph.AddEdge(new (pipeVertice, neighbourPipe));
        }
        _leftPipeGraphIsDirty = true;
    }

    private UndirectedGraph<PipeVertice, Edge<PipeVertice>> GetPipeGraphForLayer(TileLayer layer)
    {
        switch (layer)
        {
            case TileLayer.PipeLeft:
                return _leftPipesGraph;
        }

        return _leftPipesGraph;
    }
}