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
    /// <summary>
    /// Procedural animation for interacting with tools, such as using a wrench or a screwdriver.
    /// </summary>
    public class InteractAnimations : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private float _interactionTime;

        private float _moveToolTime;

        private Sequence _interactSequence;

        private ProceduralAnimationController _controller;

        private IInteractiveTool _tool;

        private InteractionType _interact;

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _tool = target.GetComponent<IInteractiveTool>();
            _controller = proceduralAnimationController;
            _interactionTime = time;
            _moveToolTime = Mathf.Min(time, 0.5f);

            SetupInteract(mainHand, _tool);
            ReachInteractionPoint(targetPosition, mainHand, _tool, interactionType);
        }

        public override void Cancel()
        {
            _interactSequence.Kill();

            // Stop looking at item
            DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _moveToolTime);

            _controller.AnimatorController.Crouch(false);
            _tool.StopAnimation();
            _tool.GameObject.transform.DOLocalMove(Vector3.zero, _moveToolTime);
            _tool.GameObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _moveToolTime);
        }

        private void SetupInteract(Hand mainHand, IInteractiveTool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
            mainHand.Hold.PickupIkConstraint.weight = 1f;
            _controller.LookAtTargetLocker.position = tool.InteractionPoint.position;
        }

        private void AlignToolWithShoulder(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.Hold.UpperArm.transform.position).normalized;

            // rotate the tool such that its interaction transform Z axis align with the fromShoulderToTarget vector.
            Quaternion rotation = Quaternion.FromToRotation(tool.InteractionPoint.TransformDirection(Vector3.forward), fromShoulderToTarget.normalized);

            // Apply the rotation on the tool
            tool.GameObject.transform.rotation = rotation * tool.GameObject.transform.rotation;
        }

        private Vector3 ComputeToolEndPosition(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool)
        {
            // turn the player toward its target so all subsequent computations
            // are correctly done with player oriented toward target. Then, in the same frame,
            // put player at its initial rotation.
            Transform transform = mainHand.HandsController.transform;
            Vector3 directionFromTransformToTarget = interactionPoint - transform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion initialPlayerRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(directionFromTransformToTarget);

            AlignToolWithShoulder(interactionPoint, mainHand, tool);

            // Calculate the difference between the tool position and its interaction point.
            // Warning : do it only after applying the tool rotation.
            Vector3 difference = tool.InteractionPoint.position - tool.GameObject.transform.position;

            // Compute the desired position for the tool
            Vector3 endPosition = interactionPoint - difference;

            // take back initial rotation after insuring all computations above are done
            // with the right orientation.
            transform.rotation = initialPlayerRotation;

            return endPosition;
        }

        private void ReachInteractionPoint(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool, InteractionType interactionType)
        {
            _interactSequence = DOTween.Sequence();

            Vector3 endPosition = ComputeToolEndPosition(interactionPoint, mainHand, tool);

            // Rotate player toward item
            TryRotateTowardTargetPosition(_interactSequence, _controller.transform, _controller, _moveToolTime, interactionPoint);

            if (mainHand.HandBone.transform.position.y - interactionPoint.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }
           
            // Start looking at item
            _interactSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _moveToolTime));

            // Move tool to the interaction position
            _interactSequence.Join(tool.GameObject.transform.DOMove(endPosition, _moveToolTime).OnComplete(() => tool.PlayAnimation(interactionType)));

            _interactSequence.AppendInterval(_interactionTime - _moveToolTime);

            // Stop looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _moveToolTime));

            // Rotate tool back to its hold rotation
            _interactSequence.Join(tool.GameObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _moveToolTime).OnStart(() =>
            {
                _controller.AnimatorController.Crouch(false);
                tool.StopAnimation();
            }));

            _interactSequence.Join(tool.GameObject.transform.DOLocalMove(Vector3.zero, _moveToolTime));

            _interactSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
            });
        }

    }
}
