using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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
            public AbstractHoldable Holdable;
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


        /// <summary>
        /// Update the held item position and rotation IK target of the relevant hand, so that item held are placed at the right place. 
        /// </summary>
        /// <param name="hand"> The main hand holding the item</param>
        /// <param name="item"> The item held</param>
        /// <param name="withTwoHands"> If the item should be held with two hands </param>
        /// <param name="duration"> The time in second to go from the current item position to its updated position</param>
        /// <param name="toThrow"> True the item should be in a ready to throw position</param>
        public void UpdateItemPositionConstraintAndRotation(
            Hand hand, AbstractHoldable item, float duration)
        {
            if (item == null)
            {
                return;
            }

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
            hand.Hold.ItemPositionConstraint.data.constrainedObject.DOLocalRotate(hold.localRotation.eulerAngles, duration); ;
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

            if (op == SyncListOperation.Add || op == SyncListOperation.Insert || op == SyncListOperation.Set)
            {
                AddItem(newItem.Hand, newItem.Holdable);
            }

            if (op == SyncListOperation.RemoveAt)
            {
                RemoveItem(oldItem.Hand);
            }
        }

        [Client]
        private void RemoveItem(Hand hand)
        {
            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full && oppositeHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemInHand.Holdable, 0.2f);
                hand.Hold.HoldIkConstraint.weight = 1f;
                hand.Hold.ParentHandIkTargetOnHold(true, oppositeHand.ItemInHand.Holdable);
            }
        }

        [Client]
        private void AddItem(Hand hand, AbstractHoldable holdable)
        {
            // Put the holdable on the hand item position target locker and constrain it
            holdable.GameObject.transform.parent = hand.Hold.ItemPositionTargetLocker;
            holdable.GameObject.transform.localRotation = Quaternion.identity;
            holdable.GameObject.transform.localPosition = Vector3.zero;
            hand.Hold.ItemPositionConstraint.weight = 1f;

            // enable the hold constraint as well
            hand.Hold.HoldIkConstraint.weight = 1f;
            hand.Hold.ParentHandIkTargetOnHold(false, holdable);
            UpdateItemPositionConstraintAndRotation(hand, holdable, 0f);

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemInHand.Holdable, 0.2f);
            }
        }

        [Server]
        private void HandleHandContentChanged(Hand hand, Item olditem, Item newitem, ContainerChangeType type)
        {
            int handIndex = _itemsInHands.FindIndex(x => x.Hand = hand);

            if (type == ContainerChangeType.Add)
            {
                if (handIndex == -1)
                {
                    _itemsInHands.Add(new(hand, newitem.Holdable));
                }
                else
                {
                    _itemsInHands[handIndex] = new(hand, newitem.Holdable);
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
                UpdateItemPositionConstraintAndRotation(mainHand, mainHand.ItemInHand.Holdable, 0.25f);
            }
        }

        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            AbstractHoldable holdable = _hands.SelectedHand.ItemInHand?.Holdable;

            // handle aiming with shoulder aim
            if (holdable.TryGetComponent(out Gun gun) && !toThrow && isAiming)
            {
                gun.transform.parent = _hands.SelectedHand.Hold.ShoulderWeaponPivot;

                // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
                gun.transform.localPosition = -gun.RifleButt.localPosition;
                gun.transform.localRotation = Quaternion.identity;
            }
            // Stop aiming with shoulder aim
            else if (gun && !isAiming)
            {
                _hands.SelectedHand.ItemInHand.GameObject.transform.parent = _hands.SelectedHand.Hold.ItemPositionTargetLocker;
                _hands.SelectedHand.ItemInHand.GameObject.transform.localPosition = Vector3.zero;
                _hands.SelectedHand.ItemInHand.GameObject.transform.localRotation = Quaternion.identity;
                UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, 0.2f);
            }
            // if it's not a gun, or if its to throw it
            else
            {
                UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, 0.2f);
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
        private void HandleRagdoll(bool isRagdolled)
        {
            if (isRagdolled)
            {
                foreach (Hand hand in GetComponentsInChildren<Hand>())
                {
                    hand.DropHeldItem();
                }
            }
        }
    }
}
