using Coimbra;
using DebugDrawingExtension;
using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HitWithItemAnimation : NetworkActor
{
    [SerializeField]
        private Hands _hands;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        private Sequence _sequence;

        private float _armReach = 0.5f;


        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
        }

        protected void Update()
        {
            if (!Input.GetMouseButtonDown(1))
            {
                return;
            }

            // Get the mouse position in screen space
            Vector3 mouseScreenPosition = Input.mousePosition;

            // Convert the mouse position to a ray
            Ray ray = Camera.current.ScreenPointToRay(mouseScreenPosition);

            // Create a RaycastHit variable to store the information about what was hit
            RaycastHit hit;

            // Cast the ray
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0))
            {
                // If the ray hits something, you can get the position
                Vector3 mouseWorldPosition = hit.point;
                HitAnimate(mouseWorldPosition);
                Debug.Log(hit.transform.gameObject);

            }

        }
 

        [Client]
        private void HitAnimate(Vector3 hitTargetPosition)
        {
            Item item = _hands.SelectedHand.ItemInHand;

            // set the item to be on a temp object which will help with rotation on a pivot
            GameObject temp = new();
            temp.transform.parent = transform;
            Transform hold = item.Holdable.GetHold(true, _hands.SelectedHand.HandType);
            temp.transform.position = hold.position;
            temp.transform.rotation = item.transform.rotation;
            item.transform.parent = temp.transform;
            item.transform.localPosition = -hold.localPosition;

            // We start by setting the pickup IK target back on the item hold
            _hands.SelectedHand.PickupIkConstraint.weight = 1;
            Transform parent = _hands.SelectedHand.ItemInHand.Holdable.GetHold(true, _hands.SelectedHand.HandType);
            _hands.SelectedHand.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);

            float duration = 0.7f;

            // Compute the rotation of the player's root transform when looking at the hit position
            Vector3 directionFromTransformToTarget = hitTargetPosition - transform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion finalRotationPlayer = Quaternion.LookRotation(directionFromTransformToTarget);

            float timeToRotate = (Quaternion.Angle(transform.rotation, finalRotationPlayer) / 180f) * duration;

            if (_sequence != null)
            {
                _sequence.Kill();
            }

            _sequence = DOTween.Sequence();

            // In sequence, we first rotate toward the target
            _sequence.Append(transform.DORotate(finalRotationPlayer.eulerAngles, timeToRotate));

            // A bit later but still while rotating, we start changing the temporary game object position 
            _sequence.Insert(timeToRotate * 0.4f, AnimateHandPosition(hitTargetPosition, duration, finalRotationPlayer, _hands.SelectedHand.HandType == HandType.RightHand, temp.transform));

            // At the same time we move the temporary game object, we start rotating it as well.
            // We have only half the duration here so that temporary game object is pointing in the right direction approximately when reaching the hit target
            _sequence.Join(AnimateHandRotation(hitTargetPosition, duration * 0.5f, finalRotationPlayer, temp.transform));

            _sequence.OnStart(() =>
            {
                _lookAtTargetLocker.position = hitTargetPosition;
                _lookAtTargetLocker.transform.parent = null;
                _lookAtConstraint.weight = 1;
                if (_hands.SelectedHand.HandBone.transform.position.y - hitTargetPosition.y > 0.3)
                {
                    GetComponent<HumanoidAnimatorController>().Crouch(true);
                }
            }); 
            _sequence.OnComplete(() =>
            {
                GetComponent<HumanoidAnimatorController>().Crouch(false);
                temp.Dispose(true);
                _sequence = null;
                _hands.SelectedHand.ItemInHand.transform.parent = _hands.SelectedHand.ItemPositionTargetLocker;
                _hands.SelectedHand.ItemInHand.transform.DOLocalRotate(Quaternion.identity.eulerAngles, 0.3f);
                _hands.SelectedHand.ItemInHand.transform.DOLocalMove(Vector3.zero, 0.3f);
                DOTween.To(() => _hands.SelectedHand.PickupIkConstraint.weight, x=> _hands.SelectedHand.PickupIkConstraint.weight = x, 0, 0.3f);
                DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 0f, 0.3f);
            });
        }

        private Tween AnimateHandRotation(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, Transform temp)
        {

            Sequence rotation = DOTween.Sequence();

            // All computations have to be done like if the player was already facing the direction it's facing in the end of its rotation
            Quaternion currentRotation = transform.rotation;
            transform.rotation = finalRotation;

            _hands.SelectedHand.PickupTargetLocker.transform.rotation = _hands.SelectedHand.HandBone.transform.rotation;

            Vector3 fromHandToHit = (hitTargetPosition - _hands.SelectedHand.HandBone.position).normalized;

            // todo : change rotation based on tool hit point

            ItemHitPoint hitPoint = _hands.SelectedHand.ItemInHand.GetComponent<ItemHitPoint>();

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
            transform.rotation = currentRotation;
            return rotation;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitTargetPosition"> hit target position in world space</param>
        /// <param name="duration"> The duration of the whole animation hit </param>
        /// <param name="finalRotation"> The rotation of the player after it's facing the </param>
        /// <returns></returns>
        private Tween AnimateHandPosition(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, bool isRight, Transform temp)
        {
            // All computations have to be done like if the player was already facing the direction it's facing in the end
            Quaternion currentRotation = transform.rotation;
            transform.rotation = finalRotation;

            // direction vector from the shoulder to the hit in world space.
            Vector3 fromHandToHit = hitTargetPosition - _hands.SelectedHand.UpperArm.position;

            Vector3 targetHandHoldPosition = ComputeTargetHandHoldPosition(_hands.SelectedHand.ItemInHand.Holdable, fromHandToHit, hitTargetPosition);

            Vector3[] path = ComputeHandPath(targetHandHoldPosition, fromHandToHit, isRight);

            // Play a trajectory that is local to the player's root, so will stay the same relatively to player's root if player moves
            Tween tween = temp.DOLocalPath(path, duration, PathType.CatmullRom);

            // Allows showing the trajectory in editor
            tween.onUpdate += () =>
            {
                DebugExtension.DebugWireSphere(_hands.SelectedHand.PickupTargetLocker.position, 0.01f, 2f);
            };

            tween.Pause();

            // We restore the rotation we modified
            transform.rotation = currentRotation;
            return tween;
        }

        /// <summary>
        /// Compute the path the hand will take for the hit animation.
        /// </summary>
        /// <param name="targetHandHoldPosition"> The target hand hold position in world space.</param>
        /// <param name="fromHandToHit"></param>
        /// <param name="isRight"></param>
        /// <returns></returns>
        private Vector3[] ComputeHandPath(Vector3 targetHandHoldPosition, Vector3 fromHandToHit, bool isRight)
        {

            float deviationFromStraightTrajectory = 0.2f;

            // direction vector from the hand to the hit in transform parent space.
            Vector3 fromHandToHitRelativeToPlayer = transform.InverseTransformDirection(fromHandToHit);

            // compute the hit position relative to player root
            Vector3 hitPositionRelativeToPlayer = transform.InverseTransformPoint(targetHandHoldPosition);

            // compute the item position relative to player root
            Vector3 handPositionRelativeToPlayer = transform.InverseTransformPoint(_hands.SelectedHand.ItemInHand.transform.position);

            // compute the middle between hand and hit position, still in player's root referential
            Vector3 middleFromHandToHit =  (hitPositionRelativeToPlayer + handPositionRelativeToPlayer) / 2;

            int deviationRightOrLeft = isRight ? 1 : -1;

            // compute the trajectory deviation point, using the cross product to get a vector orthogonal to the hit direction,
            // then, from the middle of the distance between hand and hit, step on the side from a given quantity.
            // Uses the up vector for the cross product, hopefully the player never hits perfectly vertically 
            Vector3 trajectoryPeak = middleFromHandToHit + (Vector3.Cross(Vector3.up, fromHandToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // Same as trajectoryPeak but for when the hand gets back in rest position
            Vector3 trajectoryPeakBack = hitPositionRelativeToPlayer - (Vector3.Cross(Vector3.up, fromHandToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory));

            // show the hit target position in the player referential
            DebugExtension.DebugPoint((transform.rotation * hitPositionRelativeToPlayer) + transform.position, Color.blue, 0.2f, 2f);

            // show the beginning of the animation
            DebugExtension.DebugPoint((transform.rotation * handPositionRelativeToPlayer) + transform.position, Color.blue, 0.2f, 2f);

            // show the middle of the two precedent points
            DebugExtension.DebugPoint((transform.rotation * middleFromHandToHit) + transform.position, Color.green, 1f, 2f);

            Debug.DrawRay((transform.rotation * middleFromHandToHit) + transform.position, Vector3.Cross(Vector3.up, fromHandToHit).normalized * 0.6f, Color.green, 2f);

            // show the direction from hand to the hit target
            Debug.DrawRay( _hands.SelectedHand.HandBone.position, fromHandToHit, Color.red, 2f);

            // show the trajectory point guiding the hand outside
            DebugExtension.DebugPoint( (transform.rotation * trajectoryPeak) + transform.position, Color.cyan, 0.2f,2f);

            // Define the points for the trajectory in the player's root referential
            Vector3[] path = {
                handPositionRelativeToPlayer - (fromHandToHitRelativeToPlayer.normalized * 0.4f) + (Vector3.Cross(Vector3.up, fromHandToHitRelativeToPlayer).normalized * (deviationRightOrLeft * deviationFromStraightTrajectory)),
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
        private Vector3 ComputeTargetHandHoldPosition(IHoldProvider holdable, Vector3 fromHandToHit, Vector3 targetHitPosition)
        {
            Transform item = holdable.GameObject.transform;
            Quaternion savedRotation = item.rotation;
            Vector3 savedPosition = item.position;

            ItemHitPoint hitPoint = _hands.SelectedHand.ItemInHand.GetComponent<ItemHitPoint>();

            item.rotation = QuaternionExtension.AltForwardLookRotation(item.GetComponent<ItemHitPoint>().HitPoint.forward, hitPoint.ForwardHit, hitPoint.UpHit);

            // Place item such that the item hit position match the target hit position 
            item.position = targetHitPosition - item.GetComponent<ItemHitPoint>().HitPoint.localPosition;

            Transform hold = holdable.GetHold(true, _hands.SelectedHand.HandType);

            // once in place simply get the hold position
            Vector3 targetHandHoldPosition = hold.position;

            // restore position and rotation
            item.position = savedPosition;
            item.rotation = savedRotation;

            float distanceHoldToHit = Vector3.Distance(hold.position, item.GetComponent<ItemHitPoint>().HitPoint.position);

            // We don't want our trajectory to be too streched, so we put the hit point closer if necessary, reachable by human hitting with item
            if (fromHandToHit.magnitude > _armReach + distanceHoldToHit)
            {
                targetHandHoldPosition = _hands.SelectedHand.ItemInHand.transform.position + (fromHandToHit.normalized * _armReach);
            }

            return targetHandHoldPosition;
        }
}
