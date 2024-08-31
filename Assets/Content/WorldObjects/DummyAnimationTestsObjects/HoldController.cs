using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    /// <summary>
    /// Handle moving around the hold target lockers.
    /// </summary>
    public class HoldController : MonoBehaviour
    {
        private sealed record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, HandType PrimaryHand);

        private readonly List<HoldAndOffset> _holdData = new List<HoldAndOffset>();

        [FormerlySerializedAs("intents")]
        [SerializeField]
        private IntentController _intents;

        [FormerlySerializedAs("hands")]
        [SerializeField]
        private DummyHands _hands;

        [FormerlySerializedAs("pickup")]
        [SerializeField]
        private DummyPickUp _pickup;

        // Hold positions
        [FormerlySerializedAs("gunHoldRight")]
        [SerializeField]
        private Transform _gunHoldRight;

        [FormerlySerializedAs("gunHoldLeft")]
        [SerializeField]
        private Transform _gunHoldLeft;

        [FormerlySerializedAs("toolboxHoldRight")]
        [SerializeField]
        private Transform _toolboxHoldRight;

        [FormerlySerializedAs("toolBoxHoldLeft")]
        [SerializeField]
        private Transform _toolBoxHoldLeft;

        [FormerlySerializedAs("shoulderHoldRight")]
        [SerializeField]
        private Transform _shoulderHoldRight;

        [FormerlySerializedAs("shoulderHoldLeft")]
        [SerializeField]
        private Transform _shoulderHoldLeft;

        [FormerlySerializedAs("gunHoldHarmRight")]
        [SerializeField]
        private Transform _gunHoldHarmRight;

        [FormerlySerializedAs("gunHoldHarmLeft")]
        [SerializeField]
        private Transform _gunHoldHarmLeft;

        [FormerlySerializedAs("throwToolboxLeft")]
        [SerializeField]
        private Transform _throwToolboxLeft;

        [FormerlySerializedAs("throwToolboxRight")]
        [SerializeField]
        private Transform _throwToolboxRight;

        [FormerlySerializedAs("smallItemRight")]
        [SerializeField]
        private Transform _smallItemRight;

        [FormerlySerializedAs("smallItemLeft")]
        [SerializeField]
        private Transform _smallItemLeft;

        [FormerlySerializedAs("throwController")]
        [SerializeField]
        private DummyThrow _throwController;

        public void UpdateItemPositionConstraintAndRotation(
            DummyHand hand, IHoldProvider item, bool withTwoHands, float duration, bool toThrow)
        {
            if (item == null)
            {
                return;
            }

            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, _intents.Intent, toThrow);

            Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            Vector3 startingOffset = hand.ItemPositionConstraint.data.offset;
            Vector3 finalOffset = OffsetFromHoldTypeAndHand(itemHoldType, hand.HandType);

            StartCoroutine(CoroutineHelper.ModifyVector3OverTime(
                x => hand.ItemPositionConstraint.data.offset = x, startingOffset, finalOffset, duration));

            Quaternion startingRotation = hand.ItemPositionConstraint.data.constrainedObject.localRotation;
            Quaternion finalRotation = hold.localRotation;

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(
                x => hand.ItemPositionConstraint.data.constrainedObject.localRotation = x, startingRotation, finalRotation, duration));
        }

        public void MovePickupAndHoldTargetLocker(DummyHand hand, bool secondary, IHoldProvider holdProvider)
        {
            Transform parent = holdProvider.GetHold(!secondary, hand.HandType);

            hand.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);
            hand.SetParentTransformTargetLocker(TargetLockerType.Hold, parent);
        }

        protected void Start()
        {
            Debug.Log("start hold controller");

            _intents.OnIntentChange += HandleIntentChange;

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
        private void HandleIntentChange(object sender, Intent e)
        {
            DummyHand mainHand = _hands.SelectedHand;
            DummyHand secondaryHand = _hands.GetOtherHand(_hands.SelectedHand.HandType);

            if (mainHand.Full && secondaryHand.Empty && mainHand.Item.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(
                    mainHand, mainHand.Item, true, 0.25f, _throwController.IsAiming);
            }
            else if (mainHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(
                    mainHand, mainHand.Item, false, 0.25f, _throwController.IsAiming);
            }

            if (secondaryHand.Full && mainHand.Empty && secondaryHand.Item.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(
                    secondaryHand, secondaryHand.Item, true, 0.25f, _throwController.IsAiming);
            }
            else if (secondaryHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(
                    secondaryHand, secondaryHand.Item, false, 0.25f, _throwController.IsAiming);
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
