using DG.Tweening;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class GrabAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private FixedJoint _fixedJoint;

        private LayerMask _grabbableLayer;

        private float _jointBreakForce = 25000f;

        private float _itemReachDuration;

        private Draggable _grabbedObject;

        private ProceduralAnimationController _controller;

        private Sequence _grabSequence;

        private Hand _mainHand;

        public override void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;
            _grabbedObject = target.GetComponent<Draggable>();
            _mainHand = mainHand;
            _itemReachDuration = time / 2;

            SetUpGrab(_grabbedObject, mainHand, secondaryHand, false);
            GrabReach(_grabbedObject, mainHand);
        }

        public override void Cancel()
        {
            ReleaseGrab();
        }

        private void ReleaseGrab()
        {
            if (_grabbedObject is not null && _grabbedObject.TryGetComponent(out Rigidbody grabbedRb))
            {
                grabbedRb.detectCollisions = true;
            }

            if (_fixedJoint is not null)
            { 
                MonoBehaviour.Destroy(_fixedJoint);
            }

            Sequence stopSequence = DOTween.Sequence();

            // Stop looking
            stopSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemReachDuration));

            // Stop picking
            stopSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));

            _controller.PositionController.ChangeGrab(false);
        }

        private void SetUpGrab(Draggable item, Hand mainHand, Hand secondaryHand, bool withTwoHands)
        {
            mainHand.Hold.SetParentTransformTargetLocker(TargetLockerType.Pickup, item.transform);

            // Needed if this has been changed elsewhere
            mainHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                secondaryHand.Hold.SetParentTransformTargetLocker(TargetLockerType.Pickup, item.transform);
                secondaryHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;
            }

            // Set up the look at target locker on the item to pick up.
            _controller.LookAtTargetLocker.parent = item.transform;
            _controller.LookAtTargetLocker.localPosition = Vector3.zero;
            _controller.LookAtTargetLocker.localRotation = Quaternion.identity;

            OrientTargetForHandRotation(mainHand);
        }

        private void GrabReach(Draggable item, Hand mainHand)
        {
            _grabSequence = DOTween.Sequence();

            _grabSequence = TryRotateTowardTargetPosition(_grabSequence, _controller.transform, _controller, _itemReachDuration, item.transform.position);

            _controller.PositionController.ChangeGrab(true);

            // Start looking at grabbed part
            _grabSequence.Join(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change pickup constraint weight of the main hand from 0 to 1
            _grabSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x =>  mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration).OnComplete(() =>
            {
                HandleGrabbing(item, mainHand);
            }));

            // Stop looking
            _grabSequence.Append(DOTween.To(() => _controller.LookAtConstraint.weight, x => _controller.LookAtConstraint.weight = x, 0f, _itemReachDuration));

            // Stop picking
            _grabSequence.Join(DOTween.To(() => mainHand.Hold.PickupIkConstraint.weight, x => mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));
        }

        private void HandleGrabbing(Draggable draggable, Hand mainHand)
        {
            mainHand.HandBone.GetComponent<Rigidbody>().isKinematic = true;
            mainHand.HandBone.GetComponent<Collider>().enabled = false;
                
            // Only the owner handle physics since transform are client authoritative for now
            if (!mainHand.IsOwner)
            {
                return;
            }

            Rigidbody grabbedRb = draggable.GetComponent<Rigidbody>();

            if (draggable.MoveToGrabber)
            {
                draggable.transform.position = mainHand.Hold.HoldTransform.position; 
                grabbedRb.velocity = Vector3.zero;
                grabbedRb.position = mainHand.Hold.HoldTransform.position;
            }

            grabbedRb.detectCollisions = false;

            _fixedJoint = mainHand.HandBone.gameObject.AddComponent<FixedJoint>();
            _fixedJoint.connectedBody = grabbedRb;
            _fixedJoint.breakForce = _jointBreakForce;
        }
    }
}
