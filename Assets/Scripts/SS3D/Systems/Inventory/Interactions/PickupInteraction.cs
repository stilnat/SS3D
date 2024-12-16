using JetBrains.Annotations;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Data.Generated;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Inventory.Interactions
{
    // A pickup interaction is when you pick an item and
    // add it into a container (in this case, the hands)
    // you can only pick things that are not in a container
    public sealed class PickupInteraction : DelayedInteraction
    {

        private bool _hasItemInHand;

        private float TimeToMoveBackItem { get;}

        private float TimeToReachItem { get;}

        public PickupInteraction(float timeToMoveBackItem, float timeToReachItem)
        {
            TimeToMoveBackItem = timeToMoveBackItem;
            TimeToReachItem = timeToReachItem;
            Delay = TimeToMoveBackItem + TimeToReachItem;
        }

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent) => "Pick up";

        [NotNull]
        public override string GetGenericName() => "Pickup";

        public override InteractionType InteractionType => InteractionType.Pickup;

        public override Sprite GetIcon(InteractionEvent interactionEvent) =>InteractionIcons.Take;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is not IGameObjectProvider targetBehaviour || source is not Hand hand) { return false; }

            // check that the item is within range
            bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);
            if (!isInRange) { return false; }

            // try to get the Item component from the GameObject we just interacted with
            // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
            Item item = targetBehaviour.GameObject.GetComponent<Item>();
                
            if (item is null) { return false; }

            // check that our hand is empty
            if (!hand.IsEmpty()) { return false; }

            // check the item is not in a container
            if (item.IsInContainer() && item.Container != hand.Container) { return false; }

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // We don't want to cancel the interaction if the item is already in hand
            if (_hasItemInHand)
            {
                return;
            }

            if (interactionEvent.Source is Hand hand)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);
            }
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _hasItemInHand = true;
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && source is Hand hand)
            {
                Item item = targetBehaviour.GameObject.GetComponent<Item>();
                hand.Container.AddItem(item);
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // remember that when we call this Start, we are starting the interaction per se
            // so we check if the source of the interaction is a Hand, and if the target is an Item
            if (interactionEvent.Source is Hand hand && interactionEvent.Target is Item target)
            {

                target.GiveOwnership(hand.Owner);
                hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Pickup, hand, target.Holdable, Vector3.zero, TimeToMoveBackItem + TimeToReachItem);

                try {
                    string ckey = hand.HandsController.Inventory.Body.Mind.player.Ckey;

                    // and call the event for picking up items for the Game Mode System
                    new ItemPickedUpEvent(target, ckey).Invoke(this);
                }
                catch { Debug.Log("Couldn't get Player Ckey"); }
            }

            return true;
        }
    }
}
