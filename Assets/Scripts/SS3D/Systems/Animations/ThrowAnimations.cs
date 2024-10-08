using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Animations
{
    public class ThrowAnimations : NetworkBehaviour
    { 
        public event EventHandler<bool> OnAim;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private HumanoidAnimatorController _animatorController;

        [SerializeField]
        private HumanoidLivingController _movementController;

        [SerializeField]
        private Transform _aimTarget;

        [SerializeField]
        private float _maxForce = 20;

        [SerializeField]
        private float _secondPerMeterFactorDef = 0.22f;

        [SerializeField]
        private float _secondPerMeterFactorHarm = 0.15f;

        [SerializeField]
        private Rig _bodyAimRig;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private IntentController _intents;

        private bool _isAiming;

        public bool IsAiming => _isAiming;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
            Subsystems.Get<InputSystem>().Inputs.Interactions.AimThrow.performed += AimThrowOnperformed;
        }

        private void AimThrowOnperformed(InputAction.CallbackContext obj)
        {
            if (_hands.SelectedHand.Full)
            {
                if (!_isAiming)
                {
                    RpcAim();
                }
                else
                {
                    RpcStopAim();
                }
            }
        }

        // Update is called once per frame
        protected void Update()
        {
            if (_isAiming)
            {
                UpdateAimTargetPosition();

                if (GetComponent<PositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }
        }

        [Server]
        public void ThrowAnimate()
        {
            Item item = _hands.SelectedHand.ItemInHand;
            item.RemoveOwnership();
            ObserverThrow(item);
            StartCoroutine(ServerThrow(item));
        }

        [Server]
        private IEnumerator ServerThrow(Item item)
        {
            item.GameObject.transform.parent = _hands.SelectedHand.HandBone.transform;
            yield return new WaitForSeconds(0.18f);    
            _hands.SelectedHand.Container.RemoveItem(item);
            item.GameObject.transform.parent = null;
            item.GameObject.GetComponent<Rigidbody>().isKinematic = false;
            item.GameObject.GetComponent<Collider>().enabled = true;
            AddForceToItem(item.GameObject);
        }

        [ObserversRpc]
        private void ObserverThrow(Item item)
        {
            StartCoroutine(Throw(item));
        }

        [ServerRpc]
        private void RpcAim()
        {
            ObserverAim();
        }

        [ObserversRpc]
        private void ObserverAim()
        {
            Aim();
        }

        [ServerRpc]
        private void RpcStopAim()
        {
            ObserverStopAim();
        }

        [ObserversRpc]
        private void ObserverStopAim()
        {
            StopAiming();
        }

        private IEnumerator Throw(Item item)
        {
            IHoldProvider holdable = item.Holdable;
            _hands.SelectedHand.ItemPositionConstraint.weight = 0f;
            _hands.SelectedHand.HoldIkConstraint.weight = 0f;
            _hands.SelectedHand.PickupIkConstraint.weight = 0f;

            if (holdable.CanHoldTwoHand && _hands.TryGetOppositeHand(_hands.SelectedHand, out Hand oppositeHand))
            {
                oppositeHand.ItemPositionConstraint.weight = 0f;
                oppositeHand.HoldIkConstraint.weight = 0f;
                oppositeHand.PickupIkConstraint.weight = 0f;
            }

            item.GameObject.transform.parent = _hands.SelectedHand.HandBone.transform;

            _animatorController.Throw(_hands.SelectedHand.HandType);

            StartCoroutine(TransformHelper.OrientTransformTowardTarget(transform, _aimTarget.transform, 0.18f, false, true));

            yield return new WaitForSeconds(0.18f);

            item.GameObject.transform.parent = null;

            StopAiming();
        }

        private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight, float initialHorizontalPosition)
        {
            // Those computations assume gravity is pulling in the same plane as the throw.
            // it works with any vertical gravity but not if there's a horizontal component to it.
            // be careful as g = -9.81 and not 9.81
            float g = Physics.gravity.y;
            float initialHorizontalVelocity = (targetCoordinates.x - initialHorizontalPosition) / timeToReachTarget;

            float initialVerticalVelocity = 
                (targetCoordinates.y - initialHeight - (0.5f * g * (Mathf.Pow(targetCoordinates.x - initialHorizontalPosition, 2) / Mathf.Pow(initialHorizontalVelocity, 2)))) * initialHorizontalVelocity / (targetCoordinates.x - initialHorizontalPosition);

            return new(initialHorizontalVelocity, initialVerticalVelocity);
        }

        /// <summary>
        /// Compute coordinates in the local coordinate system of the throwing hand
        /// This method assumes that the target position is in the same plane as the plane defined by the
        /// player y and z local axis.
        /// return vector2 with components in order z and y, as z is forward and y upward.
        /// </summary>
        private Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
        {
            Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

            if (rootRelativeTargetPosition.x > 0.1f)
            {
                Debug.LogError("target not in the same plane as the player root : " + rootRelativeTargetPosition.x);
            }

            return new(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
        }

        private Vector2 ComputeItemInitialCoordinates(Vector3 itemPosition, Transform playerRoot)
        {
            Vector3 rootRelativeItemPosition = playerRoot.InverseTransformPoint(itemPosition);

            return new Vector2(rootRelativeItemPosition.z, rootRelativeItemPosition.y);
        }

        private void UpdateAimTargetPosition()
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _aimTarget.position = hit.point;
            }
        }

        private void Aim()
        {
            _isAiming = true;
            _bodyAimRig.weight = 0.3f;

            if (_intents.Intent == IntentType.Harm)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, false, 0.2f, true);
            }

            OnAim?.Invoke(this, true);
        }

        private void StopAiming()
        {
            _isAiming = false;
            _bodyAimRig.weight = 0f;
            Item item = _hands.SelectedHand.ItemInHand;

            if (item != null)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(
                    _hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, false, 0.2f, false);
            }

            OnAim?.Invoke(this, false);
        }

        private void AddForceToItem(GameObject item)
        {
            Vector2 targetCoordinates = ComputeTargetCoordinates(_aimTarget.position, transform);

            Vector2 initialItemCoordinates = ComputeItemInitialCoordinates(item.transform.position, transform);

            Vector2 initialVelocity = ComputeInitialVelocity(
                ComputeTimeToReach(_intents.Intent, _aimTarget.position),
                targetCoordinates,
                initialItemCoordinates.y,
                initialItemCoordinates.x);

            Vector3 initialVelocityInRootCoordinate = new Vector3(0, initialVelocity.y, initialVelocity.x);

            Vector3 initialVelocityInWorldCoordinate = transform.TransformDirection(initialVelocityInRootCoordinate);

            if (initialVelocityInWorldCoordinate.magnitude > _maxForce)
            {
                initialVelocityInWorldCoordinate = initialVelocityInWorldCoordinate.normalized * _maxForce;
            }

            item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
        }

        private float ComputeTimeToReach(IntentType intent, Vector3 targetPosition)
        {
            float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

            float timeToReach = intent == IntentType.Help ?
                distanceToTarget * _secondPerMeterFactorDef : distanceToTarget * _secondPerMeterFactorHarm;

            return timeToReach;
        }
    }
}
