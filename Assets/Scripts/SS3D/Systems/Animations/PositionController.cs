using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Animations
{
    public class PositionController : NetworkActor
    {

        public Action<PositionType> ChangedPosition;

        public Action<MovementType> ChangedMovement;

        public Action<bool> Dance;

        [SerializeField]
        private FeetController _feetController;


        [SyncVar(OnChange = nameof(SyncPosition))]
        private PositionType _position;

        [SyncVar(OnChange = nameof(SyncMovement))]
        private MovementType _movement;

        [SyncVar]
        private MovementType _previousMovement;

        [SyncVar]
        private PositionType _previousPosition;

        [SyncVar(OnChange = nameof(SyncDance))]
        private bool _isDancing;

        [SyncVar]
        private bool _standingAbility = true;
        
        private const float MinFeetHealthFactorToStand = 0.5f;

        public PositionType Position => _position;

        public MovementType Movement => _movement;

        public bool CanGrab => _standingAbility && _movement == MovementType.Normal && _position != PositionType.Sitting;

        public bool CanSit => _standingAbility;


        public override void OnStartServer()
        {
            base.OnStartServer();
            _feetController.FeetHealthChanged += OnFeetHealthChanged;
            _standingAbility = true;
            _position = PositionType.Standing;
            _movement = MovementType.Normal;
            _previousPosition = PositionType.Standing;
            _previousMovement = MovementType.Normal;
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            if(!IsOwner)
            {
                return;
            }

            Subsystems.Get<InputSystem>().Inputs.Movement.ChangePosition.performed += HandleChangePosition;
            Subsystems.Get<InputSystem>().Inputs.Movement.Dance.performed += HandleDance;
            GetComponent<AimController>().OnAim += HandleAimChange;
        }

        private void HandleDance(InputAction.CallbackContext obj)
        {
            RpcDance();
        }

        [ServerRpc]
        private void RpcDance()
        {
            if (_standingAbility && _position == PositionType.Standing)
            {
                _isDancing = !_isDancing;
            }
        }

        private void SyncPosition(PositionType oldPosition, PositionType newPosition, bool asServer)
        {
            ChangedPosition?.Invoke(newPosition);
        }

        private void SyncMovement(MovementType oldPosition, MovementType newPosition, bool asServer)
        {
            ChangedMovement?.Invoke(newPosition);
        }

        private void SyncDance(bool wasDancing, bool isDancing, bool asServer)
        {
            Dance?.Invoke(isDancing);
        }

        [Client]
        public void TrySit()
        {
            RpcChangePosition(PositionType.Sitting);
        }

        [Client]
        public void Prone()
        {
            RpcChangePosition(PositionType.Proning);
        }

        [Client]
        public void TryCrouch()
        { 
            RpcChangePosition(_standingAbility ? PositionType.Crouching : PositionType.Proning);
        }

        [Client]
        public void TryToStandUp()
        {
            RpcChangePosition(_standingAbility ? PositionType.Standing : PositionType.Proning);
        }

        [Client]
        public void TryToGetToPreviousPosition()
        {
            //todo make checks for change
             RpcChangePosition(_previousPosition);
        }

        [ServerRpc]
        private void RpcChangeMovement(MovementType movement)
        {
            _previousMovement = _movement;
            _movement = movement;
        }

        [ServerRpc]
        private void RpcChangePosition(PositionType position)
        {
            _previousPosition = _position;
            _position = position;
        }

        /// <summary>
        /// Cycle through standing, crouching and crawling position
        /// </summary>
        [Client]
        private void HandleChangePosition(InputAction.CallbackContext obj)
        {

            switch (_position)
            {
                case PositionType.Proning :
                    TryToStandUp();
                    break;
                case PositionType.Sitting :
                    TryToStandUp();
                    break;
                case PositionType.Standing :
                    TryCrouch();
                    break;
                case PositionType.Crouching :
                    Prone();
                    break;
            }
        }

        [Client]
        public void ChangeGrab(bool grab)
        {
            RpcChangeMovement(grab ? MovementType.Dragging : MovementType.Normal);
        }

        [Client]
        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            RpcChangeMovement(isAiming ? MovementType.Aiming: MovementType.Normal);
        }

        [Server]
        private void OnFeetHealthChanged(float feetHealth)
        {
            _standingAbility = feetHealth >= MinFeetHealthFactorToStand;

            if (_standingAbility == false)
            {
                GetComponent<Ragdoll>().Knockdown(5f);
            }
        }
    }
}
