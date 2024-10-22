using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class PlaceAnimation : IProceduralAnimation
    {

        public event Action<IProceduralAnimation> OnCompletion;

        private float _itemReachPlaceDuration;

        private float _handMoveBackDuration;

        private Sequence _placeSequence;

        private ProceduralAnimationController _controller;

        private Hand _mainHand;

        private Item _item;

        public void ServerPerform(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay) { }

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            Debug.Log("Start animate place item");

            _itemReachPlaceDuration = time/2;
            _handMoveBackDuration = time/2;
            _controller = proceduralAnimationController;
            Debug.Log(mainHand);
            _mainHand = mainHand;
            _item = target.GetComponent<Item>();

            bool withTwoHands = secondaryHand.Empty && target.GetComponent<Item>().Holdable.CanHoldTwoHand;

            SetupPlace(targetPosition, target.gameObject, mainHand, secondaryHand, withTwoHands);

            Place(mainHand, secondaryHand, withTwoHands, targetPosition, target.GetComponent<Item>());
        }

        public void Cancel()
        {
            Debug.Log("Cancel place animation");
            _placeSequence.Kill();

            Sequence cancelSequence = DOTween.Sequence();

            float timeToCancelPlace = _controller.LookAtConstraint.weight * _handMoveBackDuration;

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            _mainHand.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _controller.HoldController.MovePickupAndHoldTargetLocker(_mainHand, false, _item.Holdable);

            // Move and rotate item toward its constrained position.
            _item.transform.parent = _mainHand.ItemPositionTargetLocker;
            cancelSequence.Append(_item.transform.DOLocalMove(Vector3.zero, timeToCancelPlace));
            cancelSequence.Join(_item.transform.DOLocalRotate(Quaternion.identity.eulerAngles, timeToCancelPlace));

            // Stop looking at item
            cancelSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, timeToCancelPlace));

            // At the same time, remove the pickup constraint
            cancelSequence.Join(DOTween.To(() => _mainHand.PickupIkConstraint.weight, x => _mainHand.PickupIkConstraint.weight = x, 0f, timeToCancelPlace));

            // put back hold constraint weight of the main hand to 1
            cancelSequence.Join(DOTween.To(() => _mainHand.HoldIkConstraint.weight, x => _mainHand.HoldIkConstraint.weight = x, 1f, timeToCancelPlace));

            cancelSequence.OnStart(() =>
            {
                _controller.AnimatorController.Crouch(false);
            });
        }

        [Client]
        private void SetupPlace(Vector3 placePosition, GameObject item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            // Set up the position the item should be placed on 
            mainHand.PlaceTarget.position = placePosition + (0.2f * Vector3.up);

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
            _controller.LookAtTargetLocker.transform.parent = null;
            _controller.LookAtTargetLocker.position = placePosition;
        }

        [Client]
        private void Place(Hand mainHand, Hand secondaryHand, bool withTwoHands, Vector3 placeTarget, Item item)
        {
            // Turn character toward the position to place the item.
            if (_controller.PositionController.Position != PositionType.Sitting)
            {
                //_orientTowardTarget = StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, placeTarget, _itemReachPlaceDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - placeTarget.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            _placeSequence = DOTween.Sequence();

            // Slowly increase looking at place item position
            _placeSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _itemReachPlaceDuration));

            // At the same time, Slowly move item toward the position it should be placed.
            _placeSequence.Join(item.transform.DOMove(placeTarget, _handMoveBackDuration));

            _placeSequence.Append(DOTween.To(() => mainHand.PickupIkConstraint.weight, x => mainHand.PickupIkConstraint.weight = x, 1f, _itemReachPlaceDuration).OnComplete(() =>
            {
                _controller.AnimatorController.Crouch(false);
                mainHand.PickupTargetLocker.parent = null;
            }));


            // Then, Slowly stop looking at item place position
            _placeSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
            _placeSequence.Join(DOTween.To(() => mainHand.PickupIkConstraint.weight, x => mainHand.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            _placeSequence.Join(DOTween.To(() => mainHand.HoldIkConstraint.weight, x => mainHand.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // reproduce changes on second hand
            if (withTwoHands)
            {
                _placeSequence.Join(DOTween.To(() => secondaryHand.PickupIkConstraint.weight, x => secondaryHand.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
                _placeSequence.Join(DOTween.To(() => secondaryHand.HoldIkConstraint.weight, x => secondaryHand.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            }

            // Catch two hands holdable item in other hand with main hand, just freed.
            if (secondaryHand.Full && secondaryHand.ItemInHand.Holdable is not null && secondaryHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                _controller.HoldController.UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.ItemInHand.Holdable, true, _itemReachPlaceDuration, false);
                _controller.HoldController.MovePickupAndHoldTargetLocker(mainHand, true, item.Holdable);

                _placeSequence.Append(DOTween.To(() => mainHand.HoldIkConstraint.weight, x => mainHand.HoldIkConstraint.weight = x, 1f, _itemReachPlaceDuration/2));
            }

            _placeSequence.OnComplete(() => OnCompletion?.Invoke(this));
        }



    }
}
