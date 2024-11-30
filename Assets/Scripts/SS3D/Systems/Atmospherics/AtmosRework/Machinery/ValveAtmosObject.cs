using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Engine.AtmosphericsRework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ValveAtmosObject : NetworkActor, IInteractionTarget, IAtmosPipe
{
    public enum ValveType
    {
        Manual,
        Digital
    }

    public ValveType valveType;
    
    [SerializeField]
    private bool _isEnabled = true;
    
    public PlacedTileObject PlacedTileObject => GetComponent<PlacedTileObject>(); 

    public int PipeNetIndex { get; set; }
    
    public AtmosObject AtmosObject { get; set; }
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        AtmosObject = new(new int2(0, 0), 0.5f);

        if (Subsystems.Get<PipeSystem>().IsSetUp)
        {
            Subsystems.Get<PipeSystem>().RegisterPipe(this);
        }
        else
        {
            Subsystems.Get<PipeSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSystem>().RegisterPipe(this);
        }
    }

    private void SetValve(bool enable)
    {
        _isEnabled = enable;
        if (_isEnabled)
        {
            Subsystems.Get<PipeSystem>().RegisterPipe(this);
        }
        else
        {
            Subsystems.Get<PipeSystem>().RemovePipe(this);
        }
    }

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[]
        {
            new SimpleInteraction
            {
                Name = _isEnabled ? "Close valve" : "Open valve", Interact = ValveInteract, RangeCheck = true,
            },
        };
    }
        
    private void ValveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
    {
        SetValve(!_isEnabled);
    }
}
