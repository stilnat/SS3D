using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Procedural animation for interaction done with hand holding nothing.
    /// </summary>
    public class InteractWithHandAnimation : AbstractProceduralAnimation
    { 
        public override event Action<IProceduralAnimation> OnCompletion;

        private float _interactionTime;
        private float _moveHandTime;
        private ProceduralAnimationController _controller;

        private Hand _hand;

        private Sequence _interactSequence;

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _interactionTime = time;
            _moveHandTime = Mathf.Min(time, 0.5f);
            _hand = mainHand;

            _interactSequence = DOTween.Sequence();

            SetupInteract(mainHand, targetPosition);
            InteractWithHand(mainHand, targetPosition, interactionType);
        }

        public override void Cancel()
        {
            _interactSequence.Kill();

            // Stop looking at item
            DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _moveHandTime);

            // Stop reaching for the position of interaction
            DOTween.To(() => _hand.Hold.PickupIkConstraint.weight, x => _hand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime);

            _hand.Hold.StopAnimation();
        }

        private void SetupInteract(Hand mainHand, Vector3 interactionPoint)
        {
            // disable position constraint the time of the interaction
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
            mainHand.Hold.PickupIkConstraint.weight = 1f;
            mainHand.Hold.PickupTargetLocker.position = mainHand.HandBone.position;
            _controller.LookAtTargetLocker.position = interactionPoint;
        }

        private void InteractWithHand(Hand mainHand, Vector3 targetPosition, InteractionType interactionType)
        {

            AlignHandWithShoulder(targetPosition, mainHand);

            AdaptPosition(_controller.PositionController, mainHand, targetPosition);

            // Rotate player toward item
            _interactSequence = TryRotateTowardTargetPosition(_interactSequence, _controller.transform, _controller, _interactionTime, targetPosition);

            // Start looking at item
            _interactSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _moveHandTime));

            // Move hand toward target
            _interactSequence.Join(mainHand.Hold.PickupTargetLocker.DOMove(targetPosition, _moveHandTime).OnComplete(() =>  mainHand.Hold.PlayAnimation(interactionType)));

            _interactSequence.AppendInterval(_interactionTime - _moveHandTime);

            // Stop looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _moveHandTime).OnStart(() =>
            {
                mainHand.Hold.StopAnimation();
                RestorePosition(_controller.PositionController);
            }));

            // Stop reaching for the position of interaction
            _interactSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime));

            _interactSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
            });
        }

        private void AlignHandWithShoulder(Vector3 interactionPoint, Hand mainHand)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.Hold.UpperArm.transform.position).normalized;
            mainHand.Hold.PickupTargetLocker.rotation = Quaternion.LookRotation(fromShoulderToTarget);
            mainHand.Hold.PickupTargetLocker.rotation *= Quaternion.Euler(90f, 0f,0);
            mainHand.Hold.PickupTargetLocker.rotation *= Quaternion.Euler(0f, 180f,0);
        }
    }
}
