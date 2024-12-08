using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;





namespace SS3D.Systems.Inventory.Interactions
{
    public class DropInteraction : Interaction
    {
        public override string GetGenericName()
        {
            return "Drop";
        }

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Drop";
        }

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIcons.Discard;
        }

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source.GetRootSource() is not Hand)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;
            hand.ItemInHand?.GiveOwnership(null);
            hand.Container.Dump();
            return false;
        }
    }
}
