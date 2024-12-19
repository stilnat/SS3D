using FishNet;
using FishNet.Component.Animating;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public class HumanoidAnimatorController : Actor
    {
        [SerializeField] private HumanoidMovementController _movementController;

        [SerializeField] private Animator _animator;

        [SerializeField] private NetworkAnimator _networkAnimator;

        [SerializeField] private float _lerpMultiplier;

        [SerializeField] private AimController _aimController;

        [SerializeField] private PositionController _positionController;

        private bool _isRagdoll;

        private static readonly int AngleAimMove = Animator.StringToHash("AngleAimMove");
        private static readonly int Aim = Animator.StringToHash("Aim");
        private static readonly int TorsoGunAim = Animator.StringToHash("TorsoGunAim");

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

        public void Dance(bool dance, float transitionDuration = 0.15f)
        {
            _animator.CrossFade(dance ? "Dance" : "Move", transitionDuration);
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

        public void RemoveHandHolding(Hand hand, AbstractHoldable holdable)
        {
            _animator.SetTrigger(hand.HandType == HandType.LeftHand ? "FingerRelaxedLeft" : "FingerRelaxedRight");

            if (holdable.CanHoldTwoHand && GetComponent<Hands>().TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Empty)
            {
                _animator.SetTrigger(oppositeHand.HandType == HandType.LeftHand ? "FingerRelaxedLeft" : "FingerRelaxedRight");
            }
        }

        public void AddHandHolding(Hand hand, AbstractHoldable holdable)
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
            _animator.SetFloat(AngleAimMove, (_movementController.InputAimAngle / 360f) + 0.5f);
        }

        private void SubscribeToEvents()
        {
            _movementController.OnSpeedChangeEvent += UpdateSpeedParamater;
            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
            _aimController.OnAim += HandleAimInAnimatorControler;
            _positionController.ChangedPosition += HandlePositionChanged;
            _positionController.ChangedMovement += HandleMovementChanged;
            _positionController.Dance += HandleDance;
        }

        private void HandleDance(bool isDancing)
        {
            Dance(isDancing);
        }

        private void HandleMovementChanged(MovementType movementType)
        {
            switch (movementType)
            {
                case MovementType.Dragging:
                    Grab();
                    break;
                case MovementType.Normal:
                    HandlePositionChanged(_positionController.PositionType);
                    break;
            }
        }

        private void HandlePositionChanged(PositionType position, float transitionDuration = 0.15f)
        {
            if (position != PositionType.Ragdoll && position != PositionType.ResetBones)
            {
                ToggleAnimator(true);
            }

            switch (position)
            {
                  case PositionType.Proning:
                      Prone();
                      break;
                  case PositionType.Crouching:
                      Crouch();
                      break;
                  case PositionType.Standing:
                      StandUp();
                      break;
                  case PositionType.Sitting:
                      Sit();
                      break;
                  case PositionType.RagdollRecover:
                      RagdollRecover(transitionDuration);
                      break;
                  case PositionType.Ragdoll:
                      ToggleAnimator(false);
                      break;
            }
        }

        private void RagdollRecover(float transitionDuration)
        {
            AnimationClip clipToPlay = _positionController.GetRecoverFromRagdollClip();
            float playbackSpeed = clipToPlay.length / transitionDuration;
            _animator.speed = playbackSpeed;
            _animator.Play(clipToPlay.name);
            Invoke(nameof(RestoreAnimatorSpeed), transitionDuration);
        }

        private void RestoreAnimatorSpeed()
        {
            _animator.speed = 1;
        }

        private void Sit(float transitionDuration = 0.15f)
        {
            _animator.CrossFade("Sit", transitionDuration);
        }

        private void Crouch(float transitionDuration = 0.15f)
        {
            _animator.CrossFade("Crouch", transitionDuration);
        }

        private void Prone(float transitionDuration = 0.15f)
        {
            _animator.CrossFade("Prone", transitionDuration);
        }

        private void StandUp(float transitionDuration = 0.25f)
        {
            _animator.CrossFade("Move", transitionDuration);
        }

        private void Grab(float transitionDuration = 0.15f)
        {
            _animator.CrossFade("Drag", transitionDuration);
        }

        private void HandleAimInAnimatorControler(bool isAiming, bool toThrow)
        {

            UnityEngine.Debug.Log("animator change aim");
            _animator.SetBool(Aim, isAiming);
            _animator.SetBool(TorsoGunAim, !toThrow);

            _animator.SetLayerWeight(_animator.GetLayerIndex("UpperBodyLayer"), isAiming ? 1 : 0);
        }

        private void UpdateSpeedParamater(float speed)
        {
           
            bool isMoving = speed != 0;

            // divide by max speed to get a parameter between 0 and 1
            speed /= _movementController.MaxSpeed; 

            float currentSpeed = _animator.GetFloat(Data.Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);
            
            _animator.SetFloat(Data.Animations.Humanoid.MovementSpeed, speed);
        }

        private void UnsubscribeFromEvents()
        {
            _movementController.OnSpeedChangeEvent -= UpdateSpeedParamater;
        }

        private void ToggleAnimator(bool animatorEnabled)
        {
            _animator.enabled = animatorEnabled;
            _networkAnimator.enabled = animatorEnabled;
        }

    }
}
