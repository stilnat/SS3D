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

        private float _interactionMoveDuration;
        private ProceduralAnimationController _controller;

        private Sequence _interactSequence;

        public event Action<IProceduralAnimation> OnCompletion;
        public void ServerPerform(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay) { }

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _interactionMoveDuration = time;

            SetupInteract(mainHand, targetPosition);
            AlignHandWithShoulder(targetPosition, mainHand);
            

            _interactSequence = DOTween.Sequence();

            // Start looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, time));


            // Rotate player toward item
            if (_controller.PositionController.Position != PositionType.Sitting)
            {
                //StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, targetPosition, _interactionMoveDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - targetPosition.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            _interactSequence.Join(mainHand.Hold.PickupTargetLocker.DOMove(targetPosition, _interactionMoveDuration).OnStart(() =>  mainHand.Hold.PlayAnimation(interactionType)));

            // Stop looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, time));

            // Stop reaching for the position of interaction
            _interactSequence.Append(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, time).OnComplete(() => mainHand.Hold.StopAnimation()));

            _interactSequence.OnComplete(() =>
            {
                _controller.AnimatorController.Crouch(false);
                OnCompletion?.Invoke(this);
            });
        }

        public void Cancel()
        {

        }

        private void SetupInteract(Hand mainHand, Vector3 interactionPoint)
        {
            // disable position constraint the time of the interaction
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
            mainHand.Hold.PickupIkConstraint.weight = 1f;
            _controller.LookAtTargetLocker.position = interactionPoint;
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
