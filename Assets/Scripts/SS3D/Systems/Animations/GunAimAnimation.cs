using Coimbra;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace SS3D.Systems.Animations
{
    public class GunAimAnimation : NetworkBehaviour
    {
        public event EventHandler<bool> OnAim;

        [SerializeField]
        private Transform _aimTarget;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private HoldController _holdController;

        [SerializeField]
        private Rig _bodyAimRig;

        [SerializeField]
        private float _rotationSpeed = 5f;

        private bool _canAim;

        private bool _isAiming;

        [SerializeField]
        private HumanoidLivingController _movementController;

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
                    RpcAim(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.GameObject.GetComponent<Gun>());
                    _isAiming = true;
                }

                if (GetComponent<PositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }
            else if (_isAiming && (!_canAim || !Input.GetMouseButton(1)))
            {
                RpcStopAim(_hands.SelectedHand);
            }

            if (Input.GetKey(KeyCode.E) && _hands.SelectedHand.Full
                && _isAiming && _hands.SelectedHand.ItemInHand.GameObject.TryGetComponent(out Gun gun))
            {
                gun.Fire();
            }
        }

        [ServerRpc]
        private void RpcAim(Hand hand, Gun gun)
        {
            ObserverAim(hand, gun);
        }

        [ObserversRpc]
        private void ObserverAim(Hand hand, Gun gun)
        {
            Aim(hand, gun);
        }

        [ServerRpc]
        private void RpcStopAim(Hand hand)
        {
            ObserverStopAim(hand);
        }

        [ObserversRpc]
        private void ObserverStopAim(Hand hand)
        {
            StopAiming(hand);
        }

        private void Aim(Hand hand, Gun gun)
        {
            _bodyAimRig.weight = 0.3f;
            gun.transform.parent = _hands.SelectedHand.ShoulderWeaponPivot;

            // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
            gun.transform.localPosition = -gun.RifleButt.localPosition;
            gun.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, true);
        }

        private void StopAiming(Hand hand)
        {
            _isAiming = false;
            _bodyAimRig.weight = 0f;

            if (!hand.Full)
            {
                return;
            }

            hand.ItemInHand.GameObject.transform.parent = hand.ItemPositionTargetLocker;
            _holdController.UpdateItemPositionConstraintAndRotation(
                hand, hand.ItemInHand.Holdable, true, 0.25f, false);
            hand.ItemInHand.GameObject.transform.localPosition = Vector3.zero;
            hand.ItemInHand.GameObject.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, false);
        }

        private void UpdateAimAbility(Hand selectedHand)
        {
            _canAim = _intents.Intent == IntentType.Harm && selectedHand.Full && selectedHand.ItemInHand.GameObject.HasComponent<Gun>();
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
