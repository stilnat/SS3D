using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.GameModes.Events;
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
    public class PickupInteraction : DelayedInteraction
    {

        private bool _hasItemInHand;

        public float TimeToMoveBackItem { get; private set; }

        public float TimeToReachItem { get; private set; }

        public PickupInteraction(float timeToMoveBackItem, float timeToReachItem)
        {
            TimeToMoveBackItem = timeToMoveBackItem;
            TimeToReachItem = timeToReachItem;
            Delay = TimeToMoveBackItem + TimeToReachItem;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Pick up";
        }

        public override string GetGenericName() => "Pickup";

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // if the target is whatever the hell Alain did
            // and the part that matters, if the interaction source is a hand
            if (target is IGameObjectProvider targetBehaviour && source is Hand hand)
            {
                // check that the item is within range
                bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);
                if (!isInRange) {
                    return false;
                }

                // try to get the Item component from the GameObject we just interacted with
                // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
                Item item = targetBehaviour.GameObject.GetComponent<Item>();
                if (item == null)
                {
                    return false;
                }

                // check that our hand is empty
                if (!hand.IsEmpty() && hand.ItemInHand != item)
                {
                    return false;
                }

                // check the item is not in a container
                if (item.IsInContainer() && item.Container != hand.Container)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);

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

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // We don't want to cancel the interaction if the item is already in hand
            if (_hasItemInHand)
            {
                return;
            }

            if (interactionEvent.Source is Hand hand && interactionEvent.Target is Item target)
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
    }
}
