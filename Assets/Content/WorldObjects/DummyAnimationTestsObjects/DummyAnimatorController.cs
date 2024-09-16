using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace DummyStuff
{
    public class DummyAnimatorController : Actor
    {
        [SerializeField]
        private DummyMovement _movementController;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private float _lerpMultiplier;

        private bool _startMoving;
        private bool _endMoving;

        private bool _wasMovingPreviousUpdate;

        public void TriggerPickUp()
        {
            _animator.SetTrigger("PickUpRight");
        }

        public void Throw(HandType handtype)
        {
            if (handtype == HandType.RightHand)
            {
                _animator.SetTrigger("ThrowRight");
            }
            else
            {
                _animator.SetTrigger("ThrowLeft");
            }
        }

        public void Sit(bool sitState)
        {
            _animator.SetBool("Sit", sitState);
        }

        public void Crouch(bool crouchState)
        {
            _animator.SetBool("Crouch", crouchState);
        }

        protected override void OnStart()
        {
            base.OnStart();
            _movementController.OnSpeedChangeEvent += UpdateMovement;
            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            _movementController.OnSpeedChangeEvent -= UpdateMovement;
        }

        private void UpdateMovement(float speed)
        {
            bool isMoving = speed != 0;
            _startMoving = isMoving && !_wasMovingPreviousUpdate;
            _endMoving = !isMoving && _wasMovingPreviousUpdate;

            float currentSpeed = _animator.GetFloat(Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);

            _animator.SetFloat(Animations.Humanoid.MovementSpeed, speed);
            if (_startMoving)
            {
                _animator.SetTrigger(Animations.Humanoid.StartMoving);
            }

            if (_endMoving)
            {
                _animator.SetTrigger(Animations.Humanoid.EndMoving);
            }

            _wasMovingPreviousUpdate = isMoving;
        }

        private void HandleNetworkTick()
        {
            _animator.SetFloat("AngleAimMove", (_movementController.InputAimAngle / 360f) + 0.5f);
        }
    }
}
