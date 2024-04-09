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

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent, bool toThrow)
        {
            return handHold;
        }

        public bool CanHoldTwoHand => true;

        public GameObject GameObject => gameObject;
    }
}
