using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class GrabbableBodyPart : NetworkBehaviour, IHoldProvider
    {
        [FormerlySerializedAs("handHold")]
        [SerializeField]
        private HandHoldType _handHold = HandHoldType.DoubleHandGun;

        public bool CanHoldTwoHand => true;

        public GameObject GameObject => gameObject;

        public Transform GetHold(bool primary, HandType handType)
        {
            return transform;
        }

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent, bool toThrow)
        {
            return _handHold;
        }
    }
}
