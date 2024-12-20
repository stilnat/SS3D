﻿using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Entities.Silicon
{
    public class EngineerBorgAnimatorController : Actor
    {
        [SerializeField]
        private ThreadController _movementController;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private float _lerpMultiplier;

        protected override void OnStart()
        {
            base.OnStart();
            SubscribeToEvents();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _movementController.OnSpeedChanged += UpdateMovement;
            _movementController.OnPowerChanged += UpdatePower;
        }

        private void UpdatePower(bool power)
        {
            _animator.SetBool(SS3D.Systems.Entities.Data.Animations.Silicon.Power, power);
        }

        private void UnsubscribeFromEvents()
        {
            _movementController.OnSpeedChanged -= UpdateMovement;
            _movementController.OnPowerChanged -= UpdatePower;
        }

        private void UpdateMovement(float speed)
        {
            bool isMoving = speed != 0;
            float currentSpeed = _animator.GetFloat(SS3D.Systems.Entities.Data.Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);
            _animator.SetFloat(SS3D.Systems.Entities.Data.Animations.Humanoid.MovementSpeed, speed);
        }
    }
}
