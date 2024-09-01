using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using InputSystem = SS3D.Systems.Inputs.InputSystem;
using Object = System.Object;

namespace DummyStuff
{
    public class DummyMovement : NetworkActor
    {
        public event Action<float> OnSpeedChangeEvent;

        private const float WalkAnimatorValue = .3f;
        private const float RunAnimatorValue = 1f;

        [FormerlySerializedAs("rb")]
        [SerializeField]
        private Rigidbody _rb;

        [FormerlySerializedAs("aimTarget")]
        [SerializeField]
        private Transform _aimTarget;

        [FormerlySerializedAs("movementType")]
        [SerializeField]
        private MovementType _movementType;

        [FormerlySerializedAs("movementSpeed")]
        [Header("Movement Settings")]
        [SerializeField]
        private float _movementSpeed;
        [FormerlySerializedAs("lerpMultiplier")]
        [SerializeField]
        private float _lerpMultiplier;
        [FormerlySerializedAs("rotationLerpMultiplier")]
        [SerializeField]
        private float _rotationLerpMultiplier;

        [FormerlySerializedAs("movementTarget")]
        [Header("Movement IK Targets")]
        [SerializeField]
        private Transform _movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        private Vector3 _absoluteMovement;
        private Vector2 _input;
        private Vector2 _smoothedInput;

        [FormerlySerializedAs("targetMovement")]
        [SerializeField]
        private Vector3 _targetMovement;

        private Actor _camera;
        private Controls.MovementActions _movementControls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputSystem _inputSystem;

        [FormerlySerializedAs("aimRotationSpeed")]
        [SerializeField]
        private float _aimRotationSpeed = 5f;

        public void RotatePlayerTowardTarget()
        {
            // Get the direction to the target
            Vector3 direction = _aimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _aimRotationSpeed * Time.deltaTime);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
                return;
            }

            Setup();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            _targetMovement = Vector3.zero;
        }

        /// <inheritdoc/>
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            _movementControls.ToggleRun.performed -= HandleToggleRun;
            _inputSystem.ToggleActionMap(_movementControls, false);
            _inputSystem.ToggleActionMap(_hotkeysControls, false);
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets.
        /// </summary>
        protected void ProcessCharacterMovement()
        {
            ProcessPlayerInput();

            if (_input.magnitude != 0)
            {
                MoveMovementTarget(_input);
                if (_movementType != MovementType.Aiming)
                {
                    RotatePlayerToMovement(_movementType == MovementType.Dragging);
                }

                MovePlayer();
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
            }
        }

        /// <summary>
        /// Moves the player to the target movement.
        /// </summary>
        protected void MovePlayer()
        {
            _rb.velocity = _targetMovement * (float)(_movementSpeed * TimeManager.TickDelta);
        }

        /// <summary>
        /// Moves the movement targets with the given input.
        /// </summary>
        /// <param name="movementInput"></param>
        protected void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
        {
            Vector3 newTargetMovement = (movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized)
                + (movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized);

            // smoothly changes the target movement
            _targetMovement = Vector3.Lerp(_targetMovement, newTargetMovement, Time.deltaTime * (_lerpMultiplier * multiplier));

            Vector3 resultingMovement = _targetMovement + Position;
            _absoluteMovement = resultingMovement;
            _movementTarget.position = _absoluteMovement;
        }

        /// <summary>
        /// Rotates the player to the target movement.
        /// </summary>
        protected void RotatePlayerToMovement(bool lookOpposite)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookOpposite ? -_targetMovement : _targetMovement);
            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, Time.deltaTime * _rotationLerpMultiplier);
        }

        /// <summary>
        /// Process the player movement input, smoothing it.
        /// </summary>
        protected void ProcessPlayerInput()
        {
            float x = _movementControls.Movement.ReadValue<Vector2>().x;
            float y = _movementControls.Movement.ReadValue<Vector2>().y;

            float inputFilteredSpeed = FilterSpeed();

            _input = Vector2.ClampMagnitude(new Vector2(x, y), inputFilteredSpeed);
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, Time.deltaTime * (_lerpMultiplier / 10));

            OnSpeedChanged(_input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        protected virtual float FilterSpeed()
        {
            return _isRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Toggles your movement between run/walk.
        /// </summary>
        protected void HandleToggleRun(InputAction.CallbackContext context)
        {
            _isRunning = !_isRunning;
        }

        protected void OnSpeedChanged(float speed)
        {
            OnSpeedChangeEvent?.Invoke(speed);
        }

        private void Setup()
        {
            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _inputSystem = Subsystems.Get<InputSystem>();

            Controls controls = _inputSystem.Inputs;

            _movementControls = controls.Movement;
            _hotkeysControls = controls.Hotkeys;
            _movementControls.ToggleRun.performed += HandleToggleRun;

            _inputSystem.ToggleActionMap(_movementControls, true);
            _inputSystem.ToggleActionMap(_hotkeysControls, true);

            GetComponent<DummyThrow>().OnAim += HandleAimChange;
            GetComponent<DummyAim>().OnAim += HandleAimChange;
            GetComponent<Grab>().OnGrab += HandleGrabChange;

            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
        }

        private void HandleAimChange(object sender, bool aim)
        {
            _movementType = aim ? MovementType.Aiming : MovementType.Normal;
        }

        private void HandleGrabChange(object sender, bool grab)
        {
            _movementType = grab ? MovementType.Dragging : MovementType.Normal;
        }

        private void HandleNetworkTick()
        {
            if (!enabled)
            {
                return;
            }

            ProcessCharacterMovement();
        }
    }
}
