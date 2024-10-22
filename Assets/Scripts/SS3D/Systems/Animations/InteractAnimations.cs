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
    public class InteractAnimations : IProceduralAnimation
    {

        private float _interactionMoveDuration;

        private Coroutine _interactCoroutine;

        private Sequence _interactSequence;

        private ProceduralAnimationController _controller;

        private Hand _mainHand;

        private IInteractiveTool _tool;

        private InteractionType _interactionType;

        private void SetupInteract(Hand mainHand, IInteractiveTool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.ItemPositionConstraint.weight = 0f;
            mainHand.PickupIkConstraint.weight = 1f;
            _controller.LookAtTargetLocker.position = tool.InteractionPoint.position;
        }

        private void AlignToolWithShoulder(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.UpperArm.transform.position).normalized;

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

        private void ReachInteractionPoint(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool)
        {
            _interactSequence = DOTween.Sequence();

            Vector3 endPosition = ComputeToolEndPosition(interactionPoint, mainHand, tool);

            // Rotate player toward item
            if (_controller.PositionController.Position != PositionType.Sitting)
            {
                //StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, interactionPoint, _interactionMoveDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - interactionPoint.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            // Start looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _interactionMoveDuration));

            // Move tool to the interaction position
            _interactSequence.Join(tool.GameObject.transform.DOMove(endPosition, _interactionMoveDuration).OnComplete(() => tool.PlayAnimation(_interactionType)));

            // Stop looking at item
            _interactSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _interactionMoveDuration));

            // Rotate tool back to its hold rotation
            _interactSequence.Append(tool.GameObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, 2 * _interactionMoveDuration).OnStart(() => tool.StopAnimation()));

            _interactSequence.Join(tool.GameObject.transform.DOLocalMove(Vector3.zero, 2 * _interactionMoveDuration));

            _controller.AnimatorController.Crouch(false);
        }

        public event Action<IProceduralAnimation> OnCompletion;
        public void ServerPerform(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay) { }

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _mainHand = mainHand;
            _tool = target.GetComponent<IInteractiveTool>();
            _controller = proceduralAnimationController;
            _interactionMoveDuration = time;

            SetupInteract(mainHand, _tool);
            ReachInteractionPoint(targetPosition, mainHand, _tool);
        }

        public void Cancel()
        {
            
        }
    }
}
