﻿using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Interaction to change music on Jukeboxes and boomboxes.
    /// </summary>
    public class ChangeMusicInteraction : DelayedInteraction
    {
    
        public override InteractionType InteractionType => InteractionType.Press;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Change Music";
        }

        public override string GetGenericName()
        {
            return "Change Music";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
                return boom.InteractionIcon;
            return null;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }
                return boom.AudioOn;
            }

            return false;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is Boombox boom)
            {
                boom.ChangeCurrentMusic();
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source as Hand;

            Vector3 point = interactionEvent.Point;

            if (interactionEvent.Target.TryGetInteractionPoint(interactionEvent.Source, out Vector3 customPoint))
            {
                point = customPoint;
            }

            if (hand != null)
            {
                interactionEvent.Source.GameObject.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType, hand, null, point, Delay);
            }
            
            return true;
        }
    }
}
