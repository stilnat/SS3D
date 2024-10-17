using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Interactions
{
    /// <summary>
    /// Small script to make a game object toggleable. All it does is send an OnToggle event when its state changes.
    /// </summary>
    public class GenericToggleInteractionTarget : NetworkActor, IToggleable, IInteractionTarget
    {
        [SyncVar]
        private bool _on;
        public Action<bool> OnToggle;

        [SerializeField]
        private Transform _toggle;

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new(1)
            {
                new ToggleInteraction()
            };

            return interactions.ToArray();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point)
        {
            point = Vector3.zero;

            if (_toggle != null)
            {
                point = _toggle.position;

                return true;
            }

            return false;
        }

        public bool GetState()
        {
            return _on;
        }

        public void Toggle()
        {
            _on = !_on;

            OnToggle?.Invoke(_on);
        }
    }
}
