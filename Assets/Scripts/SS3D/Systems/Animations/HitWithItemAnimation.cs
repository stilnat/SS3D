using Coimbra;
using DebugDrawingExtension;
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
    /// Procedural animation to hit with an item in hand
    /// </summary>
    public class HitWithItemAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private ProceduralAnimationController _controller;

        private Sequence _sequence;

        private float _armReach = 0.5f;

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            HitAnimate(targetPosition, mainHand, mainHand.ItemInHand.Holdable, _controller.transform, time);
        }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        [Client]
        private void HitAnimate(Vector3 hitTargetPosition, Hand mainHand, AbstractHoldable holdable, Transform rootTransform, float duration)
        {
            // Set the item to be on a temp object which will help with rotation on a pivot
            GameObject temp = new();

            // Put the temp object on the root transform so it moves with the player, and put it at the same place as the hold
            temp.transform.parent = rootTransform;
            Transform hold = holdable.GetHold(true, mainHand.HandType);
            temp.transform.position = hold.position;
            temp.transform.rotation = holdable.transform.rotation;

            // Parent the holdable on the temp object, such that the hold on the holdable is at the same place as the temp object
            holdable.transform.parent = temp.transform;
            holdable.transform.localPosition = -hold.localPosition;

            // We start by setting the pickup IK target back on the item hold
            mainHand.Hold.PickupIkConstraint.weight = 1;
            Transform parent = holdable.GetHold(true, mainHand.HandType);
            mainHand.Hold.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);

            // Compute the rotation of the player's root transform when looking at the hit position
            Vector3 directionFromTransformToTarget = hitTargetPosition - rootTransform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion finalRotationPlayer = Quaternion.LookRotation(directionFromTransformToTarget);

            float timeToRotate = (Quaternion.Angle(rootTransform.rotation, finalRotationPlayer) / 180f) * duration;

            if (_sequence != null)
            {
                _sequence.Kill();
            }

            

            _sequence = DOTween.Sequence();

            // In sequence, we first rotate toward the target
            _sequence.Append(rootTransform.DORotate(finalRotationPlayer.eulerAngles, timeToRotate));

            // A bit later but still while rotating, we start changing the temporary game object position 
            _sequence.Insert(
                timeToRotate * 0.4f, 
                AnimateHandPosition(hitTargetPosition, duration, finalRotationPlayer, mainHand.HandType == HandType.RightHand, temp.transform, rootTransform, mainHand));

            // At the same time we move the temporary game object, we start rotating it as well.
            // We have only half the duration here so that temporary game object is pointing in the right direction approximately when reaching the hit target
            _sequence.Join(AnimateHandRotation(hitTargetPosition, duration * 0.5f, finalRotationPlayer, temp.transform, rootTransform, mainHand));

            _sequence.OnStart(() =>
            {
                AdaptPosition(_controller.PositionController, mainHand, hitTargetPosition);
                _controller.LookAtTargetLocker.position = hitTargetPosition;
                _controller.LookAtTargetLocker.transform.parent = null;
                _controller.LookAtConstraint.weight = 1;
            }); 
            _sequence.OnComplete(() =>
            {
                _controller.PositionController.TryToGetToPreviousPosition();
                temp.Dispose(true);
                _sequence = null;
                holdable.transform.parent = mainHand.Hold.ItemPositionTargetLocker;
                holdable.transform.DOLocalRotate(Quaternion.identity.eulerAngles, 0.3f);
                holdable.transform.DOLocalMove(Vector3.zero, 0.3f);
                DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x=> mainHand.Hold.PickupIkConstraint.weight = x, 0, 0.3f);
                DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, 0.3f);
            });
        }

        private Tween AnimateHandRotation(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, Transform temp, Transform rootTransform, Hand mainHand)
        {

            Sequence rotation = DOTween.Sequence();

            // All computations have to be done like if the player was already facing the direction it's facing in the end of its rotation
            Quaternion currentRotation = rootTransform.rotation;
            rootTransform.rotation = finalRotation;

            mainHand.Hold.PickupTargetLocker.transform.rotation = mainHand.HandBone.transform.rotation;

            Vector3 fromHandToHit = (hitTargetPosition - mainHand.HandBone.position).normalized;

            ItemHitPoint hitPoint = mainHand.ItemInHand.GetComponent<ItemHitPoint>();

            Vector3 right = Vector3.Cross(Vector3.up, fromHandToHit).normalized;

            // the first rotation is such that the item is a bit toward the exterior to simulate gaining momentum
            Quaternion firstRotation = QuaternionExtension.AltForwardLookRotation(-right + (0.5f * fromHandToHit), hitPoint.ForwardHit, hitPoint.UpHit);

            // the last rotation is such that the item went a bit over the target rotation to simulate the momentum and rotation of arm
            Quaternion lastRotation = QuaternionExtension.AltForwardLookRotation(right + fromHandToHit, hitPoint.ForwardHit,  hitPoint.UpHit);

            rotation.Join(temp.transform.DORotate(firstRotation.eulerAngles, duration/2));
            rotation.Append(temp.transform.DORotate(lastRotation.eulerAngles, duration/2));

            rotation.SetEase(Ease.InSine);
            rotation.Pause();

            // restore the modified rotation
            rootTransform.rotation = currentRotation;
            return rotation;
        }


        /// <summary>
        ///  Animate the trajectory of the hand while hitting with item
        /// </summary>
        /// <param name="hitTargetPosition"> hit target position in world space</param>
        /// <param name="duration"> The duration of the whole animation hit </param>
        /// <param name="finalRotation"> The rotation of the player after it's facing the </param>
        private Tween AnimateHandPosition(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, bool isRight, Transform temp, Transform rootTransform, Hand mainHand)
        {
            // All computations have to be done like if the player was already facing the direction it's facing in the end
            Quaternion currentRotation = rootTransform.rotation;
            rootTransform.rotation = finalRotation;

            // direction vector from the shoulder to the hit in world space.
            Vector3 fromShoulderToHit = hitTargetPosition - mainHand.Hold.UpperArm.position;

            Vector3 targetHandHoldPosition = ComputeTargetHandHoldPosition(mainHand.ItemInHand.Holdable, fromShoulderToHit, hitTargetPosition, mainHand);

            Vector3[] path = ComputeHandPath(targetHandHoldPosition, fromShoulderToHit, isRight, rootTransform, mainHand);

            // Play a trajectory that is local to the player's root, so will stay the same relatively to player's root if player moves
            Tween tween = temp.DOLocalPath(path, duration, PathType.CatmullRom);

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
        /// <param name="targetHandHoldPosition"> The target hand hold position in world space.</param>
        /// <param name="fromHandToHit"></param>
        /// <param name="isRight"></param>
        /// <returns></returns>
        private Vector3[] ComputeHandPath(Vector3 targetHandHoldPosition, Vector3 fromShoulderToHit, bool isRight, Transform rootTransform, Hand mainHand)
        {

            float deviationFromStraightTrajectory = 0.2f;

            // direction vector from the shoulder to the hit in transform parent space.
            Vector3 fromShoulderToHitRelativeToPlayer = rootTransform.InverseTransformDirection(fromShoulderToHit);

            // compute the hit position relative to player root
            Vector3 hitPositionRelativeToPlayer = rootTransform.InverseTransformPoint(targetHandHoldPosition);

            // compute the shoulder position relative to player root
            Vector3 shoulderPositionRelativeToPlayer = rootTransform.InverseTransformPoint(mainHand.Hold.UpperArm.position);

            // compute the middle between hand and hit position, still in player's root referential
            Vector3 middleFromShoulderToHit = (hitPositionRelativeToPlayer + shoulderPositionRelativeToPlayer) / 2;

            int deviationRightOrLeft = isRight ? 1 : -1;

            // compute the trajectory deviation point, using the cross product to get a vector orthogonal to the hit direction,
            // then, from the middle of the distance between hand and hit, step on the side from a given quantity.
            // Uses the up vector for the cross product, hopefully the player never hits perfectly vertically 
            Vector3 trajectoryPeak = middleFromShoulderToHit + (Vector3.Cross(Vector3.up, fromShoulderToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // Same as trajectoryPeak but for when the hand gets back in rest position
            Vector3 trajectoryPeakBack = hitPositionRelativeToPlayer - (Vector3.Cross(Vector3.up, fromShoulderToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // show the hit target position in the player referential
            DebugExtension.DebugPoint((rootTransform.rotation * hitPositionRelativeToPlayer) + rootTransform.position, Color.blue, 0.2f, 2f);

            // show the beginning of the animation
            DebugExtension.DebugPoint((rootTransform.rotation * shoulderPositionRelativeToPlayer) + rootTransform.position, Color.blue, 0.2f, 2f);

            // show the middle of the two precedent points
            DebugExtension.DebugPoint((rootTransform.rotation * middleFromShoulderToHit) + rootTransform.position, Color.green, 1f, 2f);

            Debug.DrawRay((rootTransform.rotation * middleFromShoulderToHit) + rootTransform.position, Vector3.Cross(Vector3.up, fromShoulderToHit).normalized * 0.6f, Color.green, 2f);

            // show the direction from hand to the hit target
            Debug.DrawRay( mainHand.HandBone.position, fromShoulderToHit, Color.red, 2f);

            // show the trajectory point guiding the hand outside
            DebugExtension.DebugPoint( (rootTransform.rotation * trajectoryPeak) + rootTransform.position, Color.cyan, 0.2f,2f);

            // Define the points for the trajectory in the player's root referential
            Vector3[] path = {
                shoulderPositionRelativeToPlayer - (fromShoulderToHitRelativeToPlayer.normalized * 0.4f) + (Vector3.Cross(Vector3.up, fromShoulderToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory)),
                trajectoryPeak,
                hitPositionRelativeToPlayer,
                trajectoryPeakBack,
            };

            return path;
        }


        /// <summary>
        /// Compute the position in world space of the hand hold, such that the item hit point reach the target hit position.
        /// </summary>
        /// <param name="holdable"> The thing we hit with. </param>
        /// <param name="fromHandToHit"> The direction in world space, from the hand to the hit position. </param>
        /// <param name="targetHitPosition"> The hit position the player is trying to reach.</param>
        /// <returns></returns>
        private Vector3 ComputeTargetHandHoldPosition(AbstractHoldable holdable, Vector3 fromShoulderToHit, Vector3 targetHitPosition, Hand mainHand)
        {
            Transform item = holdable.GameObject.transform;
            Quaternion savedRotation = item.rotation;
            Vector3 savedPosition = item.position;

            ItemHitPoint hitPoint = mainHand.ItemInHand.GetComponent<ItemHitPoint>();

            item.rotation = QuaternionExtension.AltForwardLookRotation(item.GetComponent<ItemHitPoint>().HitPoint.forward, hitPoint.ForwardHit, hitPoint.UpHit);

            // Place item such that the item hit position match the target hit position 
            item.position = targetHitPosition - item.GetComponent<ItemHitPoint>().HitPoint.localPosition;

            Transform hold = holdable.GetHold(true, mainHand.HandType);

            // once in place simply get the hold position
            Vector3 targetHandHoldPosition = hold.position;

            // restore position and rotation
            item.position = savedPosition;
            item.rotation = savedRotation;

            float distanceHoldToHit = Vector3.Distance(hold.position, item.GetComponent<ItemHitPoint>().HitPoint.position);

            // We don't want our trajectory to be too streched, so we put the hit point closer if necessary, reachable by human hitting with item
            if (fromShoulderToHit.magnitude > _armReach + distanceHoldToHit)
            {
                targetHandHoldPosition = mainHand.ItemInHand.transform.position + (fromShoulderToHit.normalized * _armReach);
            }

            return targetHandHoldPosition;
        }
    }
}
