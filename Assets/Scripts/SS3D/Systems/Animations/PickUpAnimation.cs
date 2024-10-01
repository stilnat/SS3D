using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class PickUpAnimation : NetworkBehaviour
    {

        [SerializeField]
        private float _itemMoveDuration;

        [SerializeField]
        private float _itemReachDuration;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private Transform _hips;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        private Coroutine _pickupCoroutine;

        public bool IsPicking { get; private set; }

        public float ItemReachDuration => _itemReachDuration;

        [Server]
        public void Pickup(Item item)
        {
            ObserverPickUp(item);
        }

        [Client]
        public void CancelPickup(Hand hand, Item tem)
        {
            Debug.Log("cancel pick up animation");
            StopCoroutine(_pickupCoroutine);

            // Those times are to keep the speed of movements pretty much the same as when it was reaching
            float timeToCancelHold = (1 - hand.HoldIkConstraint.weight) * _itemReachDuration;
            float timeToCancelLookAt = (1 - _lookAtConstraint.weight) * _itemReachDuration;
            float timeToCancelPickup = (1 - hand.PickupIkConstraint.weight) * _itemReachDuration;

            // Change hold constraint weight of the main hand from 0 to 1
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => hand.HoldIkConstraint.weight = x, hand.HoldIkConstraint.weight, 0f, timeToCancelHold));

            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, _lookAtConstraint.weight, 0f, timeToCancelLookAt));

            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => hand.PickupIkConstraint.weight = x, hand.PickupIkConstraint.weight, 0f, timeToCancelPickup));

            GetComponent<HumanoidAnimatorController>().Crouch(false);
        }

        [ObserversRpc]
        private void ObserverPickUp(Item item)
        {
            _pickupCoroutine = StartCoroutine(PickupAnimate(item)); 
        }


        [Client]
        private IEnumerator PickupAnimate(Item item)
        {
            IsPicking = true;

            if (!_hands.TryGetOppositeHand(_hands.SelectedHand, out Hand secondaryHand))
            {
                yield break;
            }

            bool withTwoHands = secondaryHand.Empty && item.Holdable.CanHoldTwoHand;

            SetUpPickup(_hands.SelectedHand, secondaryHand, withTwoHands, item);

            yield return PickupReach(item, _hands.SelectedHand, secondaryHand, withTwoHands);

            yield return PickupPullBack(item, _hands.SelectedHand, secondaryHand, withTwoHands);
            IsPicking = false;
        }

        [Client]
        private void SetUpPickup(Hand mainHand, Hand secondaryHand, bool withTwoHands, Item item)
        {
            _holdController.UpdateItemPositionConstraintAndRotation(mainHand, item.Holdable, withTwoHands, 0f, false);

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            mainHand.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _holdController.MovePickupAndHoldTargetLocker(mainHand, false, item.Holdable);

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

        [Client]
        private IEnumerator PickupReach(Item item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            // Rotate player toward item
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, item.transform, _itemReachDuration, false, true));
            }

            // If item is too low, crouch to reach
            if (mainHand.HandBone.transform.position.y - item.transform.position.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);

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

        [Client]
        private IEnumerator PickupPullBack(Item item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            GetComponent<HumanoidAnimatorController>().Crouch(false);

            // Move item toward its constrained position.
            StartCoroutine(TransformHelper.LerpTransform(item.transform, _hands.SelectedHand.ItemPositionTargetLocker, _itemMoveDuration));

            // if an item held with two hands in the unselected hand, change it with a single hand hold
            if (secondaryHand.Full && secondaryHand.ItemInHand.Holdable != null && secondaryHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(
                    secondaryHand, secondaryHand.ItemInHand.Holdable, false, _itemMoveDuration, false);
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
        [Client]
        private void OrientTargetForHandRotation(Hand hand)
        {
            Vector3 armTargetDirection = hand.PickupTargetLocker.position - hand.UpperArm.position;

            Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);

            targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

            hand.PickupTargetLocker.rotation = targetRotation;
        }
    }
}
