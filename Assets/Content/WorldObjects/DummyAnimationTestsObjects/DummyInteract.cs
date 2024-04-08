using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace DummyStuff
{
    public class DummyInteract : MonoBehaviour
    {

        public DummyHands hands;
        public Transform lookAtTargetLocker;
        public Transform hips;

        public MultiAimConstraint lookAtConstraint;

        public float interactionMoveDuration;

        public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.1f;


        // Update is called once per frame
        private void Update()
        {
            if (!Input.GetMouseButtonDown(1))
                return;

            TryInteract();
        }

        private void TryInteract()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit) && hands.SelectedHand.Full && UnderMaxDistanceFromHips(hit.point) 
                && hands.SelectedHand.Item.GameObject.TryGetComponent(out DummyTool tool))
            {

                GameObject obj = hit.collider.gameObject;
                StartCoroutine(Interact(obj.transform, hands.SelectedHand, tool));
            }
        }

        private IEnumerator Interact(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
        {
            SetupInteract(interactionTarget, mainHand, tool);

            yield return ReachInteractionPoint(interactionTarget, mainHand, tool);

            yield return new WaitForSeconds(0.6f);

            yield return StopInteracting(interactionTarget, mainHand, tool);
        }

        private void SetupInteract(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
        {
            // disable position constraint the time of the interaction
            mainHand.itemPositionConstraint.weight = 0f;
            mainHand.pickupIkConstraint.weight = 1f;
            lookAtTargetLocker.position = tool.transform.position;
        }

        private void AlignToolWithShoulder(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
        {
            Vector3 fromShoulderToTarget = (interactionTarget.transform.position - mainHand.upperArm.transform.position).normalized;

            // rotate the tool such that its interaction transform Z axis align with the fromShoulderToTarget vector.
            Quaternion rotation = Quaternion.FromToRotation(tool.InteractionPoint.TransformDirection(Vector3.forward), fromShoulderToTarget.normalized);

            // Apply the rotation on the tool
            tool.transform.rotation = rotation * tool.transform.rotation;
        }

        private Vector3 ComputeToolEndPosition(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
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

        private IEnumerator ReachInteractionPoint(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
        {
            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight = x,
                0f, 1f, interactionMoveDuration));
            
            Vector3 startPosition = tool.transform.position;
            Vector3 endPosition = ComputeToolEndPosition(interactionTarget, mainHand, tool);

            // Rotate player toward item
            if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, interactionTarget,
                    interactionMoveDuration, false, true));
            }
            
            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.transform.position = x,
                startPosition, endPosition, interactionMoveDuration);
        }

        private IEnumerator StopInteracting(Transform interactionTarget, DummyHand mainHand, DummyTool tool)
        {
            // Stop looking at item         
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight = x,
                1f, 0f, interactionMoveDuration));

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(x =>
                    tool.transform.localRotation = x, tool.transform.localRotation,
                Quaternion.identity, 2 * interactionMoveDuration));


            yield return CoroutineHelper.ModifyVector3OverTime(x => tool.transform.localPosition = x,
                tool.transform.localPosition, Vector3.zero, 2 * interactionMoveDuration);

            tool.transform.localRotation = Quaternion.identity;
            mainHand.itemPositionConstraint.weight = 1f;
            mainHand.pickupIkConstraint.weight = 0f;
        }
    }
}
