using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace DummyStuff
{
    public class DummyItem : MonoBehaviour, IHoldProvider
    {
        [SerializeField]
        private HandHoldType singleHandHold;

        [SerializeField]
        private HandHoldType twoHandHold;

        [SerializeField]
        private HandHoldType singleHandHoldHarm;

        [SerializeField]
        private HandHoldType twoHandHoldHarm;

        [SerializeField]
        private HandHoldType singleHandHoldThrow;

        [SerializeField]
        private HandHoldType twoHandHoldThrow;

        [SerializeField]
        private Transform primaryRightHandHold;

        [SerializeField]
        private Transform secondaryRightHandHold;

        [SerializeField]
        private Transform primaryLeftHandHold;

        [SerializeField]
        private Transform secondaryLeftHandHold;

        [SerializeField]
        private bool canHoldTwoHand;

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent, bool toThrow)
        {
            switch (intent, withTwoHands)
            {
                case (Intent.Def, true):
                    return twoHandHold;
                case (Intent.Def, false):
                    return singleHandHold;
                case (Intent.Harm, true):
                    return toThrow ? twoHandHoldThrow : twoHandHoldHarm;
                case (Intent.Harm, false):
                    return toThrow ? singleHandHoldThrow : singleHandHoldHarm;
            }

            return singleHandHold;
        }

        public bool CanHoldTwoHand => canHoldTwoHand;

        [NotNull]
        public GameObject GameObject => gameObject;

        public Transform GetHold(bool primary, HandType handType)
        {
            switch (primary, handType)
            {
                case (true, HandType.LeftHand):
                    return primaryLeftHandHold;
                case (false, HandType.LeftHand):
                    return secondaryLeftHandHold;
                case (true, HandType.RightHand):
                    return primaryRightHandHold;
                case (false, HandType.RightHand):
                    return secondaryRightHandHold;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
