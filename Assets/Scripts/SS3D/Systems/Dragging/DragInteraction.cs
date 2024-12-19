using FishNet.Connection;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Dragging
{
    public class DragInteraction : ContinuousInteraction
    {
        private Draggable _draggedObject;

        private NetworkConnection _previousOwner;

        public float TimeToReachGrabPlace { get; private set; }

        public DragInteraction(float timeToReachGrabPlace)
        {
            TimeToReachGrabPlace = timeToReachGrabPlace;
            Delay = TimeToReachGrabPlace;
        }

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent) => new ClientDelayedInteraction();

        public override string GetName(InteractionEvent interactionEvent) => "Grab";

        public override string GetGenericName() => "Grab";

        public override InteractionType InteractionType => InteractionType.Grab;

        public override Sprite GetIcon(InteractionEvent interactionEvent) => throw new System.NotImplementedException();

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Can only grab with hand
            if (interactionEvent.Source.GetRootSource() is not Hand hand)
            {
                return false;
            }

            if (interactionEvent.Target is not Draggable grabbable)
            {
                return false;
            }

            // check that our hand is empty
            if (!hand.IsEmpty())
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();

            if (!entity)
            {
                return false;
            }

            return true;
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            Draggable grabbable = interactionEvent.Target as Draggable;

            hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType, hand, grabbable.GetComponent<NetworkObject>(), grabbable.GameObject.transform.position, Delay);

            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;

            _draggedObject = interactionEvent.Target as Draggable;
            _previousOwner = _draggedObject.Owner;
            _draggedObject.NetworkObject.GiveOwnership(hand.Owner);
        }


        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);

            // previous owner regain authority when not grabbed anymore
            if (_draggedObject != null)
            {
                _draggedObject.GiveOwnership(_previousOwner);
            }
        }

        protected override bool CanKeepInteracting(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }
    }
}
