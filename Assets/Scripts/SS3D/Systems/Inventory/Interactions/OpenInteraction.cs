using FishNet.Object;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    [Serializable]
    public class OpenInteraction : DelayedInteraction
    {
        public event EventHandler<bool> OnOpenStateChanged;

        protected static readonly int OpenId = Animator.StringToHash("Open");

        private AttachedContainer _attachedContainer;

        public OpenInteraction() { }

        public OpenInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public override InteractionType InteractionType => InteractionType.Open;

        public override string GetGenericName()
        {
            return "Open";
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            Animator animator = interactionEvent.Target.GameObject.GetComponent<Animator>();
            if (_attachedContainer == null)
            {
                return animator.GetBool(OpenId) ? "Close" : "Open";
            }

            string name = _attachedContainer.ContainerName;

            return animator.GetBool(OpenId) ? "Close " + name : "Open " + name;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Open;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Check whether the object is in range
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return interactionEvent.Target is IGameObjectProvider target && IsFirstContainerOpenable(target);
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("in OpenInteraction, Start");
            GameObject target = interactionEvent.Target.GameObject;
            Animator animator = target.GetComponent<Animator>();
            bool open = animator.GetBool(OpenId);
            animator.SetBool(OpenId, !open);
            OnOpenStateChange(!open);
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IItemHolder hand = interactionEvent.Source as IItemHolder;

            Vector3 point = interactionEvent.Point;

            if (interactionEvent.Target.TryGetInteractionPoint(interactionEvent.Source, out Vector3 customPoint))
            {
                point = customPoint;
            }

            if (hand != null && hand is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.PlaySourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), point, Delay);
            }

            return true;
        }

        private void OnOpenStateChange(bool e)
        {
            Debug.Log("In OpenInteraction, OnOpenStateChange");
            OnOpenStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Verifies if the attachedContainer referenced by this script is the first one on the game object at the source of the interaction.
        /// </summary>
        private bool IsFirstContainerOpenable(IGameObjectProvider target)
        {
            // Only accept the first Openable container on the GameObject.
            // Note: if you want separately functioning doors etc, they must be on different GameObjects.
            ContainerInteractive[] attachedContainers = target.GameObject.GetComponents<ContainerInteractive>();
            for (int i = 0; i < attachedContainers.Length; i++)
            {
                if (_attachedContainer != attachedContainers[i].AttachedContainer && attachedContainers[i].AttachedContainer.IsOpenable)
                {
                    return false;
                }

                if (_attachedContainer == attachedContainers[i].AttachedContainer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
