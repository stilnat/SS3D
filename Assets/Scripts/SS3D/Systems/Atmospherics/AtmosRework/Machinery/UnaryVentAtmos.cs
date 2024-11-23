using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class UnaryVentAtmos : NetworkActor, IInteractionTarget, IAtmosLoop
    {
        
        private bool _deviceActive = true;
        private TileLayer _pipeLayer = TileLayer.PipeLeft;
        private float _targetPressure = 101.3f;
        private PressureEqualizingMode _pressureMode = PressureEqualizingMode.External;
        private OperatingMode _operatingMode = OperatingMode.Pump;

        private enum PressureEqualizingMode
        {
            Internal,
            External,
        }
        
        private enum OperatingMode
        {
            Pump,
            Suck,
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            Subsystems.Get<AtmosManager>().RegisterAtmosDevice(this); 
        }
        
        private void OnDestroy()
        {
            Subsystems.Get<AtmosManager>().RemoveAtmosDevice(this); 
        }

        public void Step()
        {
            if (!_deviceActive)
            {
                return; 
            }

            AtmosObject atmosEnv = Subsystems.Get<AtmosManager>().GetAtmosContainer(transform.position, TileLayer.Turf).AtmosObject;
            AtmosObject atmosPipe = Subsystems.Get<AtmosManager>().GetAtmosContainer(transform.position, _pipeLayer).AtmosObject;
            
            if ((_pressureMode == PressureEqualizingMode.External && atmosEnv.Pressure > _targetPressure) ||
                (_pressureMode == PressureEqualizingMode.Internal && atmosPipe.Pressure > _targetPressure))
            {
               return;
            }
            
            TileLayer layerToTransferTo = _operatingMode == OperatingMode.Pump ? TileLayer.Turf : _pipeLayer;
            TileLayer layerToTransferFrom = _operatingMode == OperatingMode.Suck ? TileLayer.Turf : _pipeLayer;
            AtmosObject atmosToTransferTo = _operatingMode == OperatingMode.Pump ? atmosEnv : atmosPipe;
            AtmosObject atmosToTransferFrom = _operatingMode == OperatingMode.Suck ? atmosEnv : atmosPipe;
            
            float4 toTransfer = AtmosCalculator.MolesToTransfer(atmosToTransferTo, ref atmosToTransferFrom, true, 0.1f, 0f, 0f);

            Subsystems.Get<AtmosManager>().AddGasses(transform.position, toTransfer, layerToTransferTo);
            Subsystems.Get<AtmosManager>().RemoveGasses(transform.position, toTransfer, layerToTransferFrom);
        }
        
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        { 
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _deviceActive ? "Stop vent" : "Start vent", Interact = ActiveInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _pressureMode == PressureEqualizingMode.Internal ? "External mode" : "Internal mode", Interact = PressureModeInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _operatingMode == OperatingMode.Suck ? "Pump mode" : "Suck mode", Interact = OperatingModeInteract, RangeCheck = true,
                },
            };
        }

        private void OperatingModeInteract(InteractionEvent arg1, InteractionReference arg2)
        {
            _operatingMode = _operatingMode switch
            {
                OperatingMode.Pump => OperatingMode.Suck,
                OperatingMode.Suck => OperatingMode.Pump,
                _ => _operatingMode,
            };
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _deviceActive = !_deviceActive;
        }

        private void PressureModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _pressureMode = _pressureMode switch
            {
                PressureEqualizingMode.Internal => PressureEqualizingMode.External,
                PressureEqualizingMode.External => PressureEqualizingMode.Internal,
                _ => _pressureMode,
            };
        }
    }
}

