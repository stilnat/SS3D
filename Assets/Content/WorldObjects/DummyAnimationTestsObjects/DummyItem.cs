using FishNet.Object;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyItem : NetworkBehaviour, IHoldProvider
    {
        [FormerlySerializedAs("singleHandHold")]
        [SerializeField]
        private HandHoldType _singleHandHold;

        [FormerlySerializedAs("twoHandHold")]
        [SerializeField]
        private HandHoldType _twoHandHold;

        [FormerlySerializedAs("singleHandHoldHarm")]
        [SerializeField]
        private HandHoldType _singleHandHoldHarm;

        [FormerlySerializedAs("twoHandHoldHarm")]
        [SerializeField]
        private HandHoldType _twoHandHoldHarm;

        [FormerlySerializedAs("singleHandHoldThrow")]
        [SerializeField]
        private HandHoldType _singleHandHoldThrow;

        [FormerlySerializedAs("twoHandHoldThrow")]
        [SerializeField]
        private HandHoldType _twoHandHoldThrow;

        [FormerlySerializedAs("primaryRightHandHold")]
        [SerializeField]
        private Transform _primaryRightHandHold;

        [FormerlySerializedAs("secondaryRightHandHold")]
        [SerializeField]
        private Transform _secondaryRightHandHold;

        [FormerlySerializedAs("primaryLeftHandHold")]
        [SerializeField]
        private Transform _primaryLeftHandHold;

        [FormerlySerializedAs("secondaryLeftHandHold")]
        [SerializeField]
        private Transform _secondaryLeftHandHold;

        [FormerlySerializedAs("canHoldTwoHand")]
        [SerializeField]
        private bool _canHoldTwoHand;

        public bool CanHoldTwoHand => _canHoldTwoHand;

        [NotNull]
        public GameObject GameObject => gameObject;

        public HandHoldType GetHoldType(bool withTwoHands, Intent intent, bool toThrow)
        {
            switch (intent, withTwoHands)
            {
                case (Intent.Def, true):
                    return _twoHandHold;
                case (Intent.Def, false):
                    return _singleHandHold;
                case (Intent.Harm, true):
                    return toThrow ? _twoHandHoldThrow : _twoHandHoldHarm;
                case (Intent.Harm, false):
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
