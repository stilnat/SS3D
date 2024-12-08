using DG.Tweening;
using UnityEngine;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Animations;
using SS3D.Systems.Crafting;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A hand is what an entity uses to grab and hold items, to interact with things in range.
    /// </summary>
    public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider
    {
        public delegate void HandEventHandler(Hand hand);

        public delegate void HandContentsHandler(Hand hand, Item oldItem, Item newItem, ContainerChangeType type);

        public event HandEventHandler OnHandDisabled;

        public event HandContentsHandler OnContentsChanged;

        /// <summary>
        /// the hands script controlling this hand.
        /// </summary>
        public Hands HandsController;

        /// <summary>
        /// Container linked to this hand, necessary to hold stuff.
        /// </summary>
        public AttachedContainer Container;

        /// <summary>
        /// Point from where distances for interaction is computed.
        /// </summary>
        [SerializeField] 
        private Transform _interactionOrigin;

        [SerializeField]
        private Transform _interactionPoint;

        /// <summary>
        /// Horizontal and vertical max distance to interact with stuff.
        /// </summary>
        [SerializeField] 
        private RangeLimit _range = new(0.7f, 2);

        public bool Empty => Container.Empty;

        public bool Full => !Container.Empty;

        /// <summary>
        /// The item held in this hand, if it exists
        /// </summary>
        public Item ItemInHand => Container.Items.FirstOrDefault();

        public Vector3 InteractionOrigin => _interactionOrigin.position;


        [field: SerializeField]
        public HandType HandType { get; private set; }

        [field:SerializeField]
        public Transform HandBone { get; private set; }

        [field:SerializeField]
        public HandHoldIk Hold { get; private set; }


        public void Awake()
        {
            // should only be called on server, however it's too late if listening in OnStartServer (again, issue with initialization timing of our systems...)
            Container.OnContentsChanged += ContainerOnOnContentsChanged;
        }

        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries)
        {
            // todo : hands should not handle sit interactions, ass should, but the interaction controller needs some changes to handle interaction sources other than hands
            base.CreateSourceInteractions(targets, entries);
            IInteractionTarget target = targets.FirstOrDefault(x => x?.GameObject.GetComponent<Sittable>());

            if (target is Sittable)
            {
                entries.Add(new InteractionEntry(target, new SitInteraction(1f)));
            }
            
        }

        private void ContainerOnOnContentsChanged(AttachedContainer container, Item olditem, Item newitem, ContainerChangeType type)
        {
            if (type == ContainerChangeType.Remove && olditem != null)
            {
                GetComponentInParent<HumanoidAnimatorController>()?.RemoveHandHolding(this, olditem.Holdable);
                StopHolding(olditem);
            }

            if (type == ContainerChangeType.Add && newitem.Holdable != null)
            {
                GetComponentInParent<HumanoidAnimatorController>()?.AddHandHolding(this, newitem.Holdable);
            }

            OnContentsChanged?.Invoke(this, olditem, newitem, type);
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
            DropHeldItem();
        }

        [Server]
        public void DropHeldItem()
        {
            Container.Dump();
        }

        private void StopHolding(Item item)
        {
            item.transform.parent = null;
            Hold?.StopHolding(item);
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }
        public Transform InteractionPoint => _interactionPoint;
    }
}
