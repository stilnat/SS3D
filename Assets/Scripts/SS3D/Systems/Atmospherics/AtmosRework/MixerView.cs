using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MixerView : MonoBehaviour
{
    private MixerAtmosObject _mixerAtmosObject;

    [SerializeField]
    private SwitchButton _turnOn;

    [SerializeField]
    private Slider _slider;

    public void Initialize(MixerAtmosObject mixerAtmosObject)
    {
        _mixerAtmosObject = mixerAtmosObject;
        _turnOn.Switch += isOn => _mixerAtmosObject.SetMixerActive(isOn);
        _slider.onValueChanged.AddListener(UpdateMixerFirstInputAmount);
        _mixerAtmosObject.UpdateMixerFirstInputAmount += UpdateMixerFirstInputAmount;
    }

    private void UpdateMixerFirstInputAmount(float amount)
    {
        if (Math.Abs(amount - _slider.value) < 1f)
        {
            return;
        }

        _slider.value = amount;
        _mixerAtmosObject.SetFirstInput(amount);
    }

  
}