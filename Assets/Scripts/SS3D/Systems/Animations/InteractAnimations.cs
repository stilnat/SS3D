using SS3D.Systems.Crafting;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class InteractAnimations : MonoBehaviour
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

        public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(_hips.position, position) < 1.1f;

        public void TryInteract()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit) && _hands.SelectedHand.Full && UnderMaxDistanceFromHips(hit.point)
                && _hands.SelectedHand.ItemInHand.GameObject.TryGetComponent(out Tool tool))
            {
                GameObject obj = hit.collider.gameObject;
                StartCoroutine(Interact(obj.transform, _hands.SelectedHand, tool));
            }
        }

        private IEnumerator Interact(Transform interactionTarget, Hand mainHand, Tool tool)
        {
            SetupInteract(interactionTarget, mainHand, tool);

            yield return ReachInteractionPoint(interactionTarget, mainHand, tool);

            yield return new WaitForSeconds(0.6f);

            yield return StopInteracting(mainHand, tool);
        }

        private void SetupInteract(Transform interactionTarget, Hand mainHand, Tool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.ItemPositionConstraint.weight = 0f;
            mainHand.PickupIkConstraint.weight = 1f;
            _lookAtTargetLocker.position = tool.transform.position;
        }

        private void AlignToolWithShoulder(Transform interactionTarget, Hand mainHand, Tool tool)
        {
            Vector3 fromShoulderToTarget = (interactionTarget.transform.position - mainHand.UpperArm.transform.position).normalized;

            // rotate the tool such that its interaction transform Z axis align with the fromShoulderToTarget vector.
            Quaternion rotation = Quaternion.FromToRotation(tool.InteractionPoint.TransformDirection(Vector3.forward), fromShoulderToTarget.normalized);

            // Apply the rotation on the tool
            tool.transform.rotation = rotation * tool.transform.rotation;
        }

        private Vector3 ComputeToolEndPosition(Transform interactionTarget, Hand mainHand, Tool tool)
        {
            // turn the player toward its target so all subsequent computations
            // are correctly done with player oriented toward target. Then, in the same frame,
            // put player at its initial rotation.
            Vector3 directionFromTransformToTarget = interactionTarget.position - transform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion initialPlayerRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(directionFromTransformToTarget);

            AlignToolWithShoulder(interactionTarget, mainHand, tool);

            // Calculate the difference between the tool position and its interaction point.
            // Warning : do it only after applying the tool rotation.
            Vector3 difference = tool.InteractionPoint.position - tool.transform.position;

            // Compute the desired position for the tool
            Vector3 endPosition = interactionTarget.position - difference;

            // take back initial rotation after insuring all computations above are done
            // with the right orientation.
            transform.rotation = initialPlayerRotation;

            return endPosition;
        }

        private IEnumerator ReachInteractionPoint(Transform interactionTarget, Hand mainHand, Tool tool)
        {
            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _interactionMoveDuration));

            Vector3 startPosition = tool.transform.position;
            Vector3 endPosition = ComputeToolEndPosition(interactionTarget, mainHand, tool);

            // Rotate player toward item
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, interactionTarget, _interactionMoveDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - interactionTarget.transform.position.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.transform.position = x, startPosition, endPosition, _interactionMoveDuration);
        }

        private IEnumerator StopInteracting(Hand mainHand, Tool tool)
        {
            // Stop looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _interactionMoveDuration));

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(x => tool.transform.localRotation = x, tool.transform.localRotation, Quaternion.identity, 2 * _interactionMoveDuration));

            GetComponent<HumanoidAnimatorController>().Crouch(false);

            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.transform.localPosition = x, tool.transform.localPosition, Vector3.zero, 2 * _interactionMoveDuration);

            tool.transform.localRotation = Quaternion.identity;
            mainHand.ItemPositionConstraint.weight = 1f;
            mainHand.PickupIkConstraint.weight = 0f;
        }
    }
}
