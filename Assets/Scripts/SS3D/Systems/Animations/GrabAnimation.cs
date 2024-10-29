using DG.Tweening;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    // TODO : the ownership part should be handled into the interaction system, and the interaction should be a true continuous interaction, only cancelled by client asking or by constraint 
    public class GrabAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private FixedJoint _fixedJoint;

        private LayerMask _grabbableLayer;

        private float _jointBreakForce = 25000f;

        private float _itemReachDuration;

        private float _itemMoveDuration;

        private GrabbableBodyPart _grabbedObject;

        private NetworkConnection _previousOwner;

        private ProceduralAnimationController _controller;

        private Sequence _grabSequence;

        private void RpcReleaseGrab()
        {
            // TODO move into interaction cancel
            _grabbedObject.NetworkObject.GiveOwnership(_previousOwner);
        }

        private void Grab(GrabbableBodyPart bodyPart, NetworkConnection grabbingPlayer, float timeToMoveBackHand, float timeToReachGrabPlace)
        {
            // TODO move into interaction start
            _previousOwner = bodyPart.Owner;
            bodyPart.NetworkObject.GiveOwnership(grabbingPlayer);
        }

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _grabbedObject = target.GetComponent<GrabbableBodyPart>();
            SetUpGrab(_grabbedObject, mainHand, secondaryHand, false);
            GrabReach(_grabbedObject, mainHand);
        }

        public override void Cancel()
        {
        }

        private void ReleaseGrab()
        {
            if (_grabbedObject is not null)
            {
                    if (_grabbedObject.TryGetComponent(out Rigidbody grabbedRb))
                    {
                        grabbedRb.detectCollisions = true; // Enable collisions again
                    }

                    if (_fixedJoint is not null)
                    { 
                       MonoBehaviour.Destroy(_fixedJoint);
                    }

                    _grabbedObject = null;
            }
        }

        private void SetUpGrab(GrabbableBodyPart item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            mainHand.Hold.SetParentTransformTargetLocker(TargetLockerType.Pickup, item.transform);

            // Needed if this has been changed elsewhere
            mainHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                secondaryHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;
            }

            // Set up the look at target locker on the item to pick up.
            _controller.LookAtTargetLocker.parent = item.transform;
            _controller.LookAtTargetLocker.localPosition = Vector3.zero;
            _controller.LookAtTargetLocker.localRotation = Quaternion.identity;

            OrientTargetForHandRotation(mainHand);
        }

        private void GrabReach(GrabbableBodyPart item, Hand mainHand)
        {
            _grabSequence = DOTween.Sequence();

            TryRotateTowardTargetPosition(_grabSequence, _controller.transform, _controller, _itemReachDuration, item.transform.position);

            if (mainHand.HandBone.transform.position.y - item.transform.position.y > 0.3)
            {
                _controller.AnimatorController.Crouch(true);
            }

            // Start looking at grabbed part
            _grabSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change  pickup constraint weight of the main hand from 0 to 1
            _grabSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x =>  mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));

            // those two lines necessary to smooth pulling back
            mainHand.Hold.SetParentTransformTargetLocker(TargetLockerType.Pickup, null, false, false);
            mainHand.Hold.PickupTargetLocker.transform.position = item.transform.position;

            _controller.AnimatorController.Crouch(false);

            // Since transforms are client autoritative, only the client owner should deal with the physics, in this case creating a fixed joint between grabbed part and client hand. 
            if (mainHand.IsOwner)
            {
                item.transform.position = mainHand.Hold.HoldTransform.position;
                _fixedJoint = mainHand.HandBone.gameObject.AddComponent<FixedJoint>();
                Rigidbody grabbedRb = item.GetComponent<Rigidbody>();
                _fixedJoint.connectedBody = grabbedRb;
                grabbedRb.velocity = Vector3.zero;
                _fixedJoint.breakForce = _jointBreakForce;
                grabbedRb.detectCollisions = false; // Disable collisions between the two characters
            }

            // Stop looking
            _grabSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemReachDuration));

            // Stop picking
            _grabSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));;

            Debug.Log("Grabbed object is " + item.name);
        }
    }
}
