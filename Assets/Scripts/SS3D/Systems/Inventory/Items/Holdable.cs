using JetBrains.Annotations;
using NaughtyAttributes;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public class Holdable : MonoBehaviour,  IHoldProvider
    {
        [SerializeField]
        private bool _canHoldTwoHand;

        [SerializeField]
        private HandHoldType _singleHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHold;

        [SerializeField]
        private HandHoldType _singleHandHoldHarm;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldHarm;

        [SerializeField]
        private HandHoldType _singleHandHoldThrow;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldThrow;

        [SerializeField]
        private Transform _primaryRightHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryRightHandHold;

        [SerializeField]
        private Transform _primaryLeftHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryLeftHandHold;

        public bool CanHoldTwoHand => _canHoldTwoHand;

        [NotNull]
        public GameObject GameObject => gameObject;

        public HandHoldType GetHoldType(bool withTwoHands, IntentType intent, bool toThrow)
        {
            switch (intent, withTwoHands)
            {
                case (IntentType.Help, true):
                    return _twoHandHold;
                case (IntentType.Help, false):
                    return _singleHandHold;
                case (IntentType.Harm, true):
                    return toThrow ? _twoHandHoldThrow : _twoHandHoldHarm;
                case (IntentType.Harm, false):
                    return toThrow ? _singleHandHoldThrow : _singleHandHoldHarm;
            }

            return _singleHandHold;
        }

        public Transform GetHold(bool primary, HandType handType)
        {
            switch (primary, handType)
            {
                case (true, HandType.LeftHand):
                    return _primaryLeftHandHold;
                case (false, HandType.LeftHand):
                    return _secondaryLeftHandHold;
                case (true, HandType.RightHand):
                    return _primaryRightHandHold;
                case (false, HandType.RightHand):
                    return _secondaryRightHandHold;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
