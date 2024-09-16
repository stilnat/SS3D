using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

    public interface IHoldProvider
    {
        public bool CanHoldTwoHand
        {
            get;
        }

        public GameObject GameObject
        {
            get;
        }

        public Transform GetHold(bool primary, HandType handType);

        public HandHoldType GetHoldType(bool withTwoHands, IntentType intent, bool toThrow);
    }
