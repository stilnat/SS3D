using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public interface IHoldProvider
    {
        public Transform GetHold(bool primary, HandType handType);

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent, bool toThrow);

        public bool CanHoldTwoHand
        {
            get;
        }

        public GameObject GameObject
        {
            get;
        }
    }
}
