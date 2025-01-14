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
        /// Record holding the hold target for each HoldType/HandType
        /// </summary>
        private sealed record HoldData(HandHoldType HandHoldType, Transform HoldTarget, HandType PrimaryHand);

        /// <summary>
        /// Keeps track of items held in each hand
        /// </summary>
        [SyncObject]
        private readonly SyncList<HandItem> _itemsInHands = new();

        /// <summary>
        /// List of all hold transform, for each HoldType/HandType, those transform are used to determine item position when held.
        /// </summary>
        private readonly List<HoldData> _holdData = new();

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private AimController _aimController;

        // Hold transforms, the transforms where items are going to position themselves when held in hand.
        // Left transform are parented under the left shoulder, and Right transforms under the right one.
        [SerializeField]
        private Transform _gunHoldRight;

        [SerializeField]
        private Transform _gunHoldLeft;

        [SerializeField]
        private Transform _toolboxHoldRight;

        [SerializeField]
        private Transform _toolBoxHoldLeft;

        [SerializeField]
        private Transform _shoulderHoldRight;

        [SerializeField]
        private Transform _shoulderHoldLeft;

        [SerializeField]
        private Transform _gunHoldHarmRight;

        [SerializeField]
        private Transform _gunHoldHarmLeft;

        [SerializeField]
        private Transform _throwToolboxLeft;

        [SerializeField]
        private Transform _throwToolboxRight;

        [SerializeField]
        private Transform _smallItemRight;

        [SerializeField]
        private Transform _smallItemLeft;

        [SerializeField]
        private Transform _smallItemRightThrow;

        [SerializeField]
        private Transform _smallItemLeftThrow;

        [SerializeField]
        private Transform _underArmLeft;

        [SerializeField]
        private Transform _underArmRight;

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

        /// <summary>
        /// Update the held item position and rotation IK target of the relevant hand, so that item held are placed at the right place.
        /// </summary>
        /// <param name="hand"> The main hand holding the item</param>
        /// <param name="item"> The item held</param>
        /// <param name="duration"> The time in second to go from the current item position to its updated position</param>
        public void UpdateItemPositionConstraintAndRotation([NotNull] Hand hand, [NotNull] AbstractHoldable item, float duration)
        {
            bool toThrow = _aimController.IsAimingToThrow;
            bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && item.CanHoldTwoHand && oppositeHand.Empty;

            // Fetch how the item should be held
            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent, toThrow);

            // The position where the item should be, given its hold type
            Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            // Interpolate from current position to updated position the constraint offset, so that item goes to the right hold position.
            Vector3 finalOffset = OffsetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            DOTween.To(() => hand.Hold.ItemPositionConstraint.data.offset, x => hand.Hold.ItemPositionConstraint.data.offset = x, finalOffset, duration);

            // Do the same with interpolating rotation.
            hand.Hold.ItemPositionConstraint.data.constrainedObject.DOLocalRotate(hold.localRotation.eulerAngles, duration);
        }

        protected override void OnAwake()
        {
            _itemsInHands.OnChange += SyncItemsInHandsChanged;
            _intents.OnIntentChange += HandleIntentChange;
            _aimController.OnAim += HandleAimChange;

            _holdData.Add(new(HandHoldType.DoubleHandGun, _gunHoldLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGun, _gunHoldRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.Toolbox, _toolBoxHoldLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Toolbox, _toolboxHoldRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.Shoulder, _shoulderHoldLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Shoulder, _shoulderHoldRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, _gunHoldHarmLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, _gunHoldHarmRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.SmallItem, _smallItemLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.SmallItem, _smallItemRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, _throwToolboxLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, _throwToolboxRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.UnderArm, _underArmLeft, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.UnderArm, _underArmRight, HandType.RightHand));
            _holdData.Add(new(HandHoldType.ThrowSmallItem, _smallItemLeftThrow, HandType.LeftHand));
            _holdData.Add(new(HandHoldType.ThrowSmallItem, _smallItemRightThrow, HandType.RightHand));
        }

        private IEnumerator CoroutineBringToHand([NotNull] Hand hand, [NotNull] AbstractHoldable holdable, float duration)
        {
            Vector3 start = holdable.GameObject.transform.position;
            bool toThrow = _aimController.IsAimingToThrow;
            bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && holdable.CanHoldTwoHand && oppositeHand.Empty;

            // Fetch how the item should be held
            HandHoldType itemHoldType = holdable.GetHoldType(withTwoHands, _intents.Intent, toThrow);

            Transform target = TargetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            // Smoothly move item toward the target position
            for (float timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
            {
                float factor = timePassed / duration;
                factor = Mathf.SmoothStep(0, 1, factor);

                holdable.GameObject.transform.position = Vector3.Lerp(start, target.position, factor);

                yield return null;
            }

            holdable.GameObject.transform.position = target.position;

            if (itemHoldType == HandHoldType.SmallItem)
            {
                holdable.transform.SetParent(hand.Hold.Pivot.transform, false);

                Transform holdOnItem = holdable.GetHold(true, hand.HandType);

                // multiply by inverse of hold transform to "remove" the hold parent rotation.
                hand.Hold.Pivot.transform.localRotation = Quaternion.Inverse(hand.Hold.HoldTransform.localRotation) * Quaternion.Inverse(holdOnItem.localRotation);

                // Assign the relative position between the attachment point and the object
                holdable.transform.localPosition = -holdOnItem.localPosition;
                holdable.transform.localRotation = Quaternion.identity;
                hand.Hold.HoldIkConstraint.weight = 0f;
            }
            else
            {
                holdable.transform.parent = hand.Hold.ItemPositionTargetLocker;
                holdable.GameObject.transform.localRotation = Quaternion.identity;
                holdable.GameObject.transform.localPosition = Vector3.zero;

                hand.Hold.ItemPositionConstraint.weight = 1f;

                // enable the hold constraint as well
                hand.Hold.HoldIkConstraint.weight = 1f;
                hand.Hold.HandTargetFollowHold(false, holdable);
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
            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full && oppositeHand.ItemHeld.Holdable.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f);
                hand.Hold.HoldIkConstraint.weight = 1f;
                hand.Hold.HandTargetFollowHold(true, oppositeHand.ItemHeld.Holdable);
            }
        }

        [Client]
        private void AddItem([NotNull] Hand hand, [NotNull] AbstractHoldable holdable)
        {
            BringToHand(hand, holdable, 0f);
            UpdateItemPositionConstraintAndRotation(hand, holdable, 0f);

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemHeld.Holdable, 0.2f);
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
                UpdateItemPositionConstraintAndRotation(mainHand, mainHand.ItemHeld.Holdable, 0.25f);
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
                UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemHeld.Holdable, 0.2f);
            }

            // if it's not a gun, or if its to throw it
            else
            {
                UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemHeld.Holdable, 0.2f);
            }
        }

        /// <summary>
        /// Return a target transform, given a left or right hand, and a type of hold
        /// </summary>
        private Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
        }

        private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            Transform selected = TargetFromHoldTypeAndHand(handHoldType, selectedHand);

            return selected.localPosition;
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
