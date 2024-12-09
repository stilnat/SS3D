using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class MixerAtmosObject : TrinaryAtmosDevice
    {

        private const float MaxPressure = 4500f;

        [SyncVar(OnChange = nameof(SyncFirstInputAmount))]
        private float _inputOneAmount = 50f;

        [SyncVar(OnChange = nameof(SyncMixerActive))]
        private bool _mixerActive = false;

        [SyncVar(OnChange = nameof(SyncTargetPressure))]
        private float _targetPressure = 101f;

        [ServerRpc(RequireOwnership = false)]
        public void SetMixerActive(bool mixerActive) => _mixerActive = mixerActive;

        [ServerRpc(RequireOwnership = false)]
        public void SetTargetPressure(float targetPressure) => _targetPressure = targetPressure;

        [ServerRpc(RequireOwnership = false)]
        public void SetFirstInput(float value) => _inputOneAmount = value;

        public Action<float> UpdateMixerFirstInputAmount;
        public Action<bool> UpdateMixerActive;
        public Action<float> UpdateTargetPressure;

        public GameObject MixerViewPrefab;


        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
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
            base.StepAtmos(dt);

            if (!_mixerActive || !AllPipesConnected)
            {
                return;
            }

            float ratioOnetoTwo = _inputOneAmount / 100f;

            if (BackPipe.AtmosObject.TotalMoles <= 1f || SidePipe.AtmosObject.TotalMoles <= 1f || FrontPipe.AtmosObject.Pressure >= _targetPressure || FrontPipe.AtmosObject.Pressure >= MaxPressure)
            {
                return;
            }

            // Calculate necessary moles to transfer using PV=nRT
            float pressureDifference = _targetPressure - FrontPipe.AtmosObject.Pressure;
            float transferMoles = dt * pressureDifference * 1000 * FrontPipe.AtmosObject.Volume / (FrontPipe.AtmosObject.Temperature * GasConstants.GasConstant);

            float transferMoles1 = ratioOnetoTwo * transferMoles;
            float transferMoles2 = (1f - ratioOnetoTwo) * transferMoles;


            // We can't transfer more moles than there are
            float firstInputTotalMoles = BackPipe.AtmosObject.TotalMoles;
            float secondInputTotalMoles = SidePipe.AtmosObject.TotalMoles;

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

            float4 molesFirstToTransfer = BackPipe.AtmosObject.CoreGassesProportions * transferMoles1;
            float4 molesSecondToTransfer = BackPipe.AtmosObject.CoreGassesProportions * transferMoles2;

            Subsystems.Get<PipeSystem>().RemoveCoreGasses(BackPipePosition, molesFirstToTransfer, Pipelayer);
            Subsystems.Get<PipeSystem>().RemoveCoreGasses(SidePipePosition, molesSecondToTransfer, Pipelayer);
            Subsystems.Get<PipeSystem>().AddCoreGasses(FrontPipePosition, molesFirstToTransfer, Pipelayer);
            Subsystems.Get<PipeSystem>().AddCoreGasses(FrontPipePosition, molesSecondToTransfer, Pipelayer);
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
}
