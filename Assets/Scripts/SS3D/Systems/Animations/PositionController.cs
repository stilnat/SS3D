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

        [SerializeField]
        private FeetController _feetController;


        private PositionType _position = PositionType.Standing;

        private PositionType _previousPosition = PositionType.Standing;

        [SyncVar]
        private bool _standingAbility = true;
        
        private const float MinFeetHealthFactorToStand = 0.5f;

        public PositionType Position => _position;


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
        }


        public bool TrySit()
        {
            return ChangePosition(PositionType.Sitting);
        }

        public bool Prone()
        {
            return ChangePosition(PositionType.Proning);
        }

        public bool TryCrouch()
        {
            return ChangePosition(_standingAbility ? PositionType.Crouching : PositionType.Proning);
        }

        [Client]
        public bool TryToStandUp()
        {
            return ChangePosition(_standingAbility ? PositionType.Standing : PositionType.Proning);
        }

        public bool TryToGetToPreviousPosition()
        {
            //todo make checks for change
            return ChangePosition(_previousPosition);
        }

        /// <summary>
        /// Change the position by the position passed in parameter, return true if position has changed.
        /// </summary>
        private bool ChangePosition(PositionType position)
        {
            _previousPosition = _position;
            _position = position;

            if (_position != _previousPosition)
            {
                ChangedPosition?.Invoke(_position);
            }

            return _position != _previousPosition;
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
