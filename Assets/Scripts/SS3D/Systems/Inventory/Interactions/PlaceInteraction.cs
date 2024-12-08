using JetBrains.Annotations;
using System;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class PlaceInteraction : DelayedInteraction
    {
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

        public override InteractionType InteractionType => InteractionType.Place;

        public PlaceInteraction(float timeToMoveBackHand, float timeToReachDropPlace)
        {
            TimeToReachDropPlace = timeToReachDropPlace;
            TimeToMoveBackHand = timeToMoveBackHand;
            Delay = TimeToReachDropPlace;
        }

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Place";
        }

        [NotNull]
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
            if (angle > _maxSurfaceAngle)
            {
                return false;
            }
            
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }
            bool rangeCheck = InteractionExtensions.RangeCheck(interactionEvent);

            return rangeCheck;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            if (!hand) { return true; }
            hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(
                InteractionType.Place, hand, hand.ItemInHand.GetComponent<AbstractHoldable>(), interactionEvent.Point, TimeToMoveBackHand + TimeToReachDropPlace);
            
            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is Hand hand && hand.ItemInHand is not null)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);
            }
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            if (!hand) { return; }

            Item item = hand.ItemInHand;
            item.Container.RemoveItem(item);
            item.GiveOwnership(null);
            item.transform.parent = null;
        }
    }
}
