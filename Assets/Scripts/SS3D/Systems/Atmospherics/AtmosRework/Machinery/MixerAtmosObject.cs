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

public class MixerAtmosObject : BasicAtmosDevice, IInteractionTarget
{
        
        private const float MaxPressure = 4500f;
        
        [SyncVar(OnChange = nameof(SyncFirstInputAmount))]
        private float _inputOneAmount = 50f;

        [SyncVar(OnChange = nameof(SyncMixerActive))]
        private bool _mixerActive = false;

        [SyncVar(OnChange = nameof(SyncTargetPressure))]
        private float _targetPressure = 101f;

        private TileLayer _pipeLayer = TileLayer.PipeLeft;

        [ServerRpc(RequireOwnership = false)] public void SetMixerActive(bool mixerActive) => _mixerActive = mixerActive;

        [ServerRpc(RequireOwnership = false)] public void SetTargetPressure(float targetPressure) => _targetPressure = targetPressure;

        [ServerRpc(RequireOwnership = false)] public void SetFirstInput(float value) => _inputOneAmount = value;

        public Action<float> UpdateMixerFirstInputAmount;
        public Action<bool> UpdateMixerActive;
        public Action<float> UpdateTargetPressure;

        public GameObject MixerViewPrefab;


        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        { 
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = "check filter", Interact = MixerInteract, RangeCheck = true,
                },
            };
        }

        public override void StepAtmos(float dt)
        {
            if (!_mixerActive)
            {
                return;
            }

            Vector3 outputPosition = transform.position + transform.forward;
            Vector3 secondInputPosition = transform.position + transform.right;
            Vector3 firstInputPosition = transform.position - transform.forward;
        
            if (!Subsystems.Get<PipeSystem>().TryGetAtmosPipe(outputPosition, _pipeLayer, out IAtmosPipe output)
                || !Subsystems.Get<PipeSystem>().TryGetAtmosPipe(secondInputPosition, _pipeLayer, out IAtmosPipe secondInput)
                || !Subsystems.Get<PipeSystem>().TryGetAtmosPipe(firstInputPosition, _pipeLayer, out IAtmosPipe firstInput))
            {
                return;
            }

            float ratioOnetoTwo = _inputOneAmount / 100f;

            if (firstInput.AtmosObject.TotalMoles <= 1f || secondInput.AtmosObject.TotalMoles <= 1f ||output.AtmosObject.Pressure >= _targetPressure || output.AtmosObject.Pressure >= MaxPressure)
            {
                return;
            }

            // Calculate necessary moles to transfer using PV=nRT
            float pressureDifference = _targetPressure - output.AtmosObject.Pressure;
            float transferMoles = dt * pressureDifference * 1000 * output.AtmosObject.Volume / (output.AtmosObject.Temperature * GasConstants.gasConstant);

            float transferMoles1 = ratioOnetoTwo * transferMoles;
            float transferMoles2 = (1f - ratioOnetoTwo) * transferMoles;


            // We can't transfer more moles than there are
            float firstInputTotalMoles = firstInput.AtmosObject.TotalMoles;
            float secondInputTotalMoles = secondInput.AtmosObject.TotalMoles;

            // If one of the inputs didn't contain enough gas, scale the other down
            if (transferMoles1 > firstInputTotalMoles)
            {
                transferMoles2 = firstInputTotalMoles * (1 / ratioOnetoTwo) * (1 - ratioOnetoTwo);
                transferMoles1 = firstInputTotalMoles;
            }

            if (transferMoles2 > secondInputTotalMoles)
            {
                transferMoles1 = secondInputTotalMoles * (1 / (1 - ratioOnetoTwo)) * ratioOnetoTwo;
                transferMoles2 = secondInputTotalMoles;
            }

            float4 molesFirstToTransfer = firstInput.AtmosObject.CoreGassesProportions * transferMoles1;
            float4 molesSecondToTransfer = firstInput.AtmosObject.CoreGassesProportions * transferMoles2;

            Subsystems.Get<PipeSystem>().RemoveCoreGasses(firstInputPosition, molesFirstToTransfer, _pipeLayer);
            Subsystems.Get<PipeSystem>().RemoveCoreGasses(secondInputPosition, molesSecondToTransfer, _pipeLayer);
            Subsystems.Get<PipeSystem>().AddCoreGasses(outputPosition, molesFirstToTransfer, _pipeLayer);
            Subsystems.Get<PipeSystem>().AddCoreGasses(outputPosition, molesSecondToTransfer, _pipeLayer);
        }

        private void MixerInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            GameObject mixerView = Instantiate(MixerViewPrefab);
            mixerView.GetComponent<MixerView>().Initialize(this);
        }

        private void SyncFirstInputAmount(float oldValue, float newValue, bool asServer)
        {
            UpdateMixerFirstInputAmount?.Invoke(newValue);
        }

        private void SyncTargetPressure(float oldValue, float newValue, bool asServer)
        {
            UpdateTargetPressure?.Invoke(newValue);
        }

        private void SyncMixerActive(bool oldValue, bool newValue, bool asServer)
        {
            UpdateMixerActive?.Invoke(newValue);
        }

}