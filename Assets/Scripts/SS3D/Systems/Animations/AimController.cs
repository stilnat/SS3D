using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Animations
{
    public class AimController : NetworkActor
    {

        public event EventHandler<Tuple<bool, bool, AbstractHoldable>> OnAim;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private HumanoidLivingController _movementController;

        [SerializeField]
        private Rig _bodyAimRig;

        private IntentController _intentController;

        [SyncVar(OnChange = nameof(SyncAimingToThrow))]
        private bool _isAimingToThrow;

        [SyncVar(OnChange = nameof(SyncAimingToShoot))]
        private bool _isAimingToShoot;

        public bool IsAimingToThrow => _isAimingToThrow;

        public bool IsAimingToShoot => _isAimingToShoot;

        [field: SerializeField]
        public Transform AimTarget { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _intentController = GetComponent<IntentController>();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }

            Subsystems.Get<InputSystem>().Inputs.Interactions.AimThrow.performed += AimThrowOnPerformed;
            Subsystems.Get<InputSystem>().Inputs.Interactions.AimGun.performed += AimGunOnPerformed;
        }

        protected void Update()
        {
            if (IsAimingToThrow || IsAimingToShoot)
            {
                UpdateAimTargetPosition();

                if (GetComponent<PositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }
        }

        private void SyncAimingToThrow(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(this, new(isAiming, true, _hands.SelectedHand.ItemInHand.Holdable as AbstractHoldable));
        }

        private void SyncAimingToShoot(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(this, new(isAiming, false, _hands.SelectedHand.ItemInHand.Holdable as AbstractHoldable));
        }

        private void AimThrowOnPerformed(InputAction.CallbackContext obj)
        {
            if (!_hands.SelectedHand.Full)
            {
                return;
            }

            RpcAimToThrow(!IsAimingToThrow);
        }

        private void AimGunOnPerformed(InputAction.CallbackContext obj)
        {
            bool canAim = _intentController.Intent == IntentType.Harm && _hands.SelectedHand.Full && _hands.SelectedHand.ItemInHand.GameObject.HasComponent<Gun>();

            if (canAim)
            {
                RpcAimToShoot(!IsAimingToShoot);
            }
        }

        [ServerRpc]
        private void RpcAimToThrow(bool isAimingToThrow)
        {
            _isAimingToThrow = isAimingToThrow;
        }

        [ServerRpc]
        private void RpcAimToShoot(bool isAimingToShoot)
        {
            _isAimingToShoot = isAimingToShoot;
        }

        private void UpdateAimTargetPosition()
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                AimTarget.position = hit.point;
            }
        }
    }
}
