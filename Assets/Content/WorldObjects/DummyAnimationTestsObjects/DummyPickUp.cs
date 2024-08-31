using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyPickUp : MonoBehaviour
    {
        [SerializeField]
        private float _itemMoveDuration;

        [SerializeField]
        private float _itemReachDuration;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private DummyHands _hands;

        [SerializeField]
        private Transform _hips;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(_hips.position, position) < 1.3f;

        protected void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (_hands.SelectedHand.Empty)
            {
                TryPickUp();
            }
        }

        private void TryPickUp()
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit) && UnderMaxDistanceFromHips(hit.point))
            {
                // Check if the collider belongs to a GameObject
                GameObject obj = hit.collider.gameObject;

                // should add conditions to check other objects doesn't require two hands.
                // also check picked up object doesn't require two hands if other hand is full.
                if (obj.TryGetComponent(out DummyItem item))
                {
                    StartCoroutine(PickUp(item));
                }
            }
        }

        private IEnumerator PickUp(DummyItem item)
        {
            GetComponent<DummyAnimatorController>().TriggerPickUp();

            DummyHand secondaryHand = _hands.GetOtherHand(_hands.SelectedHand.HandType);

            bool withTwoHands = secondaryHand.Empty && item.CanHoldTwoHand;

            _hands.SelectedHand.AddItem(item);

            SetUpPickup(_hands.SelectedHand, secondaryHand, withTwoHands, item);

            yield return PickupReach(item, _hands.SelectedHand, secondaryHand, withTwoHands);

            yield return PickupPullBack(item, _hands.SelectedHand, secondaryHand, withTwoHands);
        }

        private void SetUpPickup(DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands, DummyItem item)
        {
            _holdController.UpdateItemPositionConstraintAndRotation(mainHand, mainHand.Item, withTwoHands, 0f, false);

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            mainHand.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _holdController.MovePickupAndHoldTargetLocker(mainHand, false, _hands.GetItem(false, mainHand));

            // Orient hand in a natural position to reach for item.
            OrientTargetForHandRotation(mainHand);

            // Needed if this has been changed elsewhere
            mainHand.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Needed as the hand need to reach when picking up in an extended position, it looks unnatural
            // if it takes directly the rotation of the hold.
            mainHand.HoldIkConstraint.data.targetRotationWeight = 0f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                _holdController.MovePickupAndHoldTargetLocker(
                    secondaryHand, true, _hands.GetItem(true, secondaryHand));
                OrientTargetForHandRotation(secondaryHand);
                secondaryHand.PickupIkConstraint.data.tipRotationWeight = 1f;
                secondaryHand.HoldIkConstraint.data.targetRotationWeight = 0f;
            }

            // Set up the look at target locker on the item to pick up.
            _lookAtTargetLocker.transform.parent = item.transform;
            _lookAtTargetLocker.localPosition = Vector3.zero;
            _lookAtTargetLocker.localRotation = Quaternion.identity;
        }

        private IEnumerator PickupReach(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            // Move player toward item
            if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, item.transform, _itemReachDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - item.transform.position.y > 0.3)
            {
                GetComponent<DummyAnimatorController>().Crouch(true);

                yield return new WaitForSeconds(0.25f);
            }

            // Change hold constraint weight of the main hand from 0 to 1
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 0f, 1f, _itemReachDuration));

            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _itemReachDuration));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.HoldIkConstraint.weight = x, 0f, 1f, _itemReachDuration));
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.PickupIkConstraint.weight = x, 0f, 1f, _itemReachDuration));
            }

            // Change pickup constraint weight of the main hand from 0 to 1
            yield return CoroutineHelper.ModifyValueOverTime(
                x => mainHand.PickupIkConstraint.weight = x, 0f, 1f, _itemReachDuration);
        }

        private IEnumerator PickupPullBack(DummyItem item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            GetComponent<DummyAnimatorController>().Crouch(false);

            // Move item toward its constrained position.
            StartCoroutine(DummyTransformHelper.LerpTransform(item.transform, _hands.SelectedHand.ItemPositionTargetLocker, _itemMoveDuration));

            // if an item held with two hands, change it with a single hand hold
            if (secondaryHand.Full && secondaryHand.Item.CanHoldTwoHand)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.Item, false, _itemMoveDuration, false);
            }

            // Stop looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _itemReachDuration));

            // increase hold constraint rotation
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.data.targetRotationWeight = x, 0f, 1f, _itemReachDuration));

            if (withTwoHands)
            {
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.HoldIkConstraint.data.targetRotationWeight = x, 0f, 1f, _itemReachDuration));
            }

            // Get hand back at its hold position.
            if (withTwoHands)
            {
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.PickupIkConstraint.weight = x, 1f, 0f, _itemMoveDuration));
            }

            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 1f, 0f, _itemMoveDuration);

            // Place item on constrained item position
            item.transform.parent = mainHand.ItemPositionTargetLocker;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            _lookAtConstraint.weight = 0f;
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
