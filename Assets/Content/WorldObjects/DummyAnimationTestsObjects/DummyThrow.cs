using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using Object = System.Object;

namespace DummyStuff
{
    public class DummyThrow : NetworkBehaviour
    {
        public event EventHandler<bool> OnAim;

        [SerializeField]
        private DummyHands _hands;

        [SerializeField]
        private DummyAnimatorController _animatorController;

        [FormerlySerializedAs("movementController")]
        [SerializeField]
        private DummyMovement _movementController;

        [FormerlySerializedAs("aimTarget")]
        [SerializeField]
        private Transform _aimTarget;

        [FormerlySerializedAs("maxForce")]
        [SerializeField]
        private float _maxForce = 20;

        [FormerlySerializedAs("secondPerMeterFactorDef")]
        [SerializeField]
        private float _secondPerMeterFactorDef = 0.22f;

        [FormerlySerializedAs("secondPerMeterFactorHarm")]
        [SerializeField]
        private float _secondPerMeterFactorHarm = 0.15f;

        [SerializeField]
        private Rig _bodyAimRig;

        [SerializeField]
        private HoldController _holdController;

        [FormerlySerializedAs("intents")]
        [SerializeField]
        private IntentController _intents;

        private bool _canAim;

        private bool _isAiming;

        public bool IsAiming => _isAiming;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
        }

        // Update is called once per frame
        protected void Update()
        {
            UpdateAimAbility(_hands.SelectedHand);

            if (_canAim && Input.GetKeyDown(KeyCode.R))
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

            if (_isAiming)
            {
                UpdateAimTargetPosition();

                if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }

            if (Input.GetKeyDown(KeyCode.Y) && _hands.SelectedHand.Full && _isAiming)
            {
                RpcThrow();
            }
        }

        [ServerRpc]
        private void RpcThrow()
        {
            ObserverThrow();
        }

        [ObserversRpc]
        private void ObserverThrow()
        {
            StartCoroutine(Throw());
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

        private IEnumerator Throw()
        {
            IHoldProvider item = _hands.SelectedHand.Item;
            _hands.SelectedHand.ItemPositionConstraint.weight = 0f;
            _hands.SelectedHand.HoldIkConstraint.weight = 0f;
            _hands.SelectedHand.PickupIkConstraint.weight = 0f;

            if (item.CanHoldTwoHand && _hands.UnselectedHand.Empty)
            {
                _hands.UnselectedHand.ItemPositionConstraint.weight = 0f;
                _hands.UnselectedHand.HoldIkConstraint.weight = 0f;
                _hands.UnselectedHand.PickupIkConstraint.weight = 0f;
            }

            item.GameObject.transform.parent = _hands.SelectedHand.HandBone.transform;

            _animatorController.Throw(_hands.SelectedHand.HandType);

            StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, _aimTarget.transform, 0.18f, false, true));

            yield return new WaitForSeconds(0.18f);

            AddForceToItem(item.GameObject);

            StopAiming();
        }

        private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight, float initialHorizontalPosition)
        {
            // Those computations assume gravity is pulling in the same plane as the throw.
            // it works with any vertical gravity but not if there's a horizontal component to it.
            // be careful as g = -9.81 and not 9.81
            float g = Physics.gravity.y;
            float initialHorizontalVelocity = (targetCoordinates.x - initialHorizontalPosition) / timeToReachTarget;

            float initialVerticalVelocity = (targetCoordinates.y - initialHeight - (0.5f * g * (math.pow(targetCoordinates.x - initialHorizontalPosition, 2) / math.pow(initialHorizontalVelocity, 2)))) * initialHorizontalVelocity / (targetCoordinates.x - initialHorizontalPosition);

            return new Vector2(initialHorizontalVelocity, initialVerticalVelocity);
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

            return new Vector2(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
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

            if (_intents.Intent == Intent.Harm)
            {
                _holdController.UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.Item, false, 0.2f, true);
            }

            OnAim?.Invoke(this, true);
        }

        private void StopAiming()
        {
            _isAiming = false;
            _bodyAimRig.weight = 0f;
            _holdController.UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.Item, false, 0.2f, false);
            OnAim?.Invoke(this, false);
        }

        private void UpdateAimAbility(DummyHand selectedHand)
        {
            _canAim = selectedHand.Full;
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

            _hands.SelectedHand.RemoveItem();

            if (initialVelocityInWorldCoordinate.magnitude > _maxForce)
            {
                initialVelocityInWorldCoordinate = initialVelocityInWorldCoordinate.normalized * _maxForce;
            }

            item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
        }

        private float ComputeTimeToReach(Intent intent, Vector3 targetPosition)
        {
            float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

            return intent == Intent.Def ?
                distanceToTarget * _secondPerMeterFactorDef : distanceToTarget * _secondPerMeterFactorHarm;
        }
    }
}
