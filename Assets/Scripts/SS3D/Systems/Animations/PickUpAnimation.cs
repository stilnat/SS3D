using DG.Tweening;
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


        private float _itemMoveDuration;

        private float _itemReachDuration;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        private Sequence _pickUpSequence;

        public bool IsPicking { get; private set; }

        [Server]
        public void Pickup(Item item, float timeToMoveBackItem, float timeToReachItem, float delay = 0f)
        {
            ObserverPickUp(item, timeToMoveBackItem, timeToReachItem, delay);
        }

        [Server]
        public void CancelPickup(Hand hand)
        {
              ObserverCancelPickUp(hand);
        }

        [ObserversRpc]
        private void ObserverCancelPickUp(Hand hand)
        {
            Debug.Log("cancel pick up animation");

            _pickUpSequence?.Kill();

            Sequence sequence = DOTween.Sequence();

            // Those times are to keep the speed of movements pretty much the same as when it was reaching
            float timeToCancelHold = (1 - hand.HoldIkConstraint.weight) * _itemReachDuration;
            float timeToCancelLookAt = (1 - _lookAtConstraint.weight) * _itemReachDuration;
            float timeToCancelPickup = (1 - hand.PickupIkConstraint.weight) * _itemReachDuration;

            sequence.Append(DOTween.To(() => hand.HoldIkConstraint.weight, x => hand.HoldIkConstraint.weight = x, 0f, timeToCancelHold));
            sequence.Join(DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 0f, timeToCancelLookAt));
            sequence.Join(DOTween.To(() => hand.PickupIkConstraint.weight, x => hand.PickupIkConstraint.weight = x, 0f, timeToCancelPickup));
            GetComponent<HumanoidAnimatorController>().Crouch(false);
        }

        [ObserversRpc(BufferLast = true)]
        private void ObserverPickUp(Item item,  float timeToMoveBackItem, float timeToReachItem, float delay)
        {
            _itemMoveDuration = timeToMoveBackItem;
            _itemReachDuration = timeToReachItem;
            PickupAnimate(item, delay); 
        }


        [Client]
        private void PickupAnimate(Item item, float delay)
        {
            _hands.TryGetOppositeHand(_hands.SelectedHand, out Hand secondaryHand);

            bool withTwoHands = secondaryHand != null && secondaryHand.Empty && item.Holdable.CanHoldTwoHand;

            SetUpPickup(_hands.SelectedHand, secondaryHand, withTwoHands, item);

            PickupReach(item, _hands.SelectedHand, secondaryHand, withTwoHands, delay);

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
                    secondaryHand, true, item.Holdable);
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
        private void PickupReach(Item item, Hand mainHand, Hand secondaryHand, bool withTwoHands, float delay)
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
            }

            _pickUpSequence = DOTween.Sequence();

            // Start looking at item
            _pickUpSequence.Append(DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change hold and pickup constraint weight of the main hand from 0 to 1
            _pickUpSequence.Join(DOTween.To(() => mainHand.HoldIkConstraint.weight, x =>  mainHand.HoldIkConstraint.weight = x, 1f, _itemReachDuration));

            // When reached for the item, parent it to the item position target locker
            _pickUpSequence.Join(DOTween.To(() => mainHand.PickupIkConstraint.weight, x =>  mainHand.PickupIkConstraint.weight = x, 1f, _itemReachDuration).OnComplete(() => item.transform.parent = mainHand.ItemPositionTargetLocker));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                _pickUpSequence.Join(DOTween.To(() => secondaryHand.HoldIkConstraint.weight, x => secondaryHand.HoldIkConstraint.weight = x, 1f, _itemReachDuration));
                _pickUpSequence.Join(DOTween.To(() => secondaryHand.PickupIkConstraint.weight, x =>secondaryHand.PickupIkConstraint.weight = x, 1f, _itemReachDuration));
            }

            // Once reached, start moving and rotating item toward its constrained position.
            _pickUpSequence.Append(item.transform.DOLocalMove(Vector3.zero, _itemMoveDuration));
            _pickUpSequence.Join(item.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _itemMoveDuration));

            // At the same time stop looking at the item and uncrouch
            _pickUpSequence.Join(DOTween.To(() => _lookAtConstraint.weight, x => _lookAtConstraint.weight = x, 0f, _itemMoveDuration).OnStart(Uncrouch));

            // At the same time start getting the right rotation for the hand
            _pickUpSequence.Join(DOTween.To(() => mainHand.HoldIkConstraint.data.targetRotationWeight, x => mainHand.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemMoveDuration));

            // At the same time, remove the pickup constraint
            _pickUpSequence.Join(DOTween.To(() => mainHand.PickupIkConstraint.weight, x => mainHand.PickupIkConstraint.weight = x, 0f, _itemMoveDuration));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                _pickUpSequence.Join(DOTween.To(() => secondaryHand.HoldIkConstraint.data.targetRotationWeight, x => secondaryHand.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemMoveDuration));
                _pickUpSequence.Join(DOTween.To(() => secondaryHand.PickupIkConstraint.weight, x => secondaryHand.PickupIkConstraint.weight = x, 0f, _itemMoveDuration));
            }

            _pickUpSequence.SetDelay(delay);
            _pickUpSequence.OnStart(() => IsPicking = true);
            _pickUpSequence.OnComplete(() => IsPicking = false);
        }

        private void Uncrouch()
        {
            GetComponent<HumanoidAnimatorController>().Crouch(false);
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
