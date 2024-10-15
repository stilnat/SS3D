using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

    public interface IHoldProvider : IGameObjectProvider
    {
        public bool CanHoldTwoHand { get; }

        public FingerPoseType PrimaryHandPoseType { get; }

        public FingerPoseType SecondaryHandPoseType { get; }

        public HandHoldType SingleHandHold { get; }

        public HandHoldType TwoHandHold { get; }

        public HandHoldType SingleHandHoldHarm { get; }

        public HandHoldType TwoHandHoldHarm { get; }

        public HandHoldType SingleHandHoldThrow { get; }

        public HandHoldType TwoHandHoldThrow { get; }

        public Transform GetHold(bool primary, HandType handType);

        public HandHoldType GetHoldType(bool withTwoHands, IntentType intent, bool toThrow);
    }
