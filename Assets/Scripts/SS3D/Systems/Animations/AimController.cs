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
    /// <summary>
    /// Controller handling the logic to decide when a player should be aiming or not, also sync the aiming between clients.
    /// </summary>
    public class AimController : NetworkActor
    {

        public event Action<bool, bool> OnAim;

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

        /// <summary>
        /// Is the player aiming to throw ?
        /// </summary>
        public bool IsAimingToThrow => _isAimingToThrow;

        /// <summary>
        /// Is the player aiming to shoot with a gun ?
        /// </summary>
        public bool IsAimingToShoot => _isAimingToShoot;

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

        public override void OnStartServer()
        {
            base.OnStartServer();
            _hands.OnHandContentChanged += HandleHandContentChanged;
        }

        [Server]
        private void HandleHandContentChanged(Hand hand, Item olditem, Item newitem, ContainerChangeType type)
        {
            // Stop aiming if the item in hand is removed.
            if (_isAimingToShoot && hand == _hands.SelectedHand && type == ContainerChangeType.Remove)
            {
                _isAimingToShoot = false;
            }

            if (_isAimingToThrow && hand == _hands.SelectedHand && type == ContainerChangeType.Remove)
            {
                _isAimingToThrow = false;
            }
        }

        [ServerOrClient]
        private void SyncAimingToThrow(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(isAiming, true);
        }

        [ServerOrClient]
        private void SyncAimingToShoot(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(isAiming, false);
        }

        [Client]
        private void AimThrowOnPerformed(InputAction.CallbackContext obj)
        {
            if (!_hands.SelectedHand.Full)
            {
                return;
            }

            RpcAimToThrow(!IsAimingToThrow);
        }

        [Client]
        private void AimGunOnPerformed(InputAction.CallbackContext obj)
        {
            // To aim, the intent must be harmful, and a gun must be in hand.
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
    }
}
