using UnityEngine;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Animations;
using System;
using System.Collections;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A hand is what an entity uses to grab and hold items, to interact with things in range.
    /// </summary>
    public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider
    {
        [SerializeField]
        private Transform _handHoldTargetLocker;

        [SerializeField]
        private Transform _pickupTargetLocker;

        [SerializeField]
        private Transform _placeTarget;

        [SerializeField]
        private Transform _itemPositionTargetLocker;

        [SerializeField]
        private Transform _shoulderWeaponPivot;

        [SerializeField]
        private TwoBoneIKConstraint _holdIkConstraint;

        [SerializeField]
        private ChainIKConstraint _pickupIkConstraint;

        [SerializeField]
        private MultiPositionConstraint _itemPositionConstraint;

        [SerializeField]
        private Transform _upperArm;

        [SerializeField]
        private Transform _handBone;

        [SerializeField]
        private Transform _holdTransform;

        [SyncVar]
        public bool IsGrabbing;

        public Transform PickupTargetLocker => _pickupTargetLocker;

        public Transform PlaceTarget => _placeTarget;

        public Transform ItemPositionTargetLocker => _itemPositionTargetLocker;

        public Transform ShoulderWeaponPivot => _shoulderWeaponPivot;

        public TwoBoneIKConstraint HoldIkConstraint => _holdIkConstraint;

        public ChainIKConstraint PickupIkConstraint => _pickupIkConstraint;

        public MultiPositionConstraint ItemPositionConstraint => _itemPositionConstraint;

        public Transform UpperArm => _upperArm;

        public Transform HandBone => _handBone;

        public Transform HoldTransform => _holdTransform;

        public bool Empty => Container.Empty;

        public bool Full => !Container.Empty;

        /// <summary>
        /// Container linked to this hand, necessary to hold stuff.
        /// </summary>
        public AttachedContainer Container;

        /// <summary>
        /// Horizontal and vertical max distance to interact with stuff.
        /// </summary>
        [SerializeField] private RangeLimit _range = new(0.5f, 2);

        // pickup icon that this hand uses when there's a pickup interaction
        // TODO: When AssetData is on, we should update this to not use this
        [SerializeField] private Sprite _pickupIcon;

        /// <summary>
        /// The item held in this hand, if it exists
        /// </summary>
        public Item ItemInHand => Container.Items.FirstOrDefault();

        /// <summary>
        /// Point from where distances for interaction is computed.
        /// </summary>
        [SerializeField] private Transform _interactionOrigin;

        /// <summary>
        /// the hands script controlling this hand.
        /// </summary>
        public Hands HandsController;

        public Vector3 InteractionOrigin => _interactionOrigin.position;

        [SerializeField]
        private HandType _handType;

        public HandType HandType => _handType;

        public delegate void HandEventHandler(Hand hand);

        public event HandEventHandler OnHandDisabled;

        public void Awake()
        {
            // should only be called on server, however it's too late if listening in OnStartServer (again, issue with initialization timing of our systems...)
            Container.OnContentsChanged += ContainerOnOnContentsChanged;
        }

        private void ContainerOnOnContentsChanged(AttachedContainer container, Item olditem, Item newitem, ContainerChangeType type)
        {

            if (GetComponentInParent<PickUpAnimation>().IsPicking)
            {
                return;
            }

            if (type == ContainerChangeType.Add && IsServer)
            {
                // Delay necessary otherwise item holdable is not set ... messy.
                GetComponentInParent<PickUpAnimation>().Pickup(newitem, 0f, 0f, 0.1f);
            }

            if (type == ContainerChangeType.Remove)
            {
                
                StopHolding(olditem);
            }
        }



        protected override void OnDisabled()
        {
            if (!IsServer)
            {
                return;
            }

            OnHandDisabled?.Invoke(this);
        }

		public bool IsEmpty()
		{
			return Container.Empty;
		}

        /// <summary>
        /// Get the interaction source from stuff in hand if there's any.
        /// Also sets the source of the IInteraction source to be this hand.
        /// </summary>
        /// <returns></returns>
        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemInHand;
            if (itemInHand == null)
            {
                return null;
            }

            IInteractionSource interactionSource = itemInHand.GetComponent<IInteractionSource>();
            if (interactionSource != null)
            {
                interactionSource.Source = this;
            }
            return interactionSource;
        }

        public RangeLimit GetInteractionRange()
        {
            return _range;
        }

        [ServerRpc]
        public void CmdDropHeldItem()
        {
            Container.Dump();
		}

        private void StopHolding(Item item)
        {
            item.transform.parent = null;
            _holdIkConstraint.weight = 0f;
            HandsController.TryGetOppositeHand(this, out Hand oppositeHand);
            bool withTwoHands = oppositeHand.Empty && item.Holdable.CanHoldTwoHand;

            if (withTwoHands)
            {
                oppositeHand._holdIkConstraint.weight = 0f;
            }
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
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

        private Transform ChooseTargetLocker(TargetLockerType type)
        {
            Transform targetToSet = type switch
            {
                TargetLockerType.Pickup => _pickupTargetLocker,
                TargetLockerType.Hold => _handHoldTargetLocker,
                TargetLockerType.ItemPosition => _itemPositionTargetLocker,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            return targetToSet;
        }
    }
}
