using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ThrowInteraction : Interaction
    {
        private float _secondPerMeterFactorDef = 0.3f;

        private float _secondPerMeterFactorHarm = 0.15f;

        private float _maxForce = 20;

        public override string GetGenericName()
        {
            return "Throw";
        }

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Throw";
        }

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;;
        }

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if(interactionEvent.Source is not IGameObjectProvider source)
            {
                return false;
            }
            
            AimController aimController = source.GameObject.GetComponentInParent<AimController>();

            if (!aimController)
            {
                return false;
            }

            if (!aimController.IsAiming)
            {
                return false;
            }

            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not Hand hand)
            {
                return false;
            }

            if (hand.Empty)
            {
                return false;
            }

            return true;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if(interactionEvent.Source is not IGameObjectProvider source)
            {
                return false;
            }

            Hand hand = interactionEvent.Source.GetRootSource() as Hand; 
            
            ThrowAnimations throwAnimations = source.GameObject.GetComponentInParent<ThrowAnimations>();

            AimController aimController = source.GameObject.GetComponentInParent<AimController>();
            IntentController intentController = source.GameObject.GetComponentInParent<IntentController>();

            ServerThrow(hand, hand.ItemInHand, aimController.transform, aimController.AimTarget, intentController.Intent);

            source.GameObject.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Throw, hand, hand.ItemInHand, Vector3.zero, 0.3f);

            return false;
        }

        private async Task ServerThrow(Hand throwingHand, Item item, Transform playerRoot, Transform aimTarget, IntentType intent)
        {
            item.transform.parent = throwingHand.HandBone.transform;

            // ignore collisions while in hand, the time the item gets a bit out of the player collider.
            Physics.IgnoreCollision(item.GetComponent<Collider>(), playerRoot.gameObject.GetComponent<Collider>(), true);

            // wait roughly the time of animation on client before actually throwing
            await Task.Delay(180);

            throwingHand.Container.RemoveItem(item);
            item.transform.parent = null;
            item.GetComponent<Rigidbody>().isKinematic = false;
            item.GetComponent<Collider>().enabled = true;
            AddForceToItem(item.GameObject, playerRoot, aimTarget, intent);

            // after a short amount of time, stop ignoring collisions
            await Task.Delay(300);
            Physics.IgnoreCollision(item.GetComponent<Collider>(), playerRoot.gameObject.GetComponent<Collider>(), false);
        }

        private void AddForceToItem(GameObject item, Transform playerRoot, Transform aimTarget, IntentType intent)
        {
            Vector2 targetCoordinates = ComputeTargetCoordinates(aimTarget.position, playerRoot);

            Vector2 initialItemCoordinates = ComputeItemInitialCoordinates(item.transform.position, playerRoot);


            Vector2 initialVelocity = ComputeInitialVelocity(
                ComputeTimeToReach(intent, aimTarget.position, playerRoot),
                targetCoordinates,
                initialItemCoordinates.y,
                initialItemCoordinates.x);

            Vector3 initialVelocityInRootCoordinate = new Vector3(0, initialVelocity.y, initialVelocity.x);

            Vector3 initialVelocityInWorldCoordinate = playerRoot.TransformDirection(initialVelocityInRootCoordinate);

            if (initialVelocityInWorldCoordinate.magnitude > _maxForce)
            {
                initialVelocityInWorldCoordinate = initialVelocityInWorldCoordinate.normalized * _maxForce;
            }

            item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
        }

        private float ComputeTimeToReach(IntentType intent, Vector3 targetPosition, Transform playerRoot)
        {
            float distanceToTarget = Vector3.Distance(targetPosition, playerRoot.position);

            float timeToReach = intent == IntentType.Help ?
                distanceToTarget * _secondPerMeterFactorDef : distanceToTarget * _secondPerMeterFactorHarm;

            return timeToReach;
        }

        /// <summary>
        /// Compute coordinates in the local coordinate system of the throwing hand
        /// This method assumes that the target position is in the same plane as the plane defined by the
        /// player y and z local axis.
        /// return vector2 with components in order z and y, as z is forward and y upward.
        /// </summary>
        private Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
        {
            Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

            if (rootRelativeTargetPosition.x > 0.1f)
            {
                Debug.LogError("target not in the same plane as the player root : " + rootRelativeTargetPosition.x);
            }

            return new(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
        }

        private Vector2 ComputeItemInitialCoordinates(Vector3 itemPosition, Transform playerRoot)
        {
            Vector3 rootRelativeItemPosition = playerRoot.InverseTransformPoint(itemPosition);

            return new Vector2(rootRelativeItemPosition.z, rootRelativeItemPosition.y);
        }

        private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight, float initialHorizontalPosition)
        {
            // Those computations assume gravity is pulling in the same plane as the throw.
            // it works with any vertical gravity but not if there's a horizontal component to it.
            // be careful as g = -9.81 and not 9.81
            float g = Physics.gravity.y;
            float initialHorizontalVelocity = (targetCoordinates.x - initialHorizontalPosition) / timeToReachTarget;

            float initialVerticalVelocity = 
                (targetCoordinates.y - initialHeight - (0.5f * g * (Mathf.Pow(targetCoordinates.x - initialHorizontalPosition, 2) / Mathf.Pow(initialHorizontalVelocity, 2)))) * initialHorizontalVelocity / (targetCoordinates.x - initialHorizontalPosition);

            return new(initialHorizontalVelocity, initialVerticalVelocity);
        }
    }
}
