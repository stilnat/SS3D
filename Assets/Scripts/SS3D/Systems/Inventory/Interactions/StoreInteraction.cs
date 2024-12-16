﻿using JetBrains.Annotations;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class StoreInteraction : IInteraction
    {
        private readonly AttachedContainer _attachedContainer;

        public StoreInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        [NotNull]
        public string GetName(InteractionEvent interactionEvent) => "Store in " + _attachedContainer.ContainerName;

        public string GetGenericName() => "Store";

        public InteractionType InteractionType => InteractionType.None;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Discard;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IInteractionSource source = interactionEvent.Source;
            if (source is not IGameObjectProvider sourceGameObjectProvider)
            {
                return false;
            }

            Hands hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
            if (!hands || !_attachedContainer)
            {
                return false;
            }

            Item item = interactionEvent.Source.GetComponent<Item>();
            if (!item)
            {
                return false;
            }
            
            return !hands.SelectedHand.IsEmpty() && CanStore(item, _attachedContainer);
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            return target.CanContainItem(item);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            if (source is IGameObjectProvider sourceGameObjectProvider)
            {
                Hands hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
                Item item = hands.SelectedHand.ItemInHand;

                hands.SelectedHand.Container.Dump();
                _attachedContainer.AddItem(item);
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
