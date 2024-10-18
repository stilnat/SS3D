using FishNet.Object;
using SS3D.Systems.Crafting;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class InteractWithHandAnimation : MonoBehaviour
    { 
        [SerializeField]
        private Transform _lookAtTargetLocker;

        [SerializeField]
        private MultiAimConstraint _lookAtConstraint;

        [SerializeField]
        private float _interactionMoveDuration;

        [Server]
        public void ServerInteract(Hand hand, Vector3 interactionPoint, float delay)
        {
            StartCoroutine(Interact(hand, interactionPoint, 0.3f));
        }

        private IEnumerator Interact(Hand hand, Vector3 interactionPoint, float delay)
        {
            SetupInteract(hand, interactionPoint);

            _interactionMoveDuration = delay;

            yield return ReachInteractionPoint(interactionPoint, hand);

            hand.PlayAnimation();

            StopInteracting(hand);

            hand.StopAnimation();
        }

        private void SetupInteract(Hand mainHand, Vector3 interactionPoint)
        {
            // disable position constraint the time of the interaction
            mainHand.ItemPositionConstraint.weight = 0f;
            mainHand.PickupIkConstraint.weight = 1f;
            _lookAtTargetLocker.position = interactionPoint;
        }

        private void AlignHandWithShoulder(Vector3 interactionPoint, Hand mainHand)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.UpperArm.transform.position).normalized;
            mainHand.PickupTargetLocker.rotation = Quaternion.LookRotation(fromShoulderToTarget);
            mainHand.PickupTargetLocker.rotation *= Quaternion.Euler(90f, 0f,0);
            mainHand.PickupTargetLocker.rotation *= Quaternion.Euler(0f, 180f,0);
        }

        private IEnumerator ReachInteractionPoint(Vector3 interactionPoint, Hand mainHand)
        {
            // Start looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 0f, 1f, _interactionMoveDuration));

            AlignHandWithShoulder(interactionPoint, mainHand);

            // Rotate player toward item
            if (GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, interactionPoint, _interactionMoveDuration, false, true));
            }

            if (mainHand.HandBone.transform.position.y - interactionPoint.y > 0.3)
            {
                GetComponent<HumanoidAnimatorController>().Crouch(true);
            }

            yield return CoroutineHelper.ModifyVector3OverTime(x => mainHand.PickupTargetLocker.position = x, mainHand.HandBone.position, interactionPoint, _interactionMoveDuration);
        }

        private void StopInteracting(Hand mainHand)
        {
            // Stop looking at item
            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => _lookAtConstraint.weight = x, 1f, 0f, _interactionMoveDuration));

            StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => mainHand.PickupIkConstraint.weight = x, 1f, 0f, _interactionMoveDuration));

            GetComponent<HumanoidAnimatorController>().Crouch(false);

            mainHand.ItemPositionConstraint.weight = 1f;
            
        }
    }
}
