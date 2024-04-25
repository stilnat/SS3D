using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace DummyStuff
{
    /// <summary>
    /// Handle moving around the hold target lockers.
    /// </summary>
    public class HoldController : MonoBehaviour
    {
        public IntentController intents;

        public DummyHands hands;

        public DummyPickUp pickup;

        // Hold positions

        public Transform gunHoldRight;

        public Transform gunHoldLeft;

        public Transform toolboxHoldRight;

        public Transform toolBoxHoldLeft;

        public Transform shoulderHoldRight;

        public Transform shoulderHoldLeft;

        public Transform gunHoldHarmRight;

        public Transform gunHoldHarmLeft;

        public Transform throwToolboxLeft;

        public Transform throwToolboxRight;

        public Transform smallItemRight;

        public Transform smallItemLeft;

        private sealed record HoldAndOffset(HandHoldType HandHoldType, Transform HoldTarget, Vector3 Offset, HandType PrimaryHand);

        private readonly List<HoldAndOffset> _holdData = new List<HoldAndOffset>();

        [SerializeField]
        private DummyThrow throwController;

        private void Start()
        {
            Debug.Log("start hold controller");

            intents.OnIntentChange += HandleIntentChange;

            _holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldLeft, new Vector3(0.15f, -0.08f, 0.26f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGun, gunHoldRight, new Vector3(-0.15f, -0.08f, 0.26f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.Toolbox, toolBoxHoldLeft, new Vector3(-0.1f, -0.4f, 0.1f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Toolbox, toolboxHoldRight, new Vector3(0.1f, -0.4f, 0.1f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.Shoulder, shoulderHoldLeft, new Vector3(0f, 0.18f, 0f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.Shoulder, shoulderHoldRight, new Vector3(0f, 0.18f, 0f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmLeft, new Vector3(0f, -0.07f, 0.18f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.DoubleHandGunHarm, gunHoldHarmRight, new Vector3(0f, -0.07f, 0.18f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.SmallItem, smallItemLeft, new Vector3(0f, -0.35f, 0.25f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.SmallItem, smallItemRight, new Vector3(0f, -0.35f, 0.25f), HandType.RightHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, throwToolboxLeft, new Vector3(-0.23f, 0.3f, -0.03f), HandType.LeftHand));
            _holdData.Add(new(HandHoldType.ThrowToolBox, throwToolboxRight, new Vector3(0.23f, 0.3f, -0.03f), HandType.RightHand));
        }

        // TODO : add a handle for selected hand change, and update only visually the intent for selected hand
        private void HandleIntentChange(object sender, Intent e)
        {
            DummyHand mainHand = hands.SelectedHand;
            DummyHand secondaryHand = hands.GetOtherHand(hands.SelectedHand.handType);

            if (mainHand.Full && secondaryHand.Empty && mainHand.Item.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(mainHand, mainHand.Item,
                    true, 0.25f, throwController.IsAiming);
            }
            else if (mainHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(mainHand,mainHand.Item,
                    false, 0.25f, throwController.IsAiming);
            }


            if (secondaryHand.Full && mainHand.Empty && secondaryHand.Item.CanHoldTwoHand)
            {
                UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.Item,
                    true, 0.25f, throwController.IsAiming);
            }
            else if (secondaryHand.Full)
            {
                UpdateItemPositionConstraintAndRotation(secondaryHand, secondaryHand.Item,
                    false, 0.25f, throwController.IsAiming);

            }

        }

        public void UpdateItemPositionConstraintAndRotation(DummyHand hand, IHoldProvider item, bool withTwoHands,
            float duration, bool toThrow)
        {
            if (item == null)
                return;

            HandHoldType itemHoldType = item.GetHoldType(withTwoHands, intents.Intent, toThrow);

            Transform hold = TargetFromHoldTypeAndHand(itemHoldType, hand.handType);

            Vector3 startingOffset = hand.itemPositionConstraint.data.offset;
            Vector3 finalOffset = OffsetFromHoldTypeAndHand(itemHoldType, hand.handType);

            StartCoroutine(CoroutineHelper.ModifyVector3OverTime(x => hand.itemPositionConstraint.data.offset = x, startingOffset, finalOffset, duration));

            Quaternion startingRotation = hand.itemPositionConstraint.data.constrainedObject.localRotation;
            Quaternion finalRotation = hold.localRotation;

            StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(x => hand.itemPositionConstraint.data.constrainedObject.localRotation = x, startingRotation, finalRotation, duration));
        }


        public void MovePickupAndHoldTargetLocker(DummyHand hand, bool secondary, IHoldProvider holdProvider)
        {
            Transform parent = holdProvider.GetHold(!secondary, hand.handType);

            hand.SetParentTransformTargetLocker(TargetLockerType.Pickup, parent);
            hand.SetParentTransformTargetLocker(TargetLockerType.Hold, parent);
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
