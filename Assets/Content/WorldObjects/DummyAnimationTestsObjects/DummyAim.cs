using Coimbra;
using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public sealed class DummyAim : NetworkBehaviour
    {
        public event EventHandler<bool> OnAim;

        [FormerlySerializedAs("aimTarget")]
        [SerializeField]
        private Transform _aimTarget;

        [FormerlySerializedAs("hands")]
        [SerializeField]
        private DummyHands _hands;

        [FormerlySerializedAs("intents")]
        [SerializeField]
        private IntentController _intents;

        [FormerlySerializedAs("holdController")]
        [SerializeField]
        private HoldController _holdController;

        [FormerlySerializedAs("bodyAimRig")]
        [SerializeField]
        private Rig _bodyAimRig;

        [FormerlySerializedAs("rotationSpeed")]
        [SerializeField]
        private float _rotationSpeed = 5f;

        private bool _canAim;

        private bool _isAiming;

        [FormerlySerializedAs("movementController")]
        [SerializeField]
        private DummyMovement _movementController;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            UpdateAimAbility(_hands.SelectedHand);

            if (_canAim && Input.GetMouseButton(1))
            {
                UpdateAimTargetPosition();

                if (!_isAiming)
                {
                    RpcAim(_hands.SelectedHand, _hands.SelectedHand.Item.GameObject.GetComponent<DummyGun>());
                    _isAiming = true;
                }

                if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }
            else if (_isAiming && (!_canAim || !Input.GetMouseButton(1)))
            {
                RpcStopAim(_hands.SelectedHand);
            }

            if (Input.GetKey(KeyCode.E) && _hands.SelectedHand.Full
                && _isAiming && _hands.SelectedHand.Item.GameObject.TryGetComponent(out DummyGun gun))
            {
                gun.GetComponent<DummyFire>().Fire();
            }
        }

        [ServerRpc]
        private void RpcAim(DummyHand hand, DummyGun gun)
        {
            ObserverAim(hand, gun);
        }

        [ObserversRpc]
        private void ObserverAim(DummyHand hand, DummyGun gun)
        {
            Aim(hand, gun);
        }

        [ServerRpc]
        private void RpcStopAim(DummyHand hand)
        {
            ObserverStopAim(hand);
        }

        [ObserversRpc]
        private void ObserverStopAim(DummyHand hand)
        {
            StopAiming(hand);
        }

        private void Aim(DummyHand hand, DummyGun gun)
        {
            _bodyAimRig.weight = 0.3f;
            gun.transform.parent = _hands.SelectedHand.ShoulderWeaponPivot;

            // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
            gun.transform.localPosition = -gun.RifleButt.localPosition;
            gun.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, true);
        }

        private void StopAiming(DummyHand hand)
        {
            _isAiming = false;
            _bodyAimRig.weight = 0f;

            if (!hand.Full)
            {
                return;
            }

            hand.Item.GameObject.transform.parent = hand.ItemPositionTargetLocker;
            _holdController.UpdateItemPositionConstraintAndRotation(
                hand, hand.Item, true, 0.25f, false);
            hand.Item.GameObject.transform.localPosition = Vector3.zero;
            hand.Item.GameObject.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, false);
        }

        private void UpdateAimAbility(DummyHand selectedHand)
        {
            _canAim = _intents.Intent == Intent.Harm && selectedHand.Full && selectedHand.Item.GameObject.HasComponent<DummyGun>();
        }

        /// <summary>
        /// Sync the aiming target transform for all observers, target should have a networkTransform component.
        /// </summary>
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
    }
}
