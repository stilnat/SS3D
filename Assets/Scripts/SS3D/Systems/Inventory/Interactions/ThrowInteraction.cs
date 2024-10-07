using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ThrowInteraction : Interaction
    {
        public override string GetGenericName()
        {
            return "Throw";
        }

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Throw";
        }

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;;
        }

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if(interactionEvent.Source is not IGameObjectProvider source)
            {
                return false;
            }
            
            ThrowAnimations throwAnimations = source.GameObject.GetComponentInParent<ThrowAnimations>();

            if (!throwAnimations)
            {
                return false;
            }

            if (!throwAnimations.IsAiming)
            {
                return false;
            }

            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not Hand hand)
            {
                return false;
            }

            if (hand.Empty)
            {
                return false;
            }

            return true;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if(interactionEvent.Source is not IGameObjectProvider source)
            {
                return false;
            }
            
            ThrowAnimations throwAnimations = source.GameObject.GetComponentInParent<ThrowAnimations>();

            throwAnimations.ThrowAnimate();

            return false;
        }
    }
}
