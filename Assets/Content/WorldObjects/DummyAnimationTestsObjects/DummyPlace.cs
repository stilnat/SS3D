using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyPlace : MonoBehaviour
    {
        [FormerlySerializedAs("hands")]
        [SerializeField]
        private DummyHands _hands;

        [FormerlySerializedAs("itemReachDuration")]
        [SerializeField]
        private float _itemReachDuration;

        [FormerlySerializedAs("itemMoveDuration")]
        [SerializeField]
        private float _itemMoveDuration;

        [FormerlySerializedAs("holdController")]
        [SerializeField]
        private HoldController _holdController;

        [FormerlySerializedAs("lookAtTargetLocker")]
        [SerializeField]
        private Transform _lookAtTargetLocker;

        [FormerlySerializedAs("lookAtConstraint")]
        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [FormerlySerializedAs("hips")]
        [SerializeField]
        private Transform _hips;

        public bool IsPlacing { get; private set; }

        public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(_hips.position, position) < 1.3f;

        public bool CanPlace(out Vector3 placePoint)
        {
            placePoint = Vector3.zero;

            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Debug.DrawRay(ray.origin, ray.direction, Color.red, 2f);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit) && UnderMaxDistanceFromHips(hit.point))
            {
                Debug.Log(hit.point);
                placePoint = hit.point;
                return true;
            }

            return false;
        }

        public void TryPlace()
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Debug.DrawRay(ray.origin, ray.direction, Color.red, 2f);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit) && UnderMaxDistanceFromHips(hit.point))
            {
                Debug.Log(hit.point);
                StartCoroutine(Place(hit.point));
            }
        }

        public IEnumerator Place(Vector3 placePosition)
        {
            IsPlacing = true;
            DummyHand mainHand = _hands.SelectedHand;
            DummyHand secondaryHand = _hands.GetOtherHand(mainHand.HandType);
            bool withTwoHands = secondaryHand.Empty && _hands.SelectedHand.Item.CanHoldTwoHand;
            Transform placeTarget = mainHand.PlaceTarget;
            GameObject item = mainHand.Item.GameObject;

            SetupPlace(placePosition, item, mainHand, secondaryHand, withTwoHands);

            yield return PlaceReach(mainHand, placeTarget, item);

            yield return PlaceAndPullBack(mainHand, secondaryHand, withTwoHands);

            IsPlacing = false;
        }

        private void SetupPlace(Vector3 placePosition, GameObject item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            // Set up the position the item should be placed on
            _hands.SelectedHand.PlaceTarget.position = placePosition + (0.2f * Vector3.up);

            // Unparent item so its not constrained by the multi-position constrain anymore.
            item.transform.parent = null;

            // set pickup constraint to 1 so that the player can bend to reach at its feet or further in front.
            mainHand.PickupIkConstraint.weight = 1f;

            // Remove hold constraint from second hand if item held with two hands.
            if (withTwoHands)
            {
                secondaryHand.PickupIkConstraint.weight = 1f;
            }

            // Place look at target at place item position
            _lookAtTargetLocker.transform.parent = null;
            _lookAtTargetLocker.position = placePosition;
        }

        private IEnumerator PlaceReach(DummyHand mainHand, Transform placeTarget, GameObject item)
        {
            // Turn character toward the position to place the item.
            if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, placeTarget, _itemReachDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - placeTarget.position.y > 0.3)
            {
                GetComponent<DummyAnimatorController>().Crouch(true);
            }

            // Slowly increase looking at place item position
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _itemReachDuration));

            // Slowly move item toward the position it should be placed.
            yield return DummyTransformHelper.LerpTransform(item.transform, placeTarget, _itemMoveDuration, true, false, false);
        }

        private IEnumerator PlaceAndPullBack(DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
        {
            GetComponent<DummyAnimatorController>().Crouch(false);

            mainHand.RemoveItem();

            mainHand.PickupTargetLocker.parent = null;

            // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 1f, 0f, _itemReachDuration));

            // Slowly stop looking at item place position
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _itemReachDuration));

            // reproduce changes on second hand
            if (withTwoHands)
            {
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.PickupIkConstraint.weight = x, 1f, 0f, _itemReachDuration));
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.HoldIkConstraint.weight = x, 1f, 0f, _itemReachDuration));
            }

            // Slowly stop trying to hold item
            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 1f, 0f, _itemReachDuration);

            // Catch two hands holdable item in other hand with main hand, just freed.
            if (secondaryHand.Full && secondaryHand.Item.CanHoldTwoHand)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.Item, true, _itemReachDuration, false);
                _holdController.MovePickupAndHoldTargetLocker(mainHand, true, _hands.GetItem(true, mainHand));

                yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 0f, 1f, _itemReachDuration / 2);
            }
        }
    }
}
