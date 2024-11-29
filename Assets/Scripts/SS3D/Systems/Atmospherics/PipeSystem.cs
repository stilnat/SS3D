using FishNet.Object;
using QuikGraph;
using QuikGraph.Algorithms;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Engine.AtmosphericsRework;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;

public class PipeSystem : NetworkSystem
{
    public bool IsSetUp { get; private set; }
    
    public event Action OnSystemSetUp;
    
    private record PipeVertice(short X, short Y, byte Layer);

    private bool _pipesGraphIsDirty;

    /// <summary>
    /// Graph representing all electric devices and connections on the map. Each electric device is a vertice in this graph,
    /// and each electric connection is an edge.
    /// </summary>
    private UndirectedGraph<PipeVertice, Edge<PipeVertice>> _pipesGraph;

    private Dictionary<int, PipeNet> _netPipes;
    
    private List<IAtmosDevice> _atmosDevices;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _pipesGraph = new();
        _netPipes = new();
        _atmosDevices = new();
        IsSetUp = true;
        OnSystemSetUp?.Invoke();
        Subsystems.Get<AtmosManager>().AtmosTick += OnTick;
    }
    
    public void RemoveAtmosDevice(IAtmosDevice atmosDevice)
    {
        if(_atmosDevices == null)
        {
            return;
        }

        _atmosDevices.Remove(atmosDevice);
    }

    public List<IAtmosPipe> GetPipes(TileLayer layerType)
    {
        List<IAtmosPipe> pipes = new();

        foreach (PipeNet pipeNet in _netPipes.Values)
        {
            pipes.AddRange(pipeNet.GetPipes(layerType));
        }

        return pipes;
    }

    public void RegisterAtmosDevice(IAtmosDevice atmosDevice)
    {
        _atmosDevices.Add(atmosDevice);
    }
    
    public void RemovePipe(IAtmosPipe pipe)
    {
        
    }

    public void RegisterPipe(IAtmosPipe pipe)
    {
        PlacedTileObject tileObject = pipe.PlacedTileObject;
        PipeVertice pipeVertice = new((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y,
            (byte)tileObject.Layer);


        _pipesGraph.AddVertex(pipeVertice);
        List<PlacedTileObject> neighbours = tileObject.Connector?.GetNeighbours();
        foreach (PlacedTileObject neighbour in neighbours)
        {
            // todo : should check if pipe
            PipeVertice neighbourPipe = new((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                (byte)neighbour.Layer);
                
            _pipesGraph.AddVertex(neighbourPipe);
            _pipesGraph.AddEdge(new(pipeVertice, neighbourPipe));
        }
        
        _pipesGraphIsDirty = true;
    }

    private void OnTick()
    {
        if (_pipesGraphIsDirty)
        {
            UpdateAllNetPipesTopology();
        }

        foreach (IAtmosDevice atmosDevice in _atmosDevices)
        {
            atmosDevice.StepAtmos(0.1f);
        }
    }

    public bool TryGetAtmosPipe(Vector3 worldPosition, TileLayer layer, out IAtmosPipe atmosPipe)
    {
        atmosPipe = null;
        SingleTileLocation tileLocation = Subsystems.Get<TileSystem>().CurrentMap.GetTileLocation(layer, worldPosition) as SingleTileLocation;

        if (!tileLocation.TryGetPlacedObject(out PlacedTileObject placedTileObject))
        {
            return false;
        }

        if (!placedTileObject.TryGetComponent(out atmosPipe))
        {
            return false;
        }

        return true;
    }

    public void AddGasToPipeNet(int pipenetIndex, float4 coreGasses)
    {
        _netPipes[pipenetIndex].AddCoreGasses(coreGasses);
    }

    public void SetValve(IAtmosValve valve)
    {
        PlacedTileObject placed = valve.PlacedTileObject;

        if (!valve.IsOpen)
        {
            _pipesGraph.RemoveVertex(new((short)placed.WorldOrigin.x, (short)placed.WorldOrigin.y, (byte)placed.Layer));
        }
        else
        {
            List<PlacedTileObject> neighbours = placed.Connector?.GetNeighbours();
            PipeVertice pipeVertice = new((short)placed.WorldOrigin.x, (short)placed.WorldOrigin.y,
                (byte)placed.Layer);
            _pipesGraph.AddVertex(pipeVertice);
            
            foreach (PlacedTileObject neighbour in neighbours)
            {
                // todo : should check if pipe
                PipeVertice neighbourPipe = new((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                    (byte)neighbour.Layer);
                
                _pipesGraph.AddVertex(neighbourPipe);
                _pipesGraph.AddEdge(new(pipeVertice, neighbourPipe));
            }
        }

        _pipesGraphIsDirty = true;
    }
    
    [Server]
    private void UpdateAllNetPipesTopology()
    {
        Dictionary<PipeVertice, int> components = new();

        // Compute all components in the graph. One component is basically one pipenet.
        // In graph theory, a component is a connected subgraph that is not part of any larger connected subgraph.
        _pipesGraph.ConnectedComponents(components);
        _netPipes.Clear();
        
        // group all vertice coordinates by the component index they belong to.
        Dictionary<int, List<PipeVertice>> graphs = components.GroupBy(pair => pair.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Key).ToList()
            );

        foreach (int componentIndex in graphs.Keys)
        {
            List<PipeVertice> component = graphs[componentIndex];
            _netPipes[componentIndex] = new();

            foreach (PipeVertice coord in component)
            {
                TileSystem tileSystem = Subsystems.Get<TileSystem>();
                SingleTileLocation location = tileSystem.CurrentMap.GetTileLocation((TileLayer)coord.Layer, new(coord.X, 0f, coord.Y)) as SingleTileLocation;

                if (!location.TryGetPlacedObject(out PlacedTileObject placedObject)) continue;

                if (!placedObject.TryGetComponent(out IAtmosPipe pipe)) continue;

                _netPipes[componentIndex].AddPipe(pipe);
                
                pipe.PipeNetIndex = componentIndex;
            }

            _netPipes[componentIndex].Equalize();
        }
        
        _pipesGraphIsDirty = false;
    }
}

public interface IAtmosDevice
{
    public void StepAtmos(float dt);
}

public interface IAtmosPipe
{
    PlacedTileObject PlacedTileObject { get; }

    public int PipeNetIndex { get; set; }
    
    public AtmosObject AtmosObject { get; set; }
}

public interface IAtmosValve : IAtmosPipe
{
    public bool IsOpen { get; }
}

