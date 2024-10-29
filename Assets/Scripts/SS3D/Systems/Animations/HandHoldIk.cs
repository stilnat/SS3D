using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Crafting;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class HandHoldIk : NetworkActor, IInteractiveTool
    {
        [field: SerializeField]
        public Transform UpperArm { get; private set; }
         
        [field: SerializeField]
        public Transform HandHoldTargetLocker { get; private set; }

        [field: SerializeField]
        public Transform PickupTargetLocker { get; private set; }

        [field: SerializeField]
        public Transform PlaceTarget { get; private set; }

        [field: SerializeField]
        public Transform ItemPositionTargetLocker { get; private set; }

        [field: SerializeField]
        public Transform ShoulderWeaponPivot { get; private set; }

        [field: SerializeField]
        public TwoBoneIKConstraint HoldIkConstraint { get; private set; }

        [field: SerializeField]
        public ChainIKConstraint PickupIkConstraint { get; private set; }

        [field: SerializeField]
        public MultiPositionConstraint ItemPositionConstraint { get; private set; }

        [field: SerializeField]
        public Transform HandBone { get; private set; }

        [field: SerializeField]
        public Transform HoldTransform { get; private set; }

        [field: SerializeField]
        public Hands Hands { get; private set; }

        [field: SerializeField]
        public Hand Hand { get; private set; }

        [field: SerializeField]
        public Transform InteractionPoint { get; private set; }

        private Transform ChooseTargetLocker(TargetLockerType type)
        {
            Transform targetToSet = type switch
            {
                TargetLockerType.Pickup => PickupTargetLocker,
                TargetLockerType.Hold => HandHoldTargetLocker,
                TargetLockerType.ItemPosition => ItemPositionTargetLocker,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            return targetToSet;
        }

        public void SetParentTransformTargetLocker(TargetLockerType type, Transform parent, bool resetPosition = true, bool resetRotation = true)
        {
            Transform targetToSet = ChooseTargetLocker(type);
            targetToSet.parent = parent;
            if (resetPosition)
            {
                targetToSet.localPosition = Vector3.zero;
            }

            if (resetRotation)
            {
                targetToSet.localRotation = Quaternion.identity;
            }
        }

        public void StopHolding(Item item)
        {
            item.transform.parent = null;
            HoldIkConstraint.weight = 0f;
            Hands.TryGetOppositeHand(Hand, out Hand oppositeHand);
            bool withTwoHands = oppositeHand.Empty && item.Holdable.CanHoldTwoHand;

            if (withTwoHands)
            {
                oppositeHand.Hold.HoldIkConstraint.weight = 0f;
            }
        }

        public void PlayAnimation(InteractionType interactionType)
        {
            switch (interactionType)
            {
                case InteractionType.Open:
                    PlayOpenAnimation();
                    break;
            }
        }

        public void StopAnimation()
        {
            
        }

        private void PlayOpenAnimation()
        {
            // Define the points for the parabola
            Vector3[] path = new Vector3[] {
                HandBone.position,                             // Starting position
                HandBone.position + (HandBone.forward * 0.1f), // Peak (the highest point)
                HandBone.position + (HandBone.up * 0.1f),      // Final position
            };

            // Animate the GameObject along the path in a smooth parabolic motion
            PickupTargetLocker.DOPath(path, 0.1f, PathType.CatmullRom)
                .SetEase(Ease.Linear)           // You can adjust the ease function as needed
                .SetLoops(1, LoopType.Restart); // Play the animation once, no loops
        }
    }
}
