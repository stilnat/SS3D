using DebugDrawingExtension;
using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Animations
{
    public class HitAnimation : NetworkActor
    {

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        private Sequence _sequence;


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

            Vector3 fromHandToHit = hitTargetPosition - _hands.SelectedHand.HandBone.position;

            float duration = 3f * Mathf.Min(fromHandToHit.magnitude, 0.7f);

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

            _sequence.Join(DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 1f, timeToRotate));

            // A bit later but still while rotating, we start changing the hand position 
            _sequence.Insert(timeToRotate * 0.4f, AnimateHandPosition(hitTargetPosition, duration, finalRotationPlayer, _hands.SelectedHand.HandType == HandType.RightHand));

            // At the same time we move the hand, we start rotating it as well.
            // We have only half the duration here so that hand is pointing in the right direction approximately when reaching the hit target
            _sequence.Join(AnimateHandRotation(hitTargetPosition, duration * 0.5f, finalRotationPlayer));

            _sequence.OnStart(() =>
            {
                _lookAtTargetLocker.position = hitTargetPosition;
                if (_hands.SelectedHand.HandBone.transform.position.y - hitTargetPosition.y > 0.3)
                {
                    GetComponent<HumanoidAnimatorController>().Crouch(true);
                }
                GetComponent<HumanoidAnimatorController>().MakeFist(true, _hands.SelectedHand.HandType == HandType.RightHand);
            }); 
            _sequence.OnComplete(() =>
            {
                GetComponent<HumanoidAnimatorController>().Crouch(false);
                GetComponent<HumanoidAnimatorController>().MakeFist(false, _hands.SelectedHand.HandType == HandType.RightHand);
                _sequence = null;
            });
        }

        private Tween AnimateHandRotation(Vector3 hitTargetPosition, float duration, Quaternion finalRotation)
        {

            // All computations have to be done like if the player was already facing the direction it's facing in the end of its rotation
            Quaternion currentRotation = transform.rotation;
            transform.rotation = finalRotation;

            _hands.SelectedHand.PickupTargetLocker.transform.rotation = _hands.SelectedHand.HandBone.transform.rotation;

            Vector3 fromHandToHit = hitTargetPosition - _hands.SelectedHand.HandBone.position;

            Quaternion newRotation = Quaternion.FromToRotation(_hands.SelectedHand.PickupTargetLocker.transform.up, fromHandToHit) * _hands.SelectedHand.PickupTargetLocker.transform.rotation;

            Tween tween = _hands.SelectedHand.PickupTargetLocker.transform.DORotate(newRotation.eulerAngles, duration);
            tween.Pause();

            // restore the modified rotation
            transform.rotation = currentRotation;
            return tween;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitTargetPosition"> hit target position in world space</param>
        /// <param name="duration"> The duration of the whole animation hit </param>
        /// <param name="finalRotation"> The rotation of the player after it's facing the </param>
        /// <returns></returns>
        private Tween AnimateHandPosition(Vector3 hitTargetPosition, float duration, Quaternion finalRotation, bool isRight)
        {

            // All computations have to be done like if the player was already facing the direction it's facing in the end
            Quaternion currentRotation = transform.rotation;
            transform.rotation = finalRotation;

            // We start by setting the IK target on the hand bone with the same rotation for a smooth start into IK animation
            _hands.SelectedHand.PickupIkConstraint.weight = 1;
            _hands.SelectedHand.PickupTargetLocker.transform.position = _hands.SelectedHand.HandBone.transform.position;
            _hands.SelectedHand.PickupTargetLocker.transform.rotation = _hands.SelectedHand.HandBone.transform.rotation;

            float deviationFromStraightTrajectory = 0.2f;

            // direction vector from the hand to the hit in world space.
            Vector3 fromHandToHit = hitTargetPosition - _hands.SelectedHand.HandBone.position;

            // direction vector from the hand to the hit in transform parent space.
            Vector3 fromHandToHitRelativeToPlayer = transform.InverseTransformDirection(fromHandToHit);


            // We don't want our trajectory to be to streched, so we put the hit point closer if necessary, reachable by human
            if (fromHandToHit.magnitude > 0.7f)
            {
                hitTargetPosition = _hands.SelectedHand.HandBone.position + (fromHandToHit.normalized * 0.7f);
            }

            // compute the hit position relative to player root
            Vector3 hitPositionRelativeToPlayer = transform.InverseTransformPoint(hitTargetPosition);

            // compute the hand position relative to player root
            Vector3 handPositionRelativeToPlayer = transform.InverseTransformPoint( _hands.SelectedHand.HandBone.position);

            // compute the middle between hand and hit position, still in player's root referential
            Vector3 middleFromHandToHit =  (hitPositionRelativeToPlayer + handPositionRelativeToPlayer) / 2;

            int deviationRightOrLeft = isRight ? 1 : -1;

            // compute the trajectory deviation point, using the cross product to get a vector orthogonal to the hit direction,
            // then, from the middle of the distance between hand and hit, step on the side from a given quantity.
            // Uses the up vector for the cross product, hopefully the player never hits perfectly vertically 
            Vector3 trajectoryPeak = middleFromHandToHit + (deviationRightOrLeft * (Vector3.Cross(Vector3.up, fromHandToHitRelativeToPlayer).normalized * deviationFromStraightTrajectory));

            // Same as trajectoryPeak but for when the hand gets back in rest position
            Vector3 trajectoryPeakBack = middleFromHandToHit - (deviationRightOrLeft * (Vector3.Cross(Vector3.up, fromHandToHitRelativeToPlayer).normalized * deviationFromStraightTrajectory));

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
            DebugExtension.DebugPoint( (transform.rotation *trajectoryPeak) + transform.position, Color.cyan, 0.2f,2f);

            // Define the points for the trajectory in the player's root referential
            Vector3[] path = new Vector3[] {
                handPositionRelativeToPlayer,
                trajectoryPeak,
                hitPositionRelativeToPlayer,
                trajectoryPeakBack, 
            };

            // Set the IK target to be parented by human so that we can use local path to animate the IK target,
            // while keeping the trajectory relative to the player's root transform.
            _hands.SelectedHand.PickupTargetLocker.transform.parent = transform;

            // Play a trajectory that is local to the player's root, so will stay the same relatively to player's root if player moves
            Tween tween = _hands.SelectedHand.PickupTargetLocker.transform.DOLocalPath(path, duration, PathType.CatmullRom);


            // Upon reaching the hit position, we start slowly decreasing the IK 
            tween.onWaypointChange += value =>
            {
                if (value == 2)
                {
                    DOTween.To(() => _hands.SelectedHand.PickupIkConstraint.weight, x => _hands.SelectedHand.PickupIkConstraint.weight = x, 0f, duration);
                    DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 0f, duration);

                }
            };

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


    }
}
