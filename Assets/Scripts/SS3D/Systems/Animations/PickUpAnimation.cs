using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
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
    public class PickUpAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private Hand _mainHand;

        private Hand _secondaryHand;

        private float _itemReachDuration;

        public bool IsPicking { get; private set; }

        public PickUpAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand)
            : base(time, proceduralAnimationController)
        {
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
            _itemReachDuration = time / 2;
        }

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            Item item = target.GetComponent<Item>();

            bool withTwoHands = secondaryHand != null && secondaryHand.Empty && item.Holdable.CanHoldTwoHand;

            SetUpPickup(mainHand, secondaryHand, withTwoHands, item, proceduralAnimationController.HoldController, proceduralAnimationController.LookAtTargetLocker);

            PickupReach(item, mainHand, secondaryHand, withTwoHands);
        }

        public override void Cancel()
        {
            Debug.Log("cancel pick up animation");

            InteractionSequence?.Kill();

            Sequence sequence = DOTween.Sequence();

            // Those times are to keep the speed of movements pretty much the same as when it was reaching
            float timeToCancelHold = (1 - _mainHand.Hold.HoldIkConstraint.weight) * _itemReachDuration;
            float timeToCancelLookAt = (1 - Controller.LookAtConstraint.weight) * _itemReachDuration;
            float timeToCancelPickup = (1 - _mainHand.Hold.PickupIkConstraint.weight) * _itemReachDuration;

            sequence.Append(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.weight, x => _mainHand.Hold.HoldIkConstraint.weight = x, 0f, timeToCancelHold));
            sequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, timeToCancelLookAt));
            sequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, timeToCancelPickup));
            Controller.PositionController.TryToStandUp();
        }

        [Client]
        private void SetUpPickup(Hand mainHand, Hand secondaryHand, bool withTwoHands, Item item, HoldController holdController, Transform lookAtTargetLocker)
        {
            holdController.SetItemConstraintPositionAndRotation(mainHand, item.Holdable);

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            mainHand.Hold.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            holdController.MovePickupAndHoldTargetLocker(mainHand, false, item.Holdable);

            // Orient hand in a natural position to reach for item.
            OrientTargetForHandRotation(mainHand);

            // Needed if this has been changed elsewhere
            mainHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Needed as the hand need to reach when picking up in an extended position, it looks unnatural
            // if it takes directly the rotation of the hold.
            mainHand.Hold.HoldIkConstraint.data.targetRotationWeight = 0f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                holdController.MovePickupAndHoldTargetLocker(
                    secondaryHand, true, item.Holdable);
                OrientTargetForHandRotation(secondaryHand);
                secondaryHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;
                secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight = 0f;
            }

            // Set up the look at target locker on the item to pick up.
            lookAtTargetLocker.transform.parent = item.transform;
            lookAtTargetLocker.localPosition = Vector3.zero;
            lookAtTargetLocker.localRotation = Quaternion.identity;
        }

        [Client]
        private void PickupReach(Item item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            // Rotate player toward item
            TryRotateTowardTargetPosition(Controller.transform, _itemReachDuration, item.transform.position);

            AdaptPosition(Controller.PositionController, mainHand, item.transform.position);

            // Start looking at item
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change hold and pickup constraint weight of the main hand from 0 to 1
            InteractionSequence.Join(DOTween.To(() => mainHand.Hold.HoldIkConstraint.weight, x =>  mainHand.Hold.HoldIkConstraint.weight = x, 1f, _itemReachDuration));

            // When reached for the item, parent it to the item position target locker
            InteractionSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x =>  mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration).OnComplete(() =>
                item.transform.parent = mainHand.Hold.ItemPositionTargetLocker));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                InteractionSequence.Join(DOTween.To(() => secondaryHand.Hold.HoldIkConstraint.weight, x => secondaryHand.Hold.HoldIkConstraint.weight = x, 1f, _itemReachDuration));
                InteractionSequence.Join(DOTween.To(() => secondaryHand.Hold.PickupIkConstraint.weight, x =>secondaryHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));
            }

            // Once reached, start moving and rotating item toward its constrained position.
            InteractionSequence.Append(item.transform.DOLocalMove(Vector3.zero, _itemReachDuration));
            InteractionSequence.Join(item.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _itemReachDuration));

            // At the same time stop looking at the item and uncrouch
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _itemReachDuration).
                OnStart(() => RestorePosition(Controller.PositionController)));

            // At the same time start getting the right rotation for the hand
            InteractionSequence.Join(DOTween.To(() => mainHand.Hold.HoldIkConstraint.data.targetRotationWeight, x => mainHand.Hold.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemReachDuration));

            // At the same time, remove the pickup constraint, and if the second hand has an item, update its hold
            InteractionSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                InteractionSequence.Join(DOTween.To(() => secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight, x => secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemReachDuration));
                InteractionSequence.Join(DOTween.To(() => secondaryHand.Hold.PickupIkConstraint.weight, x => secondaryHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));
            }

            InteractionSequence.OnStart(() => IsPicking = true);
            InteractionSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
                IsPicking = false;
            });
        }
    }
}
