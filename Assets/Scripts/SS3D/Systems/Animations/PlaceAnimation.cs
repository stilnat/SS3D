using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class PlaceAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private float _itemReachPlaceDuration;

        private float _handMoveBackDuration;

        private Sequence _placeSequence;

        private ProceduralAnimationController _controller;

        private Hand _mainHand;

        private Hand _secondaryHand;

        private Item _item;

        public PlaceAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand)
        {
            _placeSequence = DOTween.Sequence();
            _itemReachPlaceDuration = time / 2;
            _handMoveBackDuration = time / 2;
            _controller = proceduralAnimationController;
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
        }

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            Debug.Log("Start animate place item");

            _item = target.GetComponent<Item>();

            bool withTwoHands = secondaryHand.Empty && _item.Holdable.CanHoldTwoHand;

            SetupPlace(targetPosition, target.gameObject, mainHand, secondaryHand, withTwoHands);

            Place(mainHand, secondaryHand, withTwoHands, targetPosition, _item);
        }

        public override void Cancel()
        {
            Debug.Log("Cancel place animation");
            _placeSequence.Kill();

            Sequence cancelSequence = DOTween.Sequence();

            float timeToCancelPlace = _controller.LookAtConstraint.weight * _handMoveBackDuration;

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            _mainHand.Hold.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _controller.HoldController.MovePickupAndHoldTargetLocker(_mainHand, false, _item.Holdable);

            // Move and rotate item toward its constrained position.
            _item.transform.parent = _mainHand.Hold.ItemPositionTargetLocker;
            cancelSequence.Append(_item.transform.DOLocalMove(Vector3.zero, timeToCancelPlace));
            cancelSequence.Join(_item.transform.DOLocalRotate(Quaternion.identity.eulerAngles, timeToCancelPlace));

            // Stop looking at item
            cancelSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, timeToCancelPlace));

            // At the same time, remove the pickup constraint
            cancelSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, timeToCancelPlace));

            // put back hold constraint weight of the main hand to 1
            cancelSequence.Join(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.weight, x => _mainHand.Hold.HoldIkConstraint.weight = x, 1f, timeToCancelPlace));

            cancelSequence.OnStart(() =>
            {
                _controller.AnimatorController.Crouch(false);
            });
        }

        [Client]
        private void SetupPlace(Vector3 placePosition, GameObject item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            // Set up the position the item should be placed on 
            mainHand.Hold.PlaceTarget.position = placePosition + (0.2f * Vector3.up);

            // Unparent item so its not constrained by the multi-position constrain anymore.
            //item.transform.parent = null;

            // set pickup constraint to 1 so that the player can bend to reach at its feet or further in front.
            mainHand.Hold.PickupIkConstraint.weight = 1f;

            // Remove hold constraint from second hand if item held with two hands.
            if (withTwoHands)
            {
                secondaryHand.Hold.PickupIkConstraint.weight = 1f;
            }

            // Place look at target at place item position
            _controller.LookAtTargetLocker.transform.parent = null;
            _controller.LookAtTargetLocker.position = placePosition;
        }

        [Client]
        private void Place(Hand mainHand, Hand secondaryHand, bool withTwoHands, Vector3 placeTarget, Item item)
        {
            // Turn character toward the position to place the item.
            TryRotateTowardTargetPosition(_placeSequence, _controller.transform, _controller, _itemReachPlaceDuration, placeTarget);

            if (mainHand.HandBone.transform.position.y - placeTarget.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            // Slowly increase looking at place item position
            _placeSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _itemReachPlaceDuration));

            // At the same time, Slowly move item toward the position it should be placed.
            _placeSequence.Join(item.transform.DOMove(placeTarget, _handMoveBackDuration));

            _placeSequence.Append(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachPlaceDuration).OnComplete(() =>
            {
                _controller.AnimatorController.Crouch(false);
                mainHand.Hold.PickupTargetLocker.parent = null;
            }));

            // Then, Slowly stop looking at item place position
            _placeSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
            _placeSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            _placeSequence.Join(DOTween.To(() => mainHand.Hold.HoldIkConstraint.weight, x => mainHand.Hold.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // reproduce changes on second hand
            if (withTwoHands)
            {
                _placeSequence.Join(DOTween.To(() => secondaryHand.Hold.PickupIkConstraint.weight, x => secondaryHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
                _placeSequence.Join(DOTween.To(() => secondaryHand.Hold.HoldIkConstraint.weight, x => secondaryHand.Hold.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            }

            _placeSequence.OnComplete(() => OnCompletion?.Invoke(this));
        }
    }
}
