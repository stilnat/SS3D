using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
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

        private sealed record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, HandType PrimaryHand);

        [SyncObject]
        private readonly SyncList<HandItem> _itemsInHands = new();

        private readonly List<HoldAndOffset> _holdData = new();

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private AimController _aimController;


        // Hold transforms, the transforms where items are going to position themselves when held in hand
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



        public override void OnStartServer()
        {
            base.OnStartServer();

            foreach (Hand hand in GetComponentsInChildren<Hand>())
            {
                hand.OnContentsChanged += HandleHandContentChanged;
            }
        }

        protected override void OnAwake()
        {
            _itemsInHands.OnChange += HandleItemsInHandsChanged;
            _intents.OnIntentChange += HandleIntentChange;
            _aimController.OnAim += HandleAimChange;
            GetComponent<GunAimAnimation>().OnAim += HandleGunAimChange;

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
        }

        public void SetItemConstraintPositionAndRotation(Hand hand, IHoldProvider item)
        {
            bool toThrow = _aimController.IsAiming;
            bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && item.CanHoldTwoHand && oppositeHand.Empty;

            // Fetch how the item should be held
            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent, toThrow);

            // The position where the item should be, given its hold type
            Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            hand.ItemPositionConstraint.data.offset = OffsetFromHoldTypeAndHand(itemHoldType, hand.HandType);
            hand.ItemPositionConstraint.data.constrainedObject.localRotation = hold.localRotation;
        }

        /// <summary>
        /// Update the held item position and rotation IK target of the relevant hand, so that item held are placed at the right place. 
        /// </summary>
        /// <param name="hand"> The main hand holding the item</param>
        /// <param name="item"> The item held</param>
        /// <param name="withTwoHands"> If the item should be held with two hands </param>
        /// <param name="duration"> The time in second to go from the current item position to its updated position</param>
        /// <param name="toThrow"> True the item should be in a ready to throw position</param>
        private void UpdateItemPositionConstraintAndRotation(
            Hand hand, IHoldProvider item, float duration)
        {
            if (item == null)
            {
                return;
            }

            bool toThrow = _aimController.IsAiming;
            bool withTwoHands = _hands.TryGetOppositeHand(hand, out Hand oppositeHand) && item.CanHoldTwoHand && oppositeHand.Empty;

            // Fetch how the item should be held
            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent, toThrow);

            // The position where the item should be, given its hold type
            Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            // Interpolate from current position to updated position the constraint offset, so that item goes to the right hold position.
            Vector3 startingOffset = hand.ItemPositionConstraint.data.offset;
            Vector3 finalOffset = OffsetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            StartCoroutine(CoroutineHelper.ModifyVector3OverTime(
                x => hand.ItemPositionConstraint.data.offset = x, startingOffset, finalOffset, duration));

            // Do the same with interpolating rotation.
            Quaternion startingRotation = hand.ItemPositionConstraint.data.constrainedObject.localRotation;
            Quaternion finalRotation = hold.localRotation;

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(
                x => hand.ItemPositionConstraint.data.constrainedObject.localRotation = x, startingRotation, finalRotation, duration));
        }

        /// <summary>
        /// For a given hand, move its pickup and hold target lockers at the right place on the item.
        /// </summary>
        /// <param name="hand"> The hand for which we want to set picking and holding </param>
        /// <param name="secondary"> Is it the hand with the primary hold on the item, or is it just there in support of the first one ? </param>
        /// <param name="holdProvider"> The item we want to hold, and onto which we're going to move the targets.</param>
        public void MovePickupAndHoldTargetLocker(Hand hand, bool secondary, IHoldProvider holdProvider)
        {
            Transform parent = holdProvider.GetHold(!secondary, hand.HandType);

            hand.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);
            hand.SetParentTransformTargetLocker(TargetLockerType.Hold, parent);
        }

        /// <summary>
        /// This method is necessary to sync between clients items held in hand, for instance for late joining client, or simply client far away on the map.
        /// </summary>
        private void HandleItemsInHandsChanged(SyncListOperation op, int index, HandItem oldItem, HandItem newItem, bool asServer)
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

        private void RemoveItem(Hand hand)
        {
            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full && oppositeHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemInHand.Holdable, 0.2f);
            }
        }

        private void AddItem(Hand hand, AbstractHoldable holdable)
        {
            // Put the holdable on the hand item position target locker and constrain it
            holdable.GameObject.transform.parent = hand.ItemPositionTargetLocker;
            holdable.GameObject.transform.localRotation = Quaternion.identity;
            holdable.GameObject.transform.localPosition = Vector3.zero;
            hand.ItemPositionConstraint.weight = 1f;

            // enable the hold constraint as well
            hand.HoldIkConstraint.weight = 1f;

            MovePickupAndHoldTargetLocker(hand, false, holdable);
            UpdateItemPositionConstraintAndRotation(hand, holdable, 0f);

            if (_hands.TryGetOppositeHand(hand, out Hand oppositeHand) && oppositeHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(oppositeHand, oppositeHand.ItemInHand.Holdable, 0.2f);
            }
        }

        private void HandleHandContentChanged(Hand hand, Item olditem, Item newitem, ContainerChangeType type)
        {
            int handIndex = _itemsInHands.FindIndex(x => x.Hand = hand);

            if (type == ContainerChangeType.Add)
            {
                if (handIndex == -1)
                {
                    _itemsInHands.Add(new HandItem(hand, newitem.Holdable as AbstractHoldable));
                }
                else
                {
                    _itemsInHands[handIndex] = new HandItem(hand, newitem.Holdable as AbstractHoldable);
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

        private void HandleAimChange(object sender, bool e)
        {
            UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, 0.2f);
        }


        private void HandleGunAimChange(object sender, bool e)
        {
            UpdateItemPositionConstraintAndRotation(_hands.SelectedHand, _hands.SelectedHand.ItemInHand.Holdable, 0.2f);
        }

        private Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
        }

        private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            Transform selected = TargetFromHoldTypeAndHand(handHoldType, selectedHand);

            return selected.localPosition;
        }
    }
}
