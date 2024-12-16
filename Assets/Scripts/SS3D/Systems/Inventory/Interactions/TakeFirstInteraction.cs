using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : IInteraction
    {
        private readonly AttachedContainer _attachedContainer;

        public float TimeToMoveBackItem { get; private set; }

        public float TimeToReachItem { get; private set; }

        public TakeFirstInteraction(AttachedContainer attachedContainer, float timeToMoveBackItem, float timeToReachItem)
        {
            TimeToMoveBackItem = timeToMoveBackItem;
            TimeToReachItem = timeToReachItem;
            _attachedContainer = attachedContainer;
        }

        public string GetGenericName() => "Take";

        public InteractionType InteractionType => InteractionType.None;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => throw new System.NotImplementedException();

        public string GetName(InteractionEvent interactionEvent) => "Take in " + _attachedContainer.ContainerName;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Take;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Will only appear if the current hand is empty and the container isn't empty
            if (interactionEvent.Source is Hand hand && _attachedContainer != null)
            {
                return hand.IsEmpty() && !_attachedContainer.Empty;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = (Hand) interactionEvent.Source;

            Item pickupItem = _attachedContainer.Items.First();

            if (pickupItem != null)
            {
                //hand.Pickup(pickupItem, TimeToMoveBackItem, TimeToReachItem);
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
