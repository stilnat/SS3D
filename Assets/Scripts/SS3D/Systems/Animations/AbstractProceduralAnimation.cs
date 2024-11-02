using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Provide an abstract class for the IProceduralAnimation interface, giving access to  IProceduralAnimations to some common methods often used in procedural animations
    /// </summary>
    public abstract class AbstractProceduralAnimation : IProceduralAnimation
    {
        public abstract event Action<IProceduralAnimation> OnCompletion;

        public abstract void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay);

        public abstract void Cancel();

        protected bool PositionHasChanged { get; private set; }

        /// <summary>
        ///  Try to rotate on the flat plane the player toward a given position, often useful for procedural animations.
        /// </summary>
        protected Sequence TryRotateTowardTargetPosition(Sequence sequence, Transform rootTransform, ProceduralAnimationController controller, float rotateTime, Vector3 position)
        {
            if (controller.PositionController.Position != PositionType.Sitting)
            {
                sequence.Join(controller.transform.DORotate(
                    QuaternionExtension.SameHeightPlaneLookRotation(position - rootTransform.position, Vector3.up).eulerAngles, rotateTime));
            }

            return sequence;
        }

        /// <summary>
        /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
        /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
        /// </summary>
        protected void OrientTargetForHandRotation(Hand hand)
        {
            Vector3 armTargetDirection = hand.Hold.PickupTargetLocker.position - hand.Hold.UpperArm.position;

            Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);

            targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

            hand.Hold.PickupTargetLocker.rotation = targetRotation;
        }

        /// <summary>
        /// Adapt the position of the player interacting to make the animation look more natural, such as crouching when the interaction is too low.
        /// </summary>
        protected void AdaptPosition(PositionController positionController, Hand mainHand, Vector3 targetPosition)
        {
            if (mainHand.HandBone.transform.position.y - targetPosition.y > 0.3)
            {
                PositionHasChanged = positionController.TryCrouch();
            }
        }

        protected void RestorePosition(PositionController positionController)
        {
            if (PositionHasChanged)
            {
                positionController.TryToGetToPreviousPosition();
            }
        }

    }
}
