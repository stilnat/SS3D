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
    public class PickUpAnimation : IProceduralAnimation
    {
        private float _itemMoveDuration;

        private float _itemReachDuration;

        private Sequence _pickUpSequence;

        private ProceduralAnimationController _controller;

        private Hand _mainHand;

        public bool IsPicking { get; private set; }
        public event Action<IProceduralAnimation> OnCompletion;

        public void ServerPerform(Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay) { }

        public void ClientPlay(Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _itemMoveDuration = time / 2;
            _itemReachDuration = time / 2;
            _mainHand = mainHand;

            Item item = target.GetComponent<Item>();

            bool withTwoHands = secondaryHand != null && secondaryHand.Empty && item.Holdable.CanHoldTwoHand;

            SetUpPickup(mainHand, secondaryHand, withTwoHands, item, proceduralAnimationController.HoldController, proceduralAnimationController.LookAtTargetLocker);

            PickupReach(item, mainHand, secondaryHand, withTwoHands, delay);
        }

        public void Cancel()
        {
            Debug.Log("cancel pick up animation");

            _pickUpSequence?.Kill();

            Sequence sequence = DOTween.Sequence();

            // Those times are to keep the speed of movements pretty much the same as when it was reaching
            float timeToCancelHold = (1 - _mainHand.HoldIkConstraint.weight) * _itemReachDuration;
            float timeToCancelLookAt = (1 - _controller.LookAtConstraint.weight) * _itemReachDuration;
            float timeToCancelPickup = (1 - _mainHand.PickupIkConstraint.weight) * _itemReachDuration;

            sequence.Append(DOTween.To(() => _mainHand.HoldIkConstraint.weight, x => _mainHand.HoldIkConstraint.weight = x, 0f, timeToCancelHold));
            sequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, timeToCancelLookAt));
            sequence.Join(DOTween.To(() => _mainHand.PickupIkConstraint.weight, x => _mainHand.PickupIkConstraint.weight = x, 0f, timeToCancelPickup));
            _controller.AnimatorController.Crouch(false);
        }


        [Client]
        private void SetUpPickup(Hand mainHand, Hand secondaryHand, bool withTwoHands, Item item, HoldController holdController, Transform lookAtTargetLocker)
        {
            holdController.UpdateItemPositionConstraintAndRotation(mainHand, item.Holdable, withTwoHands, 0f, false);

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            mainHand.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            holdController.MovePickupAndHoldTargetLocker(mainHand, false, item.Holdable);

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
                holdController.MovePickupAndHoldTargetLocker(
                    secondaryHand, true, item.Holdable);
                OrientTargetForHandRotation(secondaryHand);
                secondaryHand.PickupIkConstraint.data.tipRotationWeight = 1f;
                secondaryHand.HoldIkConstraint.data.targetRotationWeight = 0f;
            }

            // Set up the look at target locker on the item to pick up.
            lookAtTargetLocker.transform.parent = item.transform;
            lookAtTargetLocker.localPosition = Vector3.zero;
            lookAtTargetLocker.localRotation = Quaternion.identity;
        }

        [Client]
        private void PickupReach(Item item, Hand mainHand, Hand secondaryHand, bool withTwoHands, float delay)
        {
            // Rotate player toward item
            if (_controller.PositionController.Position != PositionType.Sitting)
            {
                //StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, item.transform, _itemReachDuration, false, true));
            }

            // If item is too low, crouch to reach
            if (mainHand.HandBone.transform.position.y - item.transform.position.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            _pickUpSequence = DOTween.Sequence();

            // Start looking at item
            _pickUpSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

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
            _pickUpSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemMoveDuration).
                OnStart(() => _controller.AnimatorController.Crouch(false)));

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
            _pickUpSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
                IsPicking = false;
            });
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
