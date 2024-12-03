using FishNet.Object;
using FishNet.Object.Synchronizing;
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


    [SyncVar]
    private float _litersPerSecond = 10f;
    
    private bool _filterActive = false;

    private float _targetPressure;

    [SyncVar(OnChange = nameof(SyncFilterOxygen))]
    private bool _filterOxygen;

    [SyncVar(OnChange = nameof(SyncFilterNitrogen))]
    private bool _filterNitrogen;

    [SyncVar(OnChange = nameof(SyncFilterCarbonDioxyde))]
    private bool _filterCarbonDioxyde;

    [SyncVar(OnChange = nameof(SyncFilterPlasma))]
    private bool _filterPlasma;

    private TileLayer _pipeLayer = TileLayer.PipeLeft;

    public GameObject FilterViewPrefab;

    public Action<bool, CoreAtmosGasses> UpdateFilterGas;

    public void SetFlux(float litersPerSecond)
    {
        _litersPerSecond = litersPerSecond;
    }


    public void SetActive(bool filterActive)
    {
        _filterActive = filterActive;
    }
    
    [Server]
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
        float maxMolesToTransfer = (atmosInput.Pressure * _litersPerSecond * dt) / (GasConstants.gasConstant * atmosInput.Temperature);
        float4 molesToTransfer = atmosInput.CoreGassesProportions * math.max(maxMolesToTransfer, atmosInput.TotalMoles);

        bool4 filterCoreGasses= new bool4(_filterOxygen, _filterNitrogen, _filterCarbonDioxyde, _filterPlasma);
        
        Subsystems.Get<PipeSystem>().AddCoreGasses(filterOutputPosition, molesToTransfer * (int4)filterCoreGasses, _pipeLayer);
        Subsystems.Get<PipeSystem>().AddCoreGasses(otherOutputPosition, molesToTransfer * (int4)!filterCoreGasses, _pipeLayer);
        Subsystems.Get<PipeSystem>().RemoveCoreGasses(otherOutputPosition, molesToTransfer, _pipeLayer);
    }

    private void FilterInteract(InteractionEvent interactionEvent, InteractionReference arg2)
    {
        GameObject filterView = Instantiate(FilterViewPrefab);
        filterView.GetComponent<FilterView>().Initialize(this);
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

    [Client]
    public void FilterGas(bool isFiltering, CoreAtmosGasses gas)
    {
        RpcFilterGas(isFiltering, gas);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcFilterGas(bool isFiltering, CoreAtmosGasses gas)
    {
        switch (gas)
        {
              case CoreAtmosGasses.Nitrogen:
                  _filterNitrogen = isFiltering;
                  break;
              case CoreAtmosGasses.Oxygen:
                  _filterOxygen = isFiltering;
                  break;
              case CoreAtmosGasses.CarbonDioxide:
                  _filterCarbonDioxyde = isFiltering;
                  break;
              case CoreAtmosGasses.Plasma:
                  _filterPlasma = isFiltering;
                  break;
        }
    }

    private void SyncFilterOxygen(bool oldValue, bool newValue, bool asServer)
    {
        UpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Oxygen);
    }

    private void SyncFilterNitrogen(bool oldValue, bool newValue, bool asServer)
    {
        UpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Nitrogen);
    }

    private void SyncFilterPlasma(bool oldValue, bool newValue, bool asServer)
    {
        UpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Plasma);
    }

    private void SyncFilterCarbonDioxyde(bool oldValue, bool newValue, bool asServer)
    {
        UpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.CarbonDioxide);
    }
}