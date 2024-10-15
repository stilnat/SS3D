using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Handle the state of holding items and hand positions
    /// </summary>
    public class HoldController : MonoBehaviour
    {
        private sealed record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, HandType PrimaryHand);

        private readonly List<HoldAndOffset> _holdData = new List<HoldAndOffset>();

        [SerializeField]
        private IntentController _intents;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private PickUpAnimation _pickup;

        // Hold positions, the place where 
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
        private ThrowAnimations _throwAnimations;


        /// <summary>
        /// Update the held item position and rotation. 
        /// </summary>
        /// <param name="hand"> The main hand holding the item</param>
        /// <param name="item"> The item held</param>
        /// <param name="withTwoHands"> If the item should be held with two hands </param>
        /// <param name="duration"> The time in second to go from the current item position to its updated position</param>
        /// <param name="toThrow"> True the item should be in a ready to throw position</param>
        public void UpdateItemPositionConstraintAndRotation(
            Hand hand, IHoldProvider item, bool withTwoHands, float duration, bool toThrow)
        {
            if (item == null)
            {
                return;
            }

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

        protected void Start()
        {
            _intents.OnIntentChange += HandleIntentChange;

            // Had to manually enter the offset for each possible position.
            // The first idea was to use game objects for that, which would allow a more dynamic way of setting the items position.
            // The offset is relative to the human shoulder.
            _holdData.Add(new(HandHoldType.DoubleHandGun, _gunHoldLeft, new Vector3(0.15f, -0.08f, 0.26f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGun, _gunHoldRight, new Vector3(-0.15f, -0.08f, 0.26f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.Toolbox, _toolBoxHoldLeft, new Vector3(-0.1f, -0.4f, 0.1f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Toolbox, _toolboxHoldRight, new Vector3(0.1f, -0.4f, 0.1f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.Shoulder, _shoulderHoldLeft, new Vector3(0f, 0.18f, 0f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Shoulder, _shoulderHoldRight, new Vector3(0f, 0.18f, 0f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, _gunHoldHarmLeft, new Vector3(0f, -0.07f, 0.18f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, _gunHoldHarmRight, new Vector3(0f, -0.07f, 0.18f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.SmallItem, _smallItemLeft, new Vector3(0f, -0.35f, 0.25f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.SmallItem, _smallItemRight, new Vector3(0f, -0.35f, 0.25f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, _throwToolboxLeft, new Vector3(-0.23f, 0.3f, -0.03f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, _throwToolboxRight, new Vector3(0.23f, 0.3f, -0.03f), HandType.RightHand));
        }

        // TODO : add a handle for selected hand change, and update only visually the intent for selected hand
        private void HandleIntentChange(object sender, IntentType e)
        {
            Hand mainHand = _hands.SelectedHand;
            _hands.TryGetOppositeHand(mainHand, out Hand secondaryHand);

            if (mainHand.Full && secondaryHand.Empty && mainHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(
                    mainHand, mainHand.ItemInHand.Holdable, true, 0.25f, _throwAnimations.IsAiming);
            }
            else if (mainHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(
                    mainHand, mainHand.ItemInHand.Holdable, false, 0.25f, _throwAnimations.IsAiming);
            }

            if (secondaryHand.Full && mainHand.Empty && secondaryHand.ItemInHand.Holdable.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(
                    secondaryHand, secondaryHand.ItemInHand.Holdable, true, 0.25f, _throwAnimations.IsAiming);
            }
            else if (secondaryHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(
                    secondaryHand, secondaryHand.ItemInHand.Holdable, false, 0.25f, _throwAnimations.IsAiming);
            }
        }

        private Transform TargetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).HoldTarget;
        }

        private Vector3 OffsetFromHoldTypeAndHand(HandHoldType handHoldType, HandType selectedHand)
        {
            return _holdData.First(x => x.HandHoldType == handHoldType && x.PrimaryHand == selectedHand).Offset;
        }
    }
}
