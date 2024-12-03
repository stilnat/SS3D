using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityMvvmToolkit.Common;
using UnityMvvmToolkit.Core;
using UnityMvvmToolkit.Core.Converters.PropertyValueConverters;
using UnityMvvmToolkit.Core.Interfaces;
using UnityMvvmToolkit.UGUI;

public class FilterView : Actor
{
    private FilterAtmosObject _filterAtmosObject;

    [SerializeField]
    private SwitchButton _turnOn;

    [SerializeField]
    private SwitchButton _filterOxygen;

    [SerializeField]
    private SwitchButton _filterNitrogen;

    [SerializeField]
    private SwitchButton _filterCarbonDioxyde;

    [SerializeField]
    private SwitchButton _filterPlasma;

    [SerializeField]
    private Slider _slider;

    public void Initialize(FilterAtmosObject filterAtmosObject)
    {
        _filterAtmosObject = filterAtmosObject;
        _filterOxygen.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Oxygen);
        _filterNitrogen.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Nitrogen);
        _filterCarbonDioxyde.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.CarbonDioxide);
        _filterPlasma.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Plasma);
        _turnOn.Switch += isOn => _filterAtmosObject.SetActive(isOn);
        _slider.onValueChanged.AddListener(value => _filterAtmosObject.SetFlux(value));
        
        _filterAtmosObject.UpdateFilterGas += UpdateFilterGas;
    }

    private void UpdateFilterGas(bool isFiltering, CoreAtmosGasses gas)
    {
        switch (gas)
        {
            case CoreAtmosGasses.Nitrogen:
                _filterNitrogen.SetState(isFiltering, true);
                break;
            case CoreAtmosGasses.Oxygen:
                _filterOxygen.SetState(isFiltering, true);
                break;
            case CoreAtmosGasses.CarbonDioxide:
                _filterCarbonDioxyde.SetState(isFiltering, true);
                break;
            case CoreAtmosGasses.Plasma:
                _filterPlasma.SetState(isFiltering, true);
                break;
        }
    }
}