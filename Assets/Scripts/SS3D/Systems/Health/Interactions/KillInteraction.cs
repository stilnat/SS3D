using System;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.Containers;
using UnityEngine;

namespace SS3D.Systems.Health
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class KillInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Kill";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Discard);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // if the interaction source's parent is not a hand we return false
            if (interactionEvent.Source.GetRootSource() is not Hands)
            {
                return false;
            }

            // and we do a range check just in case
            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // we check if the source of the interaction is a hand
            if (interactionEvent.Source.GetRootSource() is Hands hands)
            {
                var entity = interactionEvent.Target.GetComponentInParent<Entity>();
                if(entity == null) 
                {
                    return false;
                }

                Debug.Log("Kill the target entity " + entity.Ckey);
            }

            return false;
        }
    }
}