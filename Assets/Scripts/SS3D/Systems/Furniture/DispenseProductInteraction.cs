﻿using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The interaction to dispense a product on a VendingMachine.
    /// </summary>
    public class DispenseProductInteraction : IInteraction
    {
        public string ProductName;
        public int ProductStock;
        public int ProductIndex;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        /// <inheritdoc />
        public string GetName(InteractionEvent interactionEvent)
        {
            return $"Dispense {ProductName} (x{ProductStock})";
        }

        public string GetGenericName() => "Dispense";

        public InteractionType InteractionType => InteractionType.Press;

        /// <inheritdoc />
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;

            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);
            if (!inRange)
            {
                return false;
            }

            return target is VendingMachine;
        }

        /// <inheritdoc />
        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Take;

        /// <inheritdoc />
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;

            if (target is VendingMachine vendingMachine)
            {
                vendingMachine.DispenseProduct(ProductIndex);
            }
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => throw new System.NotImplementedException();

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
