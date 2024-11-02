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

        public Action<PositionType, MovementType> ChangedPositionMovement;

        [SerializeField]
        private FeetController _feetController;


        private PositionType _position = PositionType.Standing;

        private MovementType _movement = MovementType.Normal;

        private MovementType _previousMovement = MovementType.Normal;

        private PositionType _previousPosition = PositionType.Standing;

        

        [SyncVar]
        private bool _standingAbility = true;
        
        private const float MinFeetHealthFactorToStand = 0.5f;

        public PositionType Position => _position;

        public MovementType Movement => _movement;


        public override void OnStartServer()
        {
            base.OnStartServer();
            _feetController.FeetHealthChanged += OnFeetHealthChanged;
            _standingAbility = true;
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            Subsystems.Get<InputSystem>().Inputs.Movement.ChangePosition.performed += HandleChangePosition;
            GetComponent<AimController>().OnAim += HandleAimChange;
        }


        public bool TrySit()
        {
            return ChangeMovementTypeAndPosition(PositionType.Sitting, _movement);
        }

        public bool Prone()
        {
            return ChangeMovementTypeAndPosition(PositionType.Proning, _movement);
        }

        public bool TryCrouch()
        {
            return ChangeMovementTypeAndPosition(_standingAbility ? PositionType.Crouching : PositionType.Proning, _movement);
        }

        [Client]
        public bool TryToStandUp()
        {
            return ChangeMovementTypeAndPosition(_standingAbility ? PositionType.Standing : PositionType.Proning, _movement);
        }

        public bool TryToGetToPreviousPosition()
        {
            //todo make checks for change
            return ChangeMovementTypeAndPosition(_previousPosition, _movement);
        }

        /// <summary>
        /// Change the position by the position passed in parameter, return true if position has changed.
        /// </summary>
        /*private bool ChangePosition(PositionType position)
        {
            _previousPosition = _position;
            _position = position;

            if (_position != _previousPosition)
            {
                ChangedPosition?.Invoke(_position);
            }

            return _position != _previousPosition;
        } */

        private bool ChangeMovementTypeAndPosition(PositionType position, MovementType movement)
        {
            _previousPosition = _position;
            _previousMovement = _movement;
            _position = position;
            _movement = movement;

            bool changeOccured = _position != _previousPosition || _previousMovement != _movement;

            if (changeOccured)
            {
                ChangedPositionMovement?.Invoke(_position, _movement);
            }

            return changeOccured;
        }

        /// <summary>
        /// Cycle through standing, crouching and crawling position
        /// </summary>
        [Client]
        private void HandleChangePosition(InputAction.CallbackContext obj)
        {
            _previousPosition = _position;

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
        public bool ChangeGrab(bool grab)
        {
            return ChangeMovementTypeAndPosition(_position ,grab ? MovementType.Dragging : MovementType.Normal);
        }

        [Client]
        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            ChangeMovementTypeAndPosition(_position ,isAiming ? MovementType.Aiming: MovementType.Normal);
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
