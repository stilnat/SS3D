using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public interface IHoldProvider
    {
        public Transform GetHold(bool primary, HandType handType);

        public HandHoldType GetHoldThrowType(bool withTwoHands);

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent);
    }
}
