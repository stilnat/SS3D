using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Engine.AtmosphericsRework;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using AtmosObject = SS3D.Engine.AtmosphericsRework.AtmosObject;

public class AtmosPipe : NetworkActor, IAtmosPipe
{

    public AtmosObject AtmosObject { get; set; }
    
    public PlacedTileObject PlacedTileObject => GetComponent<PlacedTileObject>();
    
    public int PipeNetIndex { get; set; }
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        AtmosObject = new(new int2(0,0), 0.5f);

        if (Subsystems.Get<PipeSystem>().IsSetUp)
        {
            Subsystems.Get<PipeSystem>().RegisterPipe(this);
        }
        else
        {
            Subsystems.Get<PipeSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSystem>().RegisterPipe(this);
        }
    }
    
     
}
