using SS3D.Engine.AtmosphericsRework;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PipeNet
{
    private readonly List<IAtmosPipe> _pipes = new();

    public List<IAtmosPipe> GetPipes(TileLayer layerType)
    {
        return _pipes.Where(x => x.PlacedTileObject.Layer == layerType).ToList();
    }

    public void AddPipe(IAtmosPipe pipe)
    {
        _pipes.Add(pipe);
    }

    public void Equalize()
    {
        float4 gasses = float4.zero;
        foreach (IAtmosPipe pipe in _pipes)
        {
            gasses += pipe.AtmosObject.CoreGasses;
        }
        
        float4 gassesPerPipe = gasses / _pipes.Count;
        foreach (IAtmosPipe pipe in _pipes)
        {
            AtmosObject atmos = pipe.AtmosObject;
            atmos.ClearCoreGasses();
            atmos.AddCoreGasses(gassesPerPipe);
            pipe.AtmosObject = atmos;
        }
    }
    
    public void AddCoreGasses(float4 gasses)
    {
        float4 gassesPerPipe = gasses / _pipes.Count;
        foreach (IAtmosPipe pipe in _pipes)
        {
            AtmosObject atmos = pipe.AtmosObject;
            atmos.AddCoreGasses(gassesPerPipe);
            pipe.AtmosObject = atmos;
        }
    }
    
    public void RemoveCoreGasses(float4 gasses)
    {
        float4 gassesPerPipe = gasses / _pipes.Count;
        foreach (IAtmosPipe pipe in _pipes)
        {
            AtmosObject atmos = pipe.AtmosObject;
            atmos.RemoveCoreGasses(gassesPerPipe);
            pipe.AtmosObject = atmos;
        }
    }
}