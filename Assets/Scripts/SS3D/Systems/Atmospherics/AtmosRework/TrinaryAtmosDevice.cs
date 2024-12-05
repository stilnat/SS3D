using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using UnityEngine;

public abstract class TrinaryAtmosDevice : BasicAtmosDevice, IInteractionTarget
{
    protected Vector3 FrontPipePosition;
    protected Vector3 BackPipePosition; 
    protected Vector3 SidePipePosition;

    protected IAtmosPipe FrontPipe;
    protected IAtmosPipe BackPipe; 
    protected IAtmosPipe SidePipe;
    

    protected TileLayer Pipelayer = TileLayer.PipeLeft;

    [SerializeField]
    private bool _sideOnRight;

    protected bool AllPipesConnected => FrontPipe != null && BackPipe != null && SidePipe != null;

    public override void StepAtmos(float dt)
    {
        FrontPipePosition = transform.position + transform.forward;
        BackPipePosition = transform.position - transform.forward;
        SidePipePosition = _sideOnRight ? transform.position + transform.right : transform.position - transform.right;

        Subsystems.Get<PipeSystem>().TryGetAtmosPipe(FrontPipePosition, Pipelayer, out FrontPipe);
        Subsystems.Get<PipeSystem>().TryGetAtmosPipe(BackPipePosition, Pipelayer, out BackPipe);
        Subsystems.Get<PipeSystem>().TryGetAtmosPipe(SidePipePosition, Pipelayer, out SidePipe);
    }

    public abstract IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);
}