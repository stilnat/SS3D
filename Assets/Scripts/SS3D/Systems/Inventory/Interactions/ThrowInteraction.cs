using JetBrains.Annotations;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
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
        private const float SecondPerMeterFactorDef = 0.3f;

        private const float SecondPerMeterFactorHarm = 0.15f;

        private const float MaxForce = 20;

        [NotNull]
        public override string GetGenericName() => "Throw";

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        [NotNull]
        public override string GetName(InteractionEvent interactionEvent) => "Throw";

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public override Sprite GetIcon(InteractionEvent interactionEvent) => Icon != null ? Icon : InteractionIcons.Take;

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

            if (!aimController.IsAimingToThrow)
            {
                return false;
            }

            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not Hand hand)
            {
                return false;
            }

            return !hand.Empty;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if(interactionEvent.Source is not IGameObjectProvider source)
            {
                return false;
            }

            Hand hand = interactionEvent.Source.GetRootSource() as Hand; 
            if(!hand) { return false; }

            HumanoidMovementController movementController = source.GameObject.GetComponentInParent<HumanoidMovementController>();
            IntentController intentController = source.GameObject.GetComponentInParent<IntentController>();

            ServerThrow(hand, hand.ItemInHand, movementController.transform, movementController.AimTarget, intentController.Intent, 0.5f);

            source.GameObject.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Throw, hand, hand.ItemInHand.Holdable, Vector3.zero, 0.5f);

            return false;
        }

        private async void ServerThrow(Hand throwingHand, Item item, Transform playerRoot, Transform aimTarget, IntentType intent, float time)
        {
            // remove client ownership so that server can take full control of item trajectory
            item.RemoveOwnership();

            // ignore collisions while in hand, the time the item gets a bit out of the player collider.
            Physics.IgnoreCollision(item.GetComponent<Collider>(), playerRoot.gameObject.GetComponent<Collider>(), true);

            // wait roughly the time of animation on client before actually throwing
            await Task.Delay((int)(time * 1000));
 
            item.transform.parent = null;
            item.GetComponent<Rigidbody>().isKinematic = false;
            item.GetComponent<Collider>().enabled = true;
            AddForceToItem(item.GameObject, playerRoot, aimTarget, intent);

            throwingHand.Container.RemoveItem(item);

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

            if (initialVelocityInWorldCoordinate.magnitude > MaxForce)
            {
                initialVelocityInWorldCoordinate = initialVelocityInWorldCoordinate.normalized * MaxForce;
            }

            item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
        }

        private static float ComputeTimeToReach(IntentType intent, Vector3 targetPosition, Transform playerRoot)
        {
            float distanceToTarget = Vector3.Distance(targetPosition, playerRoot.position);

            float timeToReach = intent == IntentType.Help ?
                distanceToTarget * SecondPerMeterFactorDef : distanceToTarget * SecondPerMeterFactorHarm;

            return timeToReach;
        }

        /// <summary>
        /// Compute coordinates in the local coordinate system of the throwing hand
        /// This method assumes that the target position is in the same plane as the plane defined by the
        /// player y and z local axis.
        /// return vector2 with components in order z and y, as z is forward and y upward.
        /// </summary>
        private static Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
        {
            Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

            if (rootRelativeTargetPosition.x > 0.1f)
            {
                Debug.LogError("target not in the same plane as the player root : " + rootRelativeTargetPosition.x);
            }

            return new(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
        }

        private static Vector2 ComputeItemInitialCoordinates(Vector3 itemPosition, Transform playerRoot)
        {
            Vector3 rootRelativeItemPosition = playerRoot.InverseTransformPoint(itemPosition);

            return new Vector2(rootRelativeItemPosition.z, rootRelativeItemPosition.y);
        }

        private static Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight, float initialHorizontalPosition)
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
