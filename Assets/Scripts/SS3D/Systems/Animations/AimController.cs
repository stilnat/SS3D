using FishNet.Object;
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

        public event EventHandler<bool> OnAim;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private HumanoidLivingController _movementController;

        [SerializeField]
        private Rig _bodyAimRig;

        [SerializeField]
        private HoldController _holdController;

        private IntentController _intentController;

        [field: SerializeField]
        public Transform AimTarget { get; private set; }

        public bool IsAiming { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _intentController = GetComponent<IntentController>();
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
                if (!IsAiming)
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
            if (IsAiming)
            {
                UpdateAimTargetPosition();

                if (GetComponent<PositionController>().Position != PositionType.Sitting)
                {
                    _movementController.RotatePlayerTowardTarget();
                }
            }
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


        private void Aim()
        {
            IsAiming = true;
            _bodyAimRig.weight = 0.3f;
            OnAim?.Invoke(this, true);
        }

        private void StopAiming()
        {
            IsAiming = false;
            _bodyAimRig.weight = 0f;
            OnAim?.Invoke(this, false);
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
