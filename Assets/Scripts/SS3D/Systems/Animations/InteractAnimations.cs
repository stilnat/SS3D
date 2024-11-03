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

        private float _moveToolTime;

        private IInteractiveTool _tool;

        private Hand _mainHand;

        private InteractionType _interact;

        public InteractAnimations(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, NetworkBehaviour target, InteractionType interactionType)
            : base(time, proceduralAnimationController) 
        {
            _tool = target.GetComponent<IInteractiveTool>();
            _moveToolTime = Mathf.Min(time, 0.5f);
            _mainHand = mainHand;
            _interact = interactionType;
            SetupInteract(mainHand, _tool);
        }

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            ReachInteractionPoint(targetPosition, mainHand, _tool, interactionType);
        }

        public override void Cancel()
        {
            InteractionSequence.Kill();

            // Stop looking at item
            DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveToolTime);

            Controller.PositionController.TryToStandUp();
            _tool.StopAnimation();
            _tool.GameObject.transform.DOLocalMove(Vector3.zero, _moveToolTime);
            _tool.GameObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _moveToolTime);
            _mainHand.Hold.ItemPositionConstraint.weight = 1f;
            _mainHand.Hold.PickupIkConstraint.weight = 0f;
        }

        private void SetupInteract(Hand mainHand, IInteractiveTool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
            mainHand.Hold.PickupIkConstraint.weight = 1f;
            Controller.LookAtTargetLocker.position = tool.InteractionPoint.position;
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
            Vector3 endPosition = ComputeToolEndPosition(interactionPoint, mainHand, tool);

            // Rotate player toward item
            TryRotateTowardTargetPosition(Controller.transform, _moveToolTime, interactionPoint);

            AdaptPosition(Controller.PositionController, mainHand, interactionPoint);
           
            // Start looking at item
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _moveToolTime));

            // Move tool to the interaction position
            InteractionSequence.Join(tool.GameObject.transform.DOMove(endPosition, _moveToolTime).OnComplete(() => tool.PlayAnimation(interactionType)));

            InteractionSequence.AppendInterval(InteractionTime - _moveToolTime);

            // Stop looking at item
            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveToolTime));

            // Rotate tool back to its hold rotation
            InteractionSequence.Join(tool.GameObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, _moveToolTime).OnStart(() =>
            {
                RestorePosition(Controller.PositionController);
                tool.StopAnimation();
            }));

            InteractionSequence.Join(tool.GameObject.transform.DOLocalMove(Vector3.zero, _moveToolTime));

            InteractionSequence.OnComplete(() =>
            {
                _mainHand.Hold.PickupIkConstraint.weight = 0f;
                _mainHand.Hold.ItemPositionConstraint.weight = 1f;
                OnCompletion?.Invoke(this);
            });
        }

    }
}
