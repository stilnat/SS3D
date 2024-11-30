using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public class ScrubberAtmos : BasicAtmosDevice, IInteractionTarget
    {
        private enum OperatingMode
        {
            Scrubbing,
            Siphoning,
        }

        private enum Range
        {
            Normal,
            Extended,
        }
        
        private readonly float _molesRemovedPerSecond = 500f;

        private OperatingMode _mode = OperatingMode.Scrubbing;
        
        private Range _range = Range.Normal;

        private bool4 _filterCoreGasses;

        private TileLayer _pipeLayer = TileLayer.PipeLeft;

        private bool _deviceActive = true;

        private Vector3[] _atmosNeighboursPositions;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Vector3 position = transform.position;
            _atmosNeighboursPositions = new[]
            {
                position,
                position + Vector3.forward,
                position + Vector3.back,
                position + Vector3.right,
                position + Vector3.left,
            };
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _deviceActive ? "Stop scrubbber" : "Start scrubber", Interact = ActiveInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _mode == OperatingMode.Scrubbing ? "Siphon mode" : "Scrubbing mode", Interact = ModeInteract, RangeCheck = true,
                },
            };
        }

        public override void StepAtmos(float dt)
        {
            if (!_deviceActive)
            {
                return; 
            }
            
            if (!Subsystems.Get<PipeSystem>().TryGetAtmosPipe(transform.position, _pipeLayer, out IAtmosPipe pipe))
            {
                return;
            }

            if (pipe.AtmosObject.Pressure > 5000)
            {
                return;
            }
            
            int numOfTiles = 0;
            
            switch (_range)
            {
                case Range.Normal:
                    numOfTiles = 1;
                    break;
                case Range.Extended:
                    numOfTiles = 5;
                    break;
            }

            // We loop 1 or 5 times based on the range setting
            for (int i = 0; i < numOfTiles; i++)
            {
                AtmosObject atmos = Subsystems.Get<AtmosManager>().GetAtmosContainer(_atmosNeighboursPositions[i], TileLayer.Turf).AtmosObject;
                float4 toSiphon = 0;
                
                if (_mode == OperatingMode.Siphoning)
                {
                    toSiphon = atmos.CoreGassesProportions * math.min(atmos.TotalMoles, _molesRemovedPerSecond * dt);
                }
                
                // If scrubbing, remove only filtered gas
                else
                {
                    float4 filteredGasses = atmos.CoreGasses * (int4)_filterCoreGasses;
                    toSiphon = (filteredGasses / math.csum(filteredGasses)) * math.min(math.csum(filteredGasses), _molesRemovedPerSecond * dt);
                    toSiphon = math.any(math.isnan(toSiphon)) ? 0 : toSiphon;
                }

                if (math.all(toSiphon == 0))
                {
                    continue;
                }

                Subsystems.Get<PipeSystem>().AddCoreGasses(pipe.PlacedTileObject.gameObject.transform.position, toSiphon, _pipeLayer);
                Subsystems.Get<AtmosManager>().RemoveGasses(_atmosNeighboursPositions[i], toSiphon, TileLayer.Turf);
            }
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _deviceActive = !_deviceActive;
        }

        private void ModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _mode = _mode switch
            {
                OperatingMode.Scrubbing => OperatingMode.Siphoning,
                OperatingMode.Siphoning => OperatingMode.Scrubbing,
                _ => _mode,
            };
        }
    }
}
