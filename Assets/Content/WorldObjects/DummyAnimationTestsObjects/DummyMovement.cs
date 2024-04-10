using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;
using Object = System.Object;

namespace DummyStuff
{
    public class DummyMovement : Actor
    {
        [SerializeField]
        private Rigidbody rb;
        
        [SerializeField]
        private Transform aimTarget;

        [SerializeField]
        private MovementType movementType;
        
        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected void ProcessCharacterMovement()
        {
            ProcessPlayerInput();

            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                if(movementType != MovementType.Aiming)
                    RotatePlayerToMovement(movementType == MovementType.Dragging);
                MovePlayer();
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
            }
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected void MovePlayer()
        {
            rb.velocity = targetMovement * (movementSpeed * Time.fixedDeltaTime);
        }

        public event Action<float> OnSpeedChangeEvent;


        [Header("Movement Settings")]
        [SerializeField]
        private float movementSpeed;
        [SerializeField]
        private float lerpMultiplier;
        [SerializeField]
        private float rotationLerpMultiplier;

        [Header("Movement IK Targets")]
        [SerializeField]
        private Transform movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        protected Vector3 AbsoluteMovement;
        protected Vector2 Input;
        protected Vector2 SmoothedInput;
        
        [SerializeField]
        private Vector3 targetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;
        private InputSystem _inputSystem;
        
        private const float WalkAnimatorValue = .3f;
        private const float RunAnimatorValue = 1f;

        [SerializeField]
        private float aimRotationSpeed = 5f;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }
        
        protected override void OnDisabled()
        {
            base.OnDisabled();
            targetMovement = Vector3.zero;
        }

        private void Setup()
        {
            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _inputSystem = Subsystems.Get<InputSystem>();

            Controls controls = _inputSystem.Inputs;

            MovementControls = controls.Movement;
            HotkeysControls = controls.Hotkeys;
            MovementControls.ToggleRun.performed += HandleToggleRun;

            _inputSystem.ToggleActionMap(MovementControls, true);
            _inputSystem.ToggleActionMap(HotkeysControls, true);

            GetComponent<DummyThrow>().OnAim += HandleAimChange;
            GetComponent<DummyAim>().OnAim += HandleAimChange;
            GetComponent<Grab>().OnGrab += HandleGrabChange;

            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
        }

        private void HandleAimChange(Object sender, bool aim)
        {
            movementType = aim ? MovementType.Aiming : MovementType.Normal;
        }

        private void HandleGrabChange(Object sender, bool grab)
        {
            movementType = grab ? MovementType.Dragging : MovementType.Normal;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            MovementControls.ToggleRun.performed -= HandleToggleRun;
            _inputSystem.ToggleActionMap(MovementControls, false);
            _inputSystem.ToggleActionMap(HotkeysControls, false);
        }

        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            if (!enabled)
            {
                return;
            }

            ProcessCharacterMovement();
        }

        /// <summary>
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
        protected void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
        {
            Vector3 newTargetMovement;
            // in normal movement makes the movement align to the camera view.
            // else align on the current player rotation.
            if (movementType != MovementType.Aiming)
            {
                newTargetMovement = movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized
                    + movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized;
            }
            else
            {
                newTargetMovement = movementInput.y * Vector3.Cross(transform.right, Vector3.up).normalized 
                    + movementInput.x * Vector3.Cross(Vector3.up, transform.forward).normalized;
            }
               
            // smoothly changes the target movement
            targetMovement = Vector3.Lerp(targetMovement, newTargetMovement,
                Time.deltaTime * (lerpMultiplier * multiplier));

            Vector3 resultingMovement = targetMovement + Position;
            AbsoluteMovement = resultingMovement;
            movementTarget.position = AbsoluteMovement;
        }

        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        protected void RotatePlayerToMovement(bool lookOpposite)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookOpposite ? -targetMovement : targetMovement);
            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, 
                Time.deltaTime * rotationLerpMultiplier);
        }

        /// <summary>
        /// Process the player movement input, smoothing it
        /// </summary>
        /// <returns></returns>
        protected void ProcessPlayerInput()
        {
            float x = MovementControls.Movement.ReadValue<Vector2>().x;
            float y = MovementControls.Movement.ReadValue<Vector2>().y;

            float inputFilteredSpeed = FilterSpeed();

            Input = Vector2.ClampMagnitude(new Vector2(x, y), inputFilteredSpeed);
            SmoothedInput = Vector2.Lerp(SmoothedInput, Input, Time.deltaTime * (lerpMultiplier / 10));

            OnSpeedChanged(Input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        protected virtual float FilterSpeed()
        {
            return _isRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Toggles your movement between run/walk
        /// </summary>
        protected void HandleToggleRun(InputAction.CallbackContext context)
        {
            _isRunning = !_isRunning;
        }

        protected void OnSpeedChanged(float speed)
        {
            OnSpeedChangeEvent?.Invoke(speed);
        }
        
        public void RotatePlayerTowardTarget()
        {
            // Get the direction to the target
            Vector3 direction = aimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimRotationSpeed * Time.deltaTime);
            }
        }

    }
}
