﻿using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public class HumanoidAnimatorController : Actor
    {
        [SerializeField] private HumanoidController _movementController;

        [SerializeField] private Animator _animator;
        [SerializeField] private float _lerpMultiplier;

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
            _movementController.OnSpeedChangeEvent += UpdateMovement;
            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
            GetComponent<GunAimAnimation>().OnAim += HandleGunAim;
        }

        private void HandleGunAim(object sender, bool isAiming)
        {
            if (isAiming)
            {
                // Get the index of the layer named "UpperBody"
                int gunAimingLayerIndex = _animator.GetLayerIndex("Aiming");
                // Set the weight of the "UpperBody" layer to fully active
                _animator.SetLayerWeight(gunAimingLayerIndex, 1.0f);
            }
            else
            {
                int gunAimingLayerIndex = _animator.GetLayerIndex("Aiming");
                // Set the weight of the "UpperBody" layer to fully active
                _animator.SetLayerWeight(gunAimingLayerIndex, 0.0f);
            }
        }

        private void UnsubscribeFromEvents()
        {
            _movementController.OnSpeedChangeEvent -= UpdateMovement;
        }

        private void UpdateMovement(float speed)
        {
            bool isMoving = speed != 0;
            float currentSpeed = _animator.GetFloat(SS3D.Systems.Entities.Data.Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);
            
            _animator.SetFloat(SS3D.Systems.Entities.Data.Animations.Humanoid.MovementSpeed, speed);
        }

        public void Sit(bool sitState)
        {
            _animator.SetBool("Sit", sitState);
        }

        public void Crouch(bool crouchState)
        {
            _animator.SetBool("Crouch", crouchState);
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

        private void HandleNetworkTick()
        {
            _animator.SetFloat("AngleAimMove", (_movementController.InputAimAngle / 360f) + 0.5f);
        }
    }
}
