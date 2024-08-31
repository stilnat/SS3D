using DummyStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class Grab : MonoBehaviour
    {
        public event EventHandler<bool> OnGrab;

        private GameObject _grabbedObject;

        private FixedJoint _fixedJoint;

        [FormerlySerializedAs("grabRange")]
        [SerializeField]
        private float _grabRange = 2f;

        [FormerlySerializedAs("grabbableLayer")]
        [SerializeField]
        private LayerMask _grabbableLayer;

        [SerializeField]
        private float _jointBreakForce = 250f;

        [FormerlySerializedAs("holdController")]
        [SerializeField]
        private HoldController _holdController;

        [FormerlySerializedAs("itemReachDuration")]
        [SerializeField]
        private float _itemReachDuration;

        [FormerlySerializedAs("itemMoveDuration")]
        [SerializeField]
        private float _itemMoveDuration;

        [FormerlySerializedAs("hands")]
        [SerializeField]
        private DummyHands _hands;

        [FormerlySerializedAs("lookAtTargetLocker")]
        [SerializeField]
        private Transform _lookAtTargetLocker;

        [FormerlySerializedAs("lookAtConstraint")]
        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (_grabbedObject == null)
                {
                    TryGrab();
                }
                else
                {
                    ReleaseGrab();
                }
            }
        }

        protected void OnJointBreak(float breakForce)
        {
            Debug.Log("A joint has just been broken!, force: " + breakForce);
            ReleaseGrab();
        }

        private void TryGrab()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _grabbableLayer))
            {
                Debug.DrawRay(ray.origin, ray.direction * 5, Color.green, 2f);

                _grabbedObject = hit.transform.gameObject;
                Debug.Log("grabbed object = " + _grabbedObject);

                if (_grabbedObject.TryGetComponent(out GrabbableBodyPart bodyPart))
                {
                    StartCoroutine(GrabObject(_hands.SelectedHand, _hands.UnselectedHand, bodyPart));
                }
            }
        }

        private IEnumerator GrabObject(DummyHand mainHand, DummyHand secondaryHand, GrabbableBodyPart bodyPart)
        {
            SetUpGrab(bodyPart, mainHand, secondaryHand, false);

            yield return GrabReach(bodyPart, mainHand, secondaryHand, false);
            yield return GrabPullBack(bodyPart, mainHand, secondaryHand, false);
        }

        private void ReleaseGrab()
        {
            if (_grabbedObject != null)
            {
                if (_grabbedObject.TryGetComponent<Rigidbody>(out Rigidbody grabbedRb))
                {
                    grabbedRb.detectCollisions = true; // Enable collisions again
                }

                Destroy(_fixedJoint);
                _grabbedObject = null;

                OnGrab?.Invoke(this, false);
            }
        }

        private void SetUpGrab(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            mainHand.SetParentTransformTargetLocker(TargetLockerType.Pickup, _grabbedObject.transform);

            // Needed if this has been changed elsewhere
            mainHand.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                secondaryHand.PickupIkConstraint.data.tipRotationWeight = 1f;
            }

            // Set up the look at target locker on the item to pick up.
            _lookAtTargetLocker.parent = item.transform;
            _lookAtTargetLocker.localPosition = Vector3.zero;
            _lookAtTargetLocker.localRotation = Quaternion.identity;

            OrientTargetForHandRotation(mainHand);
        }

        private IEnumerator GrabReach(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, item.transform, _itemReachDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - item.transform.position.y > 0.3)
            {
                GetComponent<DummyAnimatorController>().Crouch(true);

                yield return new WaitForSeconds(0.25f);
            }

            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _itemReachDuration));

            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 0f, 1f, _itemReachDuration);
        }

        private IEnumerator GrabPullBack(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            // those two lines necessary to smooth pulling back
            mainHand.SetParentTransformTargetLocker(TargetLockerType.Pickup, null, false, false);
            mainHand.PickupTargetLocker.transform.position = item.transform.position;

            GetComponent<DummyAnimatorController>().Crouch(false);
            _grabbedObject.transform.position = mainHand.HoldTransform.position;
            _fixedJoint = mainHand.HandBone.gameObject.AddComponent<FixedJoint>();
            Rigidbody grabbedRb = _grabbedObject.GetComponent<Rigidbody>();
            _fixedJoint.connectedBody = grabbedRb;
            grabbedRb.velocity = Vector3.zero;
            _fixedJoint.breakForce = _jointBreakForce;
            grabbedRb.detectCollisions = false; // Disable collisions between the two characters

            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _itemReachDuration));

            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 1f, 0f, _itemMoveDuration);

            Debug.Log("Grabbed object is " + _grabbedObject.name);
            OnGrab?.Invoke(this, true);
        }

        /// <summary>
        /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
        /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
        /// </summary>
        private void OrientTargetForHandRotation(DummyHand hand)
        {
            Vector3 armTargetDirection = hand.PickupTargetLocker.position - hand.UpperArm.position;

            Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);

            targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

            hand.PickupTargetLocker.rotation = targetRotation;
        }
    }
}
