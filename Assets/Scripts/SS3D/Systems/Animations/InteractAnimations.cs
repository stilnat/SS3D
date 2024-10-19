using FishNet.Object;
using SS3D.Systems.Crafting;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class InteractAnimations : NetworkBehaviour
    {
        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        [SerializeField]
        private Transform _hips;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private float _interactionMoveDuration;

        private Coroutine _interactCoroutine;

        [Server]
        public void ServerInteract(Vector3 interactionPoint, IInteractiveTool tool, float delay, InteractionType interactionType)
        {
            _interactCoroutine = StartCoroutine(Interact(interactionPoint, _hands.SelectedHand, tool, delay, interactionType));
        }

        private IEnumerator Interact(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool, float delay, InteractionType interactionType)
        {
            SetupInteract(mainHand, tool);

            yield return ReachInteractionPoint(interactionPoint, mainHand, tool);

            tool.PlayAnimation(interactionType);

            yield return new WaitForSeconds(delay);

            yield return StopInteracting(mainHand, tool);

        }

        private void SetupInteract(Hand mainHand, IInteractiveTool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.ItemPositionConstraint.weight = 0f;
            mainHand.PickupIkConstraint.weight = 1f;
            _lookAtTargetLocker.position = tool.InteractionPoint.position;
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

        private IEnumerator ReachInteractionPoint(Vector3 interactionPoint, Hand mainHand, IInteractiveTool tool)
        {
            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _interactionMoveDuration));

            Vector3 startPosition = tool.GameObject.transform.position;
            Vector3 endPosition = ComputeToolEndPosition(interactionPoint, mainHand, tool);

            // Rotate player toward item
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, interactionPoint, _interactionMoveDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - interactionPoint.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.GameObject.transform.position = x, startPosition, endPosition, _interactionMoveDuration);
        }

        private IEnumerator StopInteracting(Hand mainHand, IInteractiveTool tool)
        {
            tool.StopAnimation();

            // Stop looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _interactionMoveDuration));

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(x => tool.GameObject.transform.localRotation = x, tool.GameObject.transform.localRotation, Quaternion.identity, 2 * _interactionMoveDuration));

            GetComponent<HumanoidAnimatorController>().Crouch(false);

            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.GameObject.transform.localPosition = x, tool.GameObject.transform.localPosition, Vector3.zero, 2 * _interactionMoveDuration);

            tool.GameObject.transform.localRotation = Quaternion.identity;
            mainHand.ItemPositionConstraint.weight = 1f;
            mainHand.PickupIkConstraint.weight = 0f;
        }

        [Server]
        public void Cancel(Hand hand, IInteractiveTool tool)
        {
            ObserverCancel(hand, tool.NetworkBehaviour);
        }

        [ObserversRpc]
        private void ObserverCancel(Hand hand, NetworkBehaviour tool)
        {
            StopCoroutine(_interactCoroutine);

            StartCoroutine(StopInteracting(hand, tool as IInteractiveTool));
        }
    }
}
