using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class GrabbableBodyPart : MonoBehaviour, IHoldProvider
    {
        public HandHoldType handHold = HandHoldType.DoubleHandGun;
        
        public Transform GetHold(bool primary, HandType handType)
        {
            return transform;
        }

        public HandHoldType GetHoldThrowType(bool withTwoHands)
        {
            return handHold;
        }

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent)
        {
            return handHold;
        }

        public bool CanHoldTwoHand => true;

        public GameObject GameObject => gameObject;
    }
}
