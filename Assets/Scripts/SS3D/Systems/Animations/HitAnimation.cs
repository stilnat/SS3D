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
        private Transform _lookAtTargetLocker;


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
            if (!Input.GetMouseButtonDown(0))
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
            }

        }
 

        [Client]
        private void HitAnimate(Vector3 hitTargetPosition)
        {
            SetUpHit(_hands.SelectedHand);

            Vector3 fromHandToHit = hitTargetPosition - _hands.SelectedHand.HandBone.position;

            Vector3 middleFromHandToHit = (hitTargetPosition + _hands.SelectedHand.HandBone.position) / 2;

            Vector3 upVector = Vector3.up;

            Vector3 rightVector = Vector3.Cross(upVector, fromHandToHit).normalized;

            // Step 4: Compute point C
            Vector3 curvePeak = middleFromHandToHit + (rightVector * 0.3f);

            Vector3 curvePeakBack = middleFromHandToHit - (rightVector * 0.3f);

            // If item is too low, crouch to reach
            if (_hands.SelectedHand.HandBone.transform.position.y - hitTargetPosition.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            float duration = 0.2f * Mathf.Max(fromHandToHit.magnitude, 2);

            _hands.SelectedHand.PickupTargetLocker.transform.rotation = _hands.SelectedHand.HandBone.rotation;

            Quaternion newRotation = Quaternion.FromToRotation(_hands.SelectedHand.PickupTargetLocker.transform.up, fromHandToHit) * _hands.SelectedHand.PickupTargetLocker.transform.rotation;

            // Define the points for the parabola
            Vector3[] path = new Vector3[] {
                _hands.SelectedHand.PickupTargetLocker.transform.position,
                curvePeak,
                hitTargetPosition,
                curvePeakBack, // Final position
            };

            Vector3 directionFromTransformToTarget = fromHandToHit;
            directionFromTransformToTarget.y = 0f;

            Quaternion rotationPlayer = Quaternion.LookRotation(directionFromTransformToTarget);

            transform.DORotate(rotationPlayer.eulerAngles, duration / 2);  
            
            // Create a DOTween sequence
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(DOTween.To(()=>  _hands.SelectedHand.PickupIkConstraint.weight, x =>  _hands.SelectedHand.PickupIkConstraint.weight = x, 1f, duration));
            mySequence.Append(DOTween.To(()=>  _hands.SelectedHand.PickupIkConstraint.weight, x =>  _hands.SelectedHand.PickupIkConstraint.weight = x, 0f, duration));

            // Animate the GameObject along the path in a smooth catmullRom curve motion
            _hands.SelectedHand.PickupTargetLocker.transform.DOPath(path, duration, PathType.CatmullRom).
                OnComplete(
                () =>
                {
                    GetComponent<HumanoidAnimatorController>().Crouch(false);
                }
                ); 

            _hands.SelectedHand.PickupTargetLocker.transform.DORotate(newRotation.eulerAngles, duration, RotateMode.FastBeyond360);


            // Create the path points relative to the player transform
            Vector3 startPoint = Vector3.zero;                             // Start at the player's local position (0,0,0)
            Vector3 controlPoint = startPoint + new Vector3(0, 2f, 0); // Control point for the curve
            Vector3 endPoint = startPoint + new Vector3(1f, 0, 0);   // End point relative to the player's local position

            // Create the path using a Vector3 array
            Vector3[] path2 = new Vector3[] { startPoint, controlPoint, endPoint };

            // Move the object along the local path relative to the player
            transform.DOLocalPath(path2, duration, PathType.CatmullRom)
                .SetEase(Ease.OutQuad)
                .OnStart(() => {
                    // Set the initial position of the object to the player's position
                    _hands.SelectedHand.ShoulderWeaponPivot.position = transform.position;
                })
                .OnComplete(() => {
                    Debug.Log("Animation complete!");
                });

        }

        [Client]
        private void SetUpHit(Hand mainHand)
        {
            mainHand.PickupTargetLocker.transform.position = mainHand.HandBone.transform.position;
            mainHand.PickupTargetLocker.transform.rotation = mainHand.HandBone.transform.rotation;
        }

    }
}
