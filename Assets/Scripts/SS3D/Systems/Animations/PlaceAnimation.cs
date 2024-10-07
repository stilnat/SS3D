using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class PlaceAnimation : NetworkBehaviour
    {

        private float _itemReachPlaceDuration;

        private float _handMoveBackDuration;

        private Coroutine _placeCoroutine;
        private Coroutine _orientTowardTarget;
        private Coroutine _lookTowardTarget;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [Server]
        public void Place(Vector3 placePosition, Item item, float timeToMoveBackHand, float timeToReachPlaceItem)
        {
            Debug.Log("On server place");
            ObserverPlace(placePosition, item, timeToMoveBackHand, timeToReachPlaceItem);
        }

        [ObserversRpc]
        private void ObserverPlace(Vector3 placePosition, Item item, float timeToMoveBackHand, float timeToReachPlaceItem)
        {
            _itemReachPlaceDuration = timeToReachPlaceItem;
            _handMoveBackDuration = timeToMoveBackHand;
            _placeCoroutine = StartCoroutine(PlaceAnimate(placePosition, item));
        }

        [Client]
        private IEnumerator PlaceAnimate(Vector3 placePosition, Item item)
        {
            Debug.Log("Start animate place item");
            Hand mainHand = _hands.SelectedHand;
            _hands.TryGetOppositeHand(mainHand, out Hand secondaryHand);
            bool withTwoHands = secondaryHand.Empty && item.Holdable.CanHoldTwoHand;
            Transform placeTarget = mainHand.PlaceTarget;

            SetupPlace(placePosition, item.gameObject, mainHand, secondaryHand, withTwoHands);

            yield return PlaceReach(mainHand, placeTarget, item.gameObject);

            yield return PlaceAndPullBack(mainHand, secondaryHand, withTwoHands, item);
        }

        [Client]
        private void SetupPlace(Vector3 placePosition, GameObject item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            // Set up the position the item should be placed on 
            _hands.SelectedHand.PlaceTarget.position = placePosition + (0.2f * Vector3.up);

            // Unparent item so its not constrained by the multi-position constrain anymore.
            //item.transform.parent = null;

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

        [Client]
        private IEnumerator PlaceReach(Hand mainHand, Transform placeTarget, GameObject item)
        {
            // Turn character toward the position to place the item.
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                _orientTowardTarget = StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, placeTarget, _itemReachPlaceDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - placeTarget.position.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            // Slowly increase looking at place item position
            _lookTowardTarget = StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _itemReachPlaceDuration));

            // Slowly move item toward the position it should be placed.
            yield return TransformHelper.LerpTransform(item.transform, placeTarget, _handMoveBackDuration, true, false, false);

            Debug.Log("unparent item in place animation");

            // unparent item from item position target locker
            item.transform.parent = null;
        }

        [Client]
        private IEnumerator PlaceAndPullBack(Hand mainHand, Hand secondaryHand, bool withTwoHands, Item item)
        {
            GetComponent<HumanoidAnimatorController>().Crouch(false);

            mainHand.PickupTargetLocker.parent = null;

            // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 1f, 0f, _itemReachPlaceDuration));

            // Slowly stop looking at item place position
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _itemReachPlaceDuration));

            // reproduce changes on second hand
            if (withTwoHands)
            {
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.PickupIkConstraint.weight = x, 1f, 0f, _itemReachPlaceDuration));
                StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => secondaryHand.HoldIkConstraint.weight = x, 1f, 0f, _itemReachPlaceDuration));
            }

            // Slowly stop trying to hold item
            yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 1f, 0f, _itemReachPlaceDuration);

            // Catch two hands holdable item in other hand with main hand, just freed.
            if (secondaryHand.Full && secondaryHand.ItemInHand.Holdable is not null && secondaryHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.ItemInHand.Holdable, true, _itemReachPlaceDuration, false);
                _holdController.MovePickupAndHoldTargetLocker(mainHand, true, item.Holdable);

                yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.HoldIkConstraint.weight = x, 0f, 1f, _itemReachPlaceDuration / 2);
            }
        }

        [Server]
        public void CancelPlace(Hand hand, Item item)
        {
            ObserverCancelPlace(hand, item);
        }

        [ObserversRpc]
        private void ObserverCancelPlace(Hand hand, Item item)
        {
            Debug.Log("Cancel place animation");
            StopCoroutine(_placeCoroutine);
            StopCoroutine(_orientTowardTarget);
            StopCoroutine(_lookTowardTarget);

            // This allow
            float timeToCancelPlace = _lookAtConstraint.weight * _handMoveBackDuration;

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            hand.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _holdController.MovePickupAndHoldTargetLocker(hand, false, item.Holdable);


            // Move back item toward its constrained position.
            StartCoroutine(TransformHelper.LerpTransform(item.transform, hand.ItemPositionTargetLocker, timeToCancelPlace));

            // Stop looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, _lookAtConstraint.weight, 0f, timeToCancelPlace));

            // remove pick up constraint
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => hand.PickupIkConstraint.weight = x, hand.PickupIkConstraint.weight, 0f, timeToCancelPlace));

            // put back hold constraint weight of the main hand to 1
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => hand.HoldIkConstraint.weight = x, hand.HoldIkConstraint.weight, 1f, timeToCancelPlace));

            GetComponent<HumanoidAnimatorController>().Crouch(false);

            // Place item on constrained item position
            item.transform.parent = hand.ItemPositionTargetLocker;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
    }
}
