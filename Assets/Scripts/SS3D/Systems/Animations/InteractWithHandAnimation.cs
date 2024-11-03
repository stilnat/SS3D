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
        private float _moveHandTime;
        private Hand _hand;


        public InteractWithHandAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand, NetworkBehaviour target)
            : base(time, proceduralAnimationController)
        {
            _hand = mainHand;
            _moveHandTime = time / 2;
        }

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            SetupInteract(mainHand, targetPosition);
            InteractWithHand(mainHand, targetPosition, interactionType);
        }

        public override void Cancel()
        {
            InteractionSequence.Kill();

            // Stop looking at item
            DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveHandTime);

            // Stop reaching for the position of interaction
            DOTween.To(() => _hand.Hold.PickupIkConstraint.weight, x => _hand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime);

            _hand.Hold.StopAnimation();
        }

        private void SetupInteract(Hand mainHand, Vector3 interactionPoint)
        {
            // disable position constraint the time of the interaction
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
            mainHand.Hold.PickupIkConstraint.weight = 1f;
            mainHand.Hold.HandIkTarget.position = mainHand.HandBone.position;
            Controller.LookAtTargetLocker.position = interactionPoint;
        }

        private void InteractWithHand(Hand mainHand, Vector3 targetPosition, InteractionType interactionType)
        {

            AlignHandWithShoulder(targetPosition, mainHand);

            AdaptPosition(Controller.PositionController, mainHand, targetPosition);

            // Rotate player toward item
            TryRotateTowardTargetPosition(Controller.transform, _moveHandTime, targetPosition);

            // Start looking at item
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _moveHandTime));

            // Move hand toward target
            InteractionSequence.Join(mainHand.Hold.HandIkTarget.DOMove(targetPosition, _moveHandTime).OnComplete(() =>  mainHand.Hold.PlayAnimation(interactionType)));

            InteractionSequence.AppendInterval(InteractionTime - _moveHandTime);

            // Stop looking at item
            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveHandTime).OnStart(() =>
            {
                mainHand.Hold.StopAnimation();
                RestorePosition(Controller.PositionController);
            }));

            // Stop reaching for the position of interaction
            InteractionSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime));

            InteractionSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
            });
        }

        private void AlignHandWithShoulder(Vector3 interactionPoint, Hand mainHand)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.Hold.UpperArm.transform.position).normalized;
            mainHand.Hold.HandIkTarget.rotation = Quaternion.LookRotation(fromShoulderToTarget);
            mainHand.Hold.HandIkTarget.rotation *= Quaternion.Euler(90f, 0f,0);
            mainHand.Hold.HandIkTarget.rotation *= Quaternion.Euler(0f, 180f,0);
        }
    }
}
