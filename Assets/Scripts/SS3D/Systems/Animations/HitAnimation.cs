using DebugDrawingExtension;
using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class HitAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private ProceduralAnimationController _controller;

        private Sequence _sequence;

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            HitAnimate(targetPosition, mainHand, _controller.transform, time);
        }

        public override void Cancel()
        {
        }

        [Client]
        private void HitAnimate(Vector3 hitTargetPosition, Hand mainHand, Transform rootTransform, float duration)
        {
            _sequence = DOTween.Sequence();

            Vector3 directionFromTransformToTarget = hitTargetPosition - rootTransform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion finalRotationPlayer = Quaternion.LookRotation(directionFromTransformToTarget);
            float timeToRotate = (Quaternion.Angle(rootTransform.rotation, finalRotationPlayer) / 180f) * duration;

            // In sequence, we first rotate toward the target
            _sequence = TryRotateTowardTargetPosition(_sequence, _controller.transform, _controller, timeToRotate, hitTargetPosition);

            _sequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, timeToRotate));

            // A bit later but still while rotating, we start changing the hand position 
            _sequence.Insert(
                timeToRotate * 0.4f, 
                AnimateHandPosition(hitTargetPosition, duration, finalRotationPlayer, mainHand.HandType == HandType.RightHand, mainHand, rootTransform));

            // At the same time we move the hand, we start rotating it as well.
            // We have only half the duration here so that hand is pointing in the right direction approximately when reaching the hit target
            _sequence.Join(AnimateHandRotation(hitTargetPosition, duration * 0.5f, finalRotationPlayer, mainHand, rootTransform));

            _sequence.OnStart(() =>
            {
                _controller.LookAtTargetLocker.position = hitTargetPosition;
                if (mainHand.HandBone.transform.position.y - hitTargetPosition.y > 0.3)
                {
                    _controller.AnimatorController.Crouch(true);
                }
                _controller.AnimatorController.MakeFist(true, mainHand.HandType == HandType.RightHand);
            }); 
            _sequence.OnComplete(() =>
            {
                _controller.AnimatorController.Crouch(false);
                _controller.AnimatorController.MakeFist(false, mainHand.HandType == HandType.RightHand);
                _sequence = null;
            });
        }

        private Tween AnimateHandRotation(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, Hand mainHand, Transform rootTransform)
        {
            // All computations have to be done like if the player was already facing the direction it's facing in the end of its rotation
            Quaternion currentRotation = rootTransform.rotation;
            rootTransform.rotation = finalRotation;

            mainHand.Hold.PickupTargetLocker.transform.rotation = mainHand.HandBone.transform.rotation;

            Vector3 fromHandToHit = hitTargetPosition - mainHand.HandBone.position;

            Quaternion newRotation = Quaternion.FromToRotation(mainHand.Hold.PickupTargetLocker.transform.up, fromHandToHit) * mainHand.Hold.PickupTargetLocker.transform.rotation;

            Tween tween = mainHand.Hold.PickupTargetLocker.transform.DORotate(newRotation.eulerAngles, duration);
            tween.Pause();

            // restore the modified rotation
            rootTransform.rotation = currentRotation;
            return tween;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitTargetPosition"> hit target position in world space</param>
        /// <param name="duration"> The duration of the whole animation hit </param>
        /// <param name="finalRotation"> The rotation of the player after it's facing the </param>
        /// <returns></returns>
        private Tween AnimateHandPosition(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, bool isRight, Hand mainHand, Transform rootTransform)
        {
            // All computations have to be done like if the player was already facing the direction it's facing in the end
            Quaternion currentRotation = rootTransform.rotation;
            rootTransform.rotation = finalRotation;

            // We start by setting the IK target on the hand bone with the same rotation for a smooth start into IK animation
            mainHand.Hold.PickupIkConstraint.weight = 1;
            mainHand.Hold.PickupTargetLocker.transform.position = mainHand.HandBone.transform.position;
            mainHand.Hold.PickupTargetLocker.transform.rotation = mainHand.HandBone.transform.rotation;

            // direction vector from the hand to the hit in world space.
            Vector3 fromShoulderToHit = hitTargetPosition - mainHand.Hold.UpperArm.position;

            Vector3 handTargetPosition = ComputeTargetHandPosition(fromShoulderToHit, hitTargetPosition, mainHand);
            Vector3[] path = ComputeHandPath(handTargetPosition, fromShoulderToHit, isRight, mainHand, rootTransform);

            // Set the IK target to be parented by human so that we can use local path to animate the IK target,
            // while keeping the trajectory relative to the player's root transform.
            mainHand.Hold.PickupTargetLocker.transform.parent = rootTransform;

            // Play a trajectory that is local to the player's root, so will stay the same relatively to player's root if player moves
            Tween tween = mainHand.Hold.PickupTargetLocker.transform.DOLocalPath(path, duration, PathType.CatmullRom);


            // Upon reaching the hit position, we start slowly decreasing the IK 
            tween.onWaypointChange += value =>
            {
                if (value == 2)
                {
                    DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, duration);
                    DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, duration);
                }
            };

            // Allows showing the trajectory in editor
            tween.onUpdate += () =>
            {
                DebugExtension.DebugWireSphere(mainHand.Hold.PickupTargetLocker.position, 0.01f, 2f);
            };

            tween.Pause();

            // We restore the rotation we modified
            rootTransform.rotation = currentRotation;
            return tween;
        }

        /// <summary>
        /// Compute the path the hand will take for the hit animation.
        /// </summary>
        private Vector3[] ComputeHandPath(Vector3 handTargetPosition, Vector3 fromShoulderToHit, bool isRight, Hand mainHand, Transform rootTransform)
        {
            float deviationFromStraightTrajectory = 0.2f;

            // direction vector from the hand to the hit in transform parent space.
            Vector3 fromShoulderToHitRelativeToPlayer = rootTransform.InverseTransformDirection(fromShoulderToHit);

            // compute the hit position relative to player root
            Vector3 handTargetPositionRelativeToPlayer = rootTransform.InverseTransformPoint(handTargetPosition);

            // compute the hand position relative to player root
            Vector3 shoulderPositionRelativeToPlayer = rootTransform.InverseTransformPoint(mainHand.Hold.UpperArm.position);

            // compute the middle between hand and hit position, still in player's root referential
            Vector3 middleFromShoulderToHit = (handTargetPositionRelativeToPlayer + shoulderPositionRelativeToPlayer) / 2;

            int deviationRightOrLeft = isRight ? 1 : -1;

            // compute the trajectory deviation point, using the cross product to get a vector orthogonal to the hit direction,
            // then, from the middle of the distance between hand and hit, step on the side from a given quantity.
            // Uses the up vector for the cross product, hopefully the player never hits perfectly vertically 
            Vector3 trajectoryPeak = middleFromShoulderToHit + (Vector3.Cross(Vector3.up, fromShoulderToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // Same as trajectoryPeak but for when the hand gets back in rest position
            Vector3 trajectoryPeakBack = middleFromShoulderToHit - (Vector3.Cross(Vector3.up, fromShoulderToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // show the hit target position in the player referential
            DebugExtension.DebugPoint((rootTransform.rotation * handTargetPositionRelativeToPlayer) + rootTransform.position, Color.blue, 0.2f, 2f);

            // show the beginning of the animation
            DebugExtension.DebugPoint((rootTransform.rotation * handTargetPositionRelativeToPlayer) + rootTransform.position, Color.blue, 0.2f, 2f);

            // show the middle of the two precedent points
            DebugExtension.DebugPoint((rootTransform.rotation * middleFromShoulderToHit) + rootTransform.position, Color.green, 1f, 2f);

            Debug.DrawRay((rootTransform.rotation * middleFromShoulderToHit) + rootTransform.position, Vector3.Cross(Vector3.up, fromShoulderToHit).normalized * 0.6f, Color.green, 2f);

            // show the direction from shoulder to the hit target
            Debug.DrawRay(mainHand.Hold.UpperArm.position, fromShoulderToHit, Color.red, 2f);

            // show the trajectory point guiding the hand outside
            DebugExtension.DebugPoint( (rootTransform.rotation * trajectoryPeak) + rootTransform.position, Color.cyan, 0.2f,2f);

            // Define the points for the trajectory in the player's root referential
            Vector3[] path =
            {
                trajectoryPeak,
                handTargetPositionRelativeToPlayer,
                trajectoryPeakBack, 
            };

            return path;
        }

        /// <summary>
        /// Compute the position in world space of the hand hold, such that the item hit point reach the target hit position.
        /// </summary>
        private Vector3 ComputeTargetHandPosition(Vector3 fromShoulderToHit, Vector3 targetHitPosition, Hand mainHand)
        {
            Vector3 handTargetPosition = targetHitPosition;

            // We don't want our trajectory to be to streched, so we put the hit point closer if necessary, reachable by human
            if (fromShoulderToHit.magnitude > 0.7f)
            {
                handTargetPosition = mainHand.Hold.UpperArm.position + (fromShoulderToHit.normalized * 0.7f);
            }

            return handTargetPosition;
        }
    }
}
