using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class PlaceAnimation : MonoBehaviour
    {
        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private float _itemReachDuration;

        [SerializeField]
        private float _itemMoveDuration;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _hips;

        public bool IsPlacing { get; private set; }

        public IEnumerator Place(Vector3 placePosition)
        {
            IsPlacing = true;
            Hand mainHand = _hands.SelectedHand;
            _hands.TryGetOppositeHand(mainHand, out Hand secondaryHand);
            bool withTwoHands = secondaryHand.Empty && _hands.SelectedHand.ItemInHand.Holdable.CanHoldTwoHand;
            Transform placeTarget = mainHand.PlaceTarget;
            GameObject item = mainHand.ItemInHand.Holdable.GameObject;

            SetupPlace(placePosition, item, mainHand, secondaryHand, withTwoHands);

            yield return PlaceReach(mainHand, placeTarget, item);

            yield return PlaceAndPullBack(mainHand, secondaryHand, withTwoHands);

            IsPlacing = false;
        }

        private void SetupPlace(Vector3 placePosition, GameObject item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
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

        private IEnumerator PlaceReach(Hand mainHand, Transform placeTarget, GameObject item)
        {
            // Turn character toward the position to place the item.
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, placeTarget, _itemReachDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - placeTarget.position.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            // Slowly increase looking at place item position
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _itemReachDuration));

            // Slowly move item toward the position it should be placed.
            yield return TransformHelper.LerpTransform(item.transform, placeTarget, _itemMoveDuration, true, false, false);
        }

        private IEnumerator PlaceAndPullBack(Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            GetComponent<HumanoidAnimatorController>().Crouch(false);

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
            if (secondaryHand.Full && secondaryHand.ItemInHand.Holdable is not null && secondaryHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.ItemInHand.Holdable, true, _itemReachDuration, false);
                _holdController.MovePickupAndHoldTargetLocker(mainHand, true, _hands.GetItem(true, mainHand));

                yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 0f, 1f, _itemReachDuration / 2);
            }
        }
    }
}
