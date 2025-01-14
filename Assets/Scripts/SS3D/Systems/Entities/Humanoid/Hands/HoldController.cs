using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Handle the state of holding items and hand positions
    /// </summary>
    public class HoldController : NetworkActor
    {
        /// <summary>
        /// Simple struct for syncing purpose
        /// </summary>
        private struct HandItem
        {
            public readonly AbstractHoldable Holdable;
            public Hand Hand;

            public HandItem(Hand hand, AbstractHoldable holdable)
            {
                Holdable = holdable;
                Hand = hand;
            }
        }

        /// <summary>
        /// Keeps track of items held in each hand
        /// </summary>
        [SyncObject]
        private readonly SyncList<HandItem> _itemsInHands = new();

        private readonly Dictionary<HandHoldType, string> _rightHandHoldTypeToAnimatorPoseParameter = new();

        private readonly Dictionary<HandHoldType, string> _leftHandHoldTypeToAnimatorPoseParameter = new();

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private AimController _aimController;

        private string _rightHandCurrentPose = "ArmIdlePoseRight";

        private string _leftHandCurrentPose = "ArmIdlePoseLeft";

        public override void OnStartServer()
        {
            base.OnStartServer();

            foreach (Hand hand in GetComponentsInChildren<Hand>())
            {
                hand.OnContentsChanged += HandleHandContentChanged;
            }

            GetComponent<Ragdoll>().OnRagdoll += HandleRagdoll;
        }

        public void BringToHand(Hand hand, AbstractHoldable holdable, float duration)
        {
            StartCoroutine(CoroutineBringToHand(hand, holdable, duration));
        }

        public void UpdatePose([NotNull] Hand hand, [NotNull] AbstractHoldable item, float duration)
        {
            StartCoroutine(UpdatePoseCoroutine(hand, item, duration));
        }

        protected override void OnAwake()
        {
            _itemsInHands.OnChange += SyncItemsInHandsChanged;
            _intents.OnIntentChange += HandleIntentChange;
            _aimController.OnAim += HandleAimChange;

            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] = "ArmIdlePoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandGun] = "RifleRestPoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.UnderArm] = "UnderArmPoseRight";
            _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandGunHarm] = "RifleHarmPoseRight";

            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] = "ArmIdlePoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandGun] = "RifleRestPoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.UnderArm] = "UnderArmPoseLeft";
            _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.DoubleHandGunHarm] = "RifleHarmPoseLeft";
        }

        private IEnumerator CoroutineBringToHand([NotNull] Hand hand, [NotNull] AbstractHoldable holdable, float duration)
        {
            holdable.GetComponent<Item>().SetFreeze(true);
            Transform holdOnItem = holdable.GetHold(true, hand.HandType);
            Vector3 startPosition = holdable.transform.position;
            Quaternion startRotation = holdable.transform.rotation;

            // multiply by inverse of hold transform to "remove" the hold parent rotation.
            hand.Hold.Pivot.transform.localRotation = Quaternion.Inverse(hand.Hold.HoldTransform.localRotation) * Quaternion.Inverse(holdOnItem.localRotation);

            // Smoothly move item toward the target position
            for (float timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
            {
                float factor = timePassed / duration;

                holdable.transform.position = Vector3.Lerp(startPosition, hand.Hold.Pivot.transform.position, factor);
                holdable.transform.localRotation = Quaternion.Lerp(startRotation,  hand.Hold.Pivot.transform.rotation, factor);

                yield return null;
            }

            holdable.transform.SetParent(hand.Hold.Pivot.transform, true);

            // Assign the relative position between the attachment point and the object
            holdable.transform.localPosition = -holdOnItem.localPosition;
            holdable.transform.localRotation = Quaternion.identity;
        }

        private IEnumerator UpdatePoseCoroutine([NotNull] Hand hand, AbstractHoldable item, float duration)
        {
            string currentPose;
            string targetPose;
            bool isRight = hand.HandType == HandType.RightHand;

            if (item)
            {
                _animator.SetLayerWeight(_animator.GetLayerIndex(isRight ? "ArmRight" : "ArmLeft"), 1);
                bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && item.CanHoldTwoHand && oppositeHand.Empty;
                bool toThrow = _aimController.IsAimingToThrow;

                // Fetch how the item should be held
                HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent, toThrow);

                currentPose = isRight ? _rightHandCurrentPose : _leftHandCurrentPose;
                targetPose = isRight ? _rightHandHoldTypeToAnimatorPoseParameter[itemHoldType] : _leftHandHoldTypeToAnimatorPoseParameter[itemHoldType];
            }
            else
            {
                _animator.SetLayerWeight(_animator.GetLayerIndex(isRight ? "ArmRight" : "ArmLeft"), 0);
                currentPose = isRight ? _rightHandCurrentPose : _leftHandCurrentPose;
                targetPose = isRight ? _rightHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem] : _leftHandHoldTypeToAnimatorPoseParameter[HandHoldType.SmallItem];
            }

            if (currentPose == targetPose)
            {
                yield break;
            }

            // Smoothly move item toward the target position
            for (float timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
            {
                _animator.SetFloat(currentPose, 1 - (timePassed / duration));
                _animator.SetFloat(targetPose, timePassed / duration);
                yield return null;
            }

            _animator.SetFloat(currentPose, 0f);
            _animator.SetFloat(targetPose, 1f);

            if (isRight)
            {
                _rightHandCurrentPose = targetPose;
            }
            else
            {
                _leftHandCurrentPose = targetPose;
            }
        }

        /// <summary>
        /// This method is necessary to sync between clients items held in hand, for instance for late joining client, or simply client far away on the map.
        /// </summary>
        private void SyncItemsInHandsChanged(SyncListOperation op, int index, HandItem oldItem, HandItem newItem, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            switch (op)
            {
                case SyncListOperation.Add or SyncListOperation.Insert or SyncListOperation.Set:
                {
                    AddItem(newItem.Hand, newItem.Holdable);
                    break;
                }

                case SyncListOperation.RemoveAt:
                {
                    RemoveItem(oldItem.Hand);
                    break;
                }
            }
        }

        [Client]
        private void RemoveItem(Hand hand)
        {
            StartCoroutine(UpdatePoseCoroutine(hand, null, 0.2f));

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full && oppositeHand.ItemHeld.Holdable.CanHoldTwoHand)
            {
                StartCoroutine(UpdatePoseCoroutine(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f));
                hand.Hold.HoldIkConstraint.weight = 1f;
                hand.Hold.HandTargetFollowHold(true, oppositeHand.ItemHeld.Holdable);
            }
        }

        [Client]
        private void AddItem([NotNull] Hand hand, [NotNull] AbstractHoldable holdable)
        {
            BringToHand(hand, holdable, 0f);
            StartCoroutine(UpdatePoseCoroutine(hand, holdable, 0f));

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full)
            {
                StartCoroutine(UpdatePoseCoroutine(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f));
            }
        }

        [Server]
        private void HandleHandContentChanged(Hand hand, Item oldItem, Item newItem, ContainerChangeType type)
        {
            int handIndex = _itemsInHands.FindIndex(x => x.Hand = hand);

            if (type == ContainerChangeType.Add)
            {
                if (handIndex == -1)
                {
                    _itemsInHands.Add(new(hand, newItem.Holdable));
                }
                else
                {
                    _itemsInHands[handIndex] = new(hand, newItem.Holdable);
                }
            }

            if (type == ContainerChangeType.Remove && handIndex != -1)
            {
                _itemsInHands.RemoveAt(handIndex);
            }
        }

        // TODO : add a handle for selected hand change, and update only visually the intent for selected hand
        private void HandleIntentChange(object sender, IntentType e)
        {
            Hand mainHand = _hands.SelectedHand;

            if (mainHand.Full)
            {
                StartCoroutine(UpdatePoseCoroutine(mainHand, mainHand.ItemHeld.Holdable, 0.25f));
            }
        }

        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            if (!_hands.SelectedHand.ItemHeld)
            {
                return;
            }

            AbstractHoldable holdable = _hands.SelectedHand.ItemHeld.Holdable;
            Gun gun = null;

            // handle aiming with shoulder aim
            if (holdable && holdable.TryGetComponent(out gun) && !toThrow && isAiming)
            {
                gun.transform.parent = _hands.SelectedHand.Hold.ShoulderWeaponPivot;

                // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
                gun.transform.localPosition = -gun.RifleButt.localPosition;
                gun.transform.localRotation = Quaternion.identity;
            }

            // Stop aiming with shoulder aim
            else if (gun && !isAiming)
            {
                _hands.SelectedHand.ItemHeld.GameObject.transform.parent = _hands.SelectedHand.Hold.ItemPositionTargetLocker;
                _hands.SelectedHand.ItemHeld.GameObject.transform.localPosition = Vector3.zero;
                _hands.SelectedHand.ItemHeld.GameObject.transform.localRotation = Quaternion.identity;
                StartCoroutine(UpdatePoseCoroutine(_hands.SelectedHand, _hands.SelectedHand.ItemHeld.Holdable, 0.2f));
            }

            // if it's not a gun, or if its to throw it
            else
            {
                StartCoroutine(UpdatePoseCoroutine(_hands.SelectedHand, _hands.SelectedHand.ItemHeld.Holdable, 0.2f));
            }
        }

        [Server]
        private void HandleRagdoll(bool isRagdoll)
        {
            if (!isRagdoll)
            {
                return;
            }

            foreach (Hand hand in GetComponentsInChildren<Hand>())
            {
                hand.DropHeldItem();
            }
        }
    }
}
