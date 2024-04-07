using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyItem : MonoBehaviour, IHoldProvider
    {
        public HandHoldType singleHandHold;

        public HandHoldType twoHandHold;

        public HandHoldType singleHandHoldHarm;

        public HandHoldType twoHandHoldHarm;

        public HandHoldType singleHandHoldThrow;

        public HandHoldType twoHandHoldThrow;

        public Transform primaryRightHandHold;

        public Transform secondaryRightHandHold;

        public Transform primaryLeftHandHold;

        public Transform secondaryLeftHandHold;

        [SerializeField]
        private bool canHoldTwoHand;

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent)
        {
            switch (intent, withTwoHands)
            {
                case (Intent.Def, true):
                    return twoHandHold;
                case (Intent.Def, false):
                    return singleHandHold;
                case (Intent.Harm, true):
                    return twoHandHoldHarm;
                case (Intent.Harm, false):
                    return singleHandHoldHarm;
            }

            Debug.LogError("case not handled");

            return singleHandHold;
        }

        public bool CanHoldTwoHand => canHoldTwoHand;

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

        public HandHoldType GetHoldThrowType(bool withTwoHands)
        {
            switch (withTwoHands)
            {
                case true:
                    return twoHandHoldThrow;
                case false:
                    return singleHandHoldThrow;
            }
        }
    }
}
