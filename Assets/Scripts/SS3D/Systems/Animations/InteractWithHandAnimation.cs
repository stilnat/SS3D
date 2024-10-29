using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Crafting;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class InteractWithHandAnimation : IProceduralAnimation
    { 

        private float _interactionTime;
        private float _moveHandTime;
        private ProceduralAnimationController _controller;

        private Sequence _interactSequence;

        public event Action<IProceduralAnimation> OnCompletion;
        public void ServerPerform(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay) { }

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _interactionTime = time;
            _moveHandTime = Mathf.Min(time, 0.5f);

            _interactSequence = DOTween.Sequence();

            SetupInteract(mainHand, targetPosition);
            InteractWithHand(mainHand, targetPosition, interactionType);
        }

        public void Cancel()
        {

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

            if (mainHand.HandBone.transform.position.y - targetPosition.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            // Rotate player toward item
            if (_controller.PositionController.Position != PositionType.Sitting)
            {
                Vector3 interactionPointProjected = targetPosition;
                interactionPointProjected.y = _controller.transform.position.y;
                _interactSequence.Join(_controller.transform.DORotate(Quaternion.LookRotation(interactionPointProjected - _controller.transform.position).eulerAngles, _moveHandTime));
            }

            // Start looking at item
            _interactSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _moveHandTime));

            // Move hand toward target
            _interactSequence.Join(mainHand.Hold.PickupTargetLocker.DOMove(targetPosition, _moveHandTime).OnComplete(() =>  mainHand.Hold.PlayAnimation(interactionType)));

            _interactSequence.AppendInterval(_interactionTime - _moveHandTime);

            // Stop looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _moveHandTime).OnStart(() =>
            {
                mainHand.Hold.StopAnimation();
                _controller.AnimatorController.Crouch(false);
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
