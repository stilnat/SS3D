﻿using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    // This handle networking for an Openable object, openable meaning having an animation opening the object.
    // It allows the open/close state of the object to be synchronized and the animation to be fired
    // on all observers when updating the open/close state.
    [RequireComponent(typeof(Animator))]
    public class NetworkedOpenable : NetworkActor, IInteractionTarget
    {
        private static readonly int OpenAnimation = Animator.StringToHash("Open");

        private Animator _animator;

        [SyncVar(OnChange = nameof(SyncOpenState))]
        private bool _openState;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public virtual IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new();
            openInteraction.OnOpenStateChanged += OpenStateChanged;

            return new IInteraction[]
            {
                openInteraction,
            };
        }

        public bool IsOpen() => _openState;

        [Server]
        public void SetOpenState(bool e)
        {
            _openState = e;
            UpdateAnimator();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _animator = GetComponent<Animator>();
        }

        protected virtual void OpenStateChanged(object sender, bool e)
        {
            OpenAllOpenables(sender, e);
            _openState = e;
            UpdateAnimator();
        }

        protected virtual void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            UpdateAnimator();
        }

        /// <summary>
        /// On game objects with a single open animation but multiple containers depending on it,
        /// this method assures us that when the animation is fired, all NetworkedOpenable scripts are updated.
        /// </summary>
        private void OpenAllOpenables(object sender, bool e)
        {
            NetworkedOpenable[] openables = gameObject.GetComponents<NetworkedOpenable>();

            // If this is the top NetworkedOpenable in the inspector, tell the others to open too.
            if (openables[0] != this)
            {
                return;
            }

            for (int i = 1; i < openables.Length; i++)
            {
                openables[i].OpenStateChanged(sender, e);
            }
        }

        private void UpdateAnimator()
        {
            _animator.SetBool(OpenAnimation, _openState);
        }
    }
}
