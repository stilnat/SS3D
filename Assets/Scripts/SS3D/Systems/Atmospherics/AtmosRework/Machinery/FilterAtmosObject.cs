using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Engine.AtmosphericsRework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FilterAtmosObject : BasicAtmosDevice, IInteractionTarget
{
    private const float MaxPressure = 4500f;

    private const float LitersPerSecond = 10f;
    
    private bool _filterActive = false;

    private float _targetPressure;
    
    private bool4 _filterCoreGasses = new(false, false, false, true);

    private TileLayer _pipeLayer = TileLayer.PipeLeft;



    public void SetActive(bool filterActive)
    {
        _filterActive = filterActive;
    }
    
    public override void StepAtmos(float dt)
    {
        if (!_filterActive)
        {
            return;
        }

        Vector3 otherOutputPosition = transform.position + transform.forward;
        Vector3 filterOutputPosition = transform.position + transform.right;
        Vector3 inputPosition = transform.position - transform.forward;
        
        if (!Subsystems.Get<PipeSystem>().TryGetAtmosPipe(otherOutputPosition, _pipeLayer, out IAtmosPipe outputOther)
        || !Subsystems.Get<PipeSystem>().TryGetAtmosPipe(filterOutputPosition, _pipeLayer, out IAtmosPipe outputFiltered)
        || !Subsystems.Get<PipeSystem>().TryGetAtmosPipe(inputPosition, _pipeLayer, out IAtmosPipe input))
        {
            return;
        }
        
        // Both outputs must not be blocked
        if (outputFiltered.AtmosObject.Pressure > MaxPressure && outputOther.AtmosObject.Pressure > MaxPressure)
        {
            return;
        }

        AtmosObject atmosInput = input.AtmosObject;
        float maxMolesToTransfer = (atmosInput.Pressure * LitersPerSecond * dt) / (GasConstants.gasConstant * atmosInput.Temperature);
        float4 molesToTransfer = atmosInput.CoreGassesProportions * math.max(maxMolesToTransfer, atmosInput.TotalMoles);
        
        Subsystems.Get<PipeSystem>().AddCoreGasses(filterOutputPosition, molesToTransfer * (int4)_filterCoreGasses, _pipeLayer);
        Subsystems.Get<PipeSystem>().AddCoreGasses(otherOutputPosition, molesToTransfer * (int4)!_filterCoreGasses, _pipeLayer);
        Subsystems.Get<PipeSystem>().RemoveCoreGasses(otherOutputPosition, molesToTransfer, _pipeLayer);
    }

    private void FilterInteract(InteractionEvent interactionEvent, InteractionReference arg2)
    {
        SetActive(!_filterActive);
    }

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[]
        {
            new SimpleInteraction
            {
                Name = _filterActive ? "Stop filter" : "Start filter", Interact = FilterInteract, RangeCheck = true,
            },
        };
    }
}
