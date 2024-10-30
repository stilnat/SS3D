using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet;
using FishNet.Object;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(Animator))]
    public abstract class HumanoidController : NetworkActor
    {
       public event Action<float> OnSpeedChangeEvent;

        protected const float WalkAnimatorValue = .3f;
        protected const float RunAnimatorValue = 1f;

        [SerializeField]
        private Rigidbody _rb;

        [SerializeField]
        private MovementType _movementType;

        [Header("Movement Settings")]
        [SerializeField]
        private float _movementSpeed;

        [SerializeField]
        private float _lerpMultiplier;

        [SerializeField]
        private float _rotationLerpMultiplier;


        [Header("Movement IK Targets")]
        [SerializeField]
        private Transform _movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        private Vector3 _absoluteMovement;
        private Vector2 _input;
        private Vector2 _smoothedInput;


        [SerializeField]
        private Vector3 _targetMovement;

        private Actor _camera;
        private Controls.MovementActions _movementControls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputSystem _inputSystem;


        [SerializeField]
        private float _aimRotationSpeed = 5f;

        private bool _isAiming;

        public float InputAimAngle { get; private set; }

        public Vector2 InputVector => _input;

        public Vector3 TargetMovement => _targetMovement;

        public bool IsRunning => _isRunning;

        public float MovementSpeed => _movementSpeed;

        [field: SerializeField]
        public Transform AimTarget { get; private set; }

        private void RotatePlayerTowardTarget()
        {
            // Get the direction to the target
            Vector3 direction = AimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRotation, (float)(_aimRotationSpeed * TimeManager.TickDelta));
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
            InstanceFinder.TimeManager.OnTick -= HandleNetworkTick;
            _inputSystem.ToggleActionMap(_movementControls, false);
            _inputSystem.ToggleActionMap(_hotkeysControls, false);
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets.
        /// </summary>
        protected virtual void ProcessCharacterMovement()
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

            if (_movementType == MovementType.Aiming && GetComponent<PositionController>().Position != PositionType.Sitting)
            {
                RotatePlayerTowardTarget();
            }

        }

        /// <summary>
        /// Moves the player to the target movement.
        /// </summary>
        protected virtual void MovePlayer()
        {
            //UnityEngine.Debug.Log($"tick delta = {TimeManager.TickDelta}, target movement = {_targetMovement}, movement speed = {_movementSpeed}");
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
            _targetMovement = Vector3.Lerp(
                _targetMovement, newTargetMovement, (float)(TimeManager.TickDelta * (_lerpMultiplier * multiplier)));

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
            transform.rotation = Quaternion.Slerp(
                Rotation, lookRotation, (float)(TimeManager.TickDelta * _rotationLerpMultiplier));
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
            _smoothedInput = Vector2.Lerp(
                _smoothedInput, _input, (float)(TimeManager.TickDelta * (_lerpMultiplier / 10)));

            OnSpeedChanged(_input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        protected virtual float FilterSpeed()
        {
            return _isRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Toggles your movement between run/walk.
        /// </summary>
        private void HandleToggleRun(InputAction.CallbackContext context)
        {
            _isRunning = !_isRunning;
        }

        private void OnSpeedChanged(float speed)
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
            GetComponent<AimController>().OnAim += HandleAimChange;
            //GetComponent<Grab>().OnGrab += HandleGrabChange;

            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
        }

        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            _movementType = isAiming ? MovementType.Aiming : MovementType.Normal;
        }

        private void ComputeAngleBetweenAimAndInput()
        {
            // Convert the target's position to 2D (XZ plane)
            Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
            Vector2 targetMove = new Vector2(_targetMovement.x, _targetMovement.z);

            InputAimAngle = Vector2.SignedAngle(targetMove, forward);
        }

        // todo : Implement a grab controller instead with a event call to change grab
        public void ChangeGrab(bool grab)
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

            if (_movementType == MovementType.Aiming)
            {
                ComputeAngleBetweenAimAndInput();
                UpdateAimTargetPosition();
            }
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
