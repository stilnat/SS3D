using System;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class PlaceInteraction : GradualInteraction
    {
        private bool _hasDroppedItem;

        public float TimeToMoveBackHand { get; private set; }

        public float TimeToReachDropPlace { get; private set; }

        /// <summary>
        /// The maximum angle of surface the item will allow being dropped on
        /// </summary>
        private float _maxSurfaceAngle = 10;

        /// <summary>
        /// Only raycast the default layer for seeing if we are vision blocked
        /// </summary>
        private LayerMask _defaultMask = LayerMask.GetMask("Default");

        public PlaceInteraction(float timeToMoveBackHand, float timeToReachDropPlace)
        {
            TimeToReachDropPlace = timeToReachDropPlace;
            TimeToMoveBackHand = timeToMoveBackHand;
            Delay = TimeToReachDropPlace + TimeToMoveBackHand;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Place";
        }

        public override string GetGenericName() => "Place";

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIcons.Discard;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            if (!entity)
            {
                return false;
            }

            // Confirm the entities ViewPoint can see the drop point
            Vector3 direction = (interactionEvent.Point - entity.ViewPoint.transform.position).normalized;
            bool raycast = Physics.Raycast(entity.ViewPoint.transform.position, direction, out RaycastHit hit, 
                Mathf.Infinity, _defaultMask);
            if (!raycast)
            {
                return false;
            }

            // Confirm raycasted hit point is near the interaction point.
            // This is necessary because interaction rays are casted from the camera, not from view point
           // if (Vector3.Distance(interactionEvent.Point, hit.point) > 0.1)
            //{
            //    return false;
           // }
            
            // Consider if the surface is facing up
            float angle = Vector3.Angle(interactionEvent.Normal, Vector3.up);
            // Debug.Log($"surface angle is {angle}, interaction norma is {interactionEvent.Normal}");
            if (angle > _maxSurfaceAngle)
            {
                return false;
            }
            
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {

            if (StartTime + TimeToMoveBackHand >= Time.time || !HasStarted || _hasDroppedItem)
            {
                return base.Update(interactionEvent, reference);
            }

            // After time to pick up has passed, put the item in the container
            _hasDroppedItem = true;
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            Hand hand = interactionEvent.Source.GetRootSource() as Hand;

            Item item = hand.ItemInHand;
            item.Container.RemoveItem(item);
            item.GiveOwnership(null);
            item.transform.parent = null;

            return base.Update(interactionEvent, reference);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);

            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            hand.GetComponentInParent<PlaceAnimation>().Place(interactionEvent.Point, hand.ItemInHand, TimeToMoveBackHand, TimeToReachDropPlace);
            
            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("attempting to cancel animation");
            // We don't want to cancel the interaction if the item is already dropped
            /*if (_hasDroppedItem)
            {
                return;
            } */

            if (interactionEvent.Source.GetRootSource() is Hand hand && hand.ItemInHand != null)
            {
                hand.GetComponentInParent<PlaceAnimation>().CancelPlace(hand, hand.ItemInHand);
            }
        }
    }
}
