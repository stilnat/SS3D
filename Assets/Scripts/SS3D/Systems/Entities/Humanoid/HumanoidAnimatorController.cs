using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
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
        }

        private void HandleGunAim(object sender, bool isAiming)
        {
            if (isAiming)
            {
                // Get the index of the layer named "UpperBody"
                _animator.SetBool("Aim", true);
            }
            else
            {
                _animator.SetBool("Aim", false);
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

        public void MakeFist(bool makeFist, bool isRight)
        {
            if (makeFist)
            {
              _animator.SetTrigger(isRight ? "FingerFistRight" : "FingerFistLeft");
            }
            else
            {
                _animator.SetTrigger(isRight ? "FingerRelaxedRight" : "FingerRelaxedLeft");
            }
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

        public void RemoveHandHolding(Hand hand, IHoldProvider holdable)
        {
            _animator.SetTrigger(hand.HandType == HandType.LeftHand ? "FingerRelaxedLeft" : "FingerRelaxedRight");

            if (holdable.CanHoldTwoHand && GetComponent<Hands>().TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Empty)
            {
                _animator.SetTrigger(oppositeHand.HandType == HandType.LeftHand ? "FingerRelaxedLeft" : "FingerRelaxedRight");
            }
        }

        public void AddHandHolding(Hand hand, IHoldProvider holdable)
        {
            switch (holdable.PrimaryHandPoseType)
            {
                 case FingerPoseType.Gun :
                     _animator.SetTrigger(hand.HandType == HandType.LeftHand ? "FingerGunLeft" : "FingerGunRight");
                     break;
            }

            if (holdable.CanHoldTwoHand && GetComponent<Hands>().TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Empty)
            {
                switch (holdable.PrimaryHandPoseType)
                {
                    case FingerPoseType.Gun :
                        _animator.SetTrigger(hand.HandType == HandType.LeftHand ? "FingerGunLeft" : "FingerGunRight");
                        break;
                }
            }
        }

        private void HandleNetworkTick()
        {
            _animator.SetFloat("AngleAimMove", (_movementController.InputAimAngle / 360f) + 0.5f);
        }
    }
}
