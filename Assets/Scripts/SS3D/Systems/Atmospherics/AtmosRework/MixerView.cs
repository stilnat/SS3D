using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MixerView : MonoBehaviour
{
    private MixerAtmosObject _mixerAtmosObject;

    [SerializeField]
    private SwitchButton _turnOn;

    [SerializeField]
    private SpaceSlider _sliderFirstInput;

    [SerializeField]
    private SpaceSlider _sliderTargetPressure;


    [SerializeField]
    private TextMeshProUGUI _displayFirstInput;

    [SerializeField]
    private TextMeshProUGUI _displayTargetPressure;

    private bool _dirtyTargetPressure;

    private bool _dirtyFirstInput;

    private const float SliderUpdateTime = 0.1f;

    private float _previousPressureUpdateTime;

    private float _previousInputUpdateTime;

    private void Update()
    {
        if (_dirtyTargetPressure && Time.time - _previousPressureUpdateTime > SliderUpdateTime)
        {
            _mixerAtmosObject.SetTargetPressure(_sliderTargetPressure.value);
            _dirtyTargetPressure = false;
            _previousPressureUpdateTime = Time.time;
        }

        if (_dirtyFirstInput && Time.time - _previousInputUpdateTime > SliderUpdateTime)
        {
            _mixerAtmosObject.SetFirstInput(_sliderFirstInput.value);
            _dirtyFirstInput = false;
            _previousInputUpdateTime = Time.time;
        }
    }

    public void Initialize(MixerAtmosObject mixerAtmosObject)
    {
        _mixerAtmosObject = mixerAtmosObject;
        _turnOn.Switch += isOn => _mixerAtmosObject.SetMixerActive(isOn);
        _sliderFirstInput.onValueChanged.AddListener(UpdateMixerFirstInputAmount);
        _sliderTargetPressure.onValueChanged.AddListener(UpdateMixerTargetPressure);
        _mixerAtmosObject.UpdateMixerFirstInputAmount += amount => UpdateVisualFirstInputAmount(amount, false);
        _mixerAtmosObject.UpdateTargetPressure += amount => UpdateVisualTargetPressure(amount, false);
    }

    private void UpdateMixerFirstInputAmount(float amount)
    {
        UpdateVisualFirstInputAmount(amount, true);
        _dirtyFirstInput = true;
    }

    private void UpdateMixerTargetPressure(float amount)
    {
        UpdateVisualTargetPressure(amount, true);
        _dirtyTargetPressure = true;
    }

    private void UpdateVisualFirstInputAmount(float amount, bool fromUI)
    {
        if (!fromUI && _sliderFirstInput.Pressed)
        {
            return;
        }

        _displayFirstInput.text = amount.ToString();
        _sliderFirstInput.value = amount;
    }

    private void UpdateVisualTargetPressure(float amount, bool fromUI)
    {
        if (!fromUI && _sliderTargetPressure.Pressed)
        {
            return;
        }

        _displayTargetPressure.text = amount.ToString();
        _sliderTargetPressure.value = amount;
    }

}