using FishNet.Object;
using SS3D.Data.Generated;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Interaction to hit another player.
    /// </summary>
    public class HitInteraction : DelayedInteraction
    {
        public HitInteraction(float time)
        {
            Delay = time;
        }

        public override InteractionType InteractionType => InteractionType.Hit;

        public override string GetName(InteractionEvent interactionEvent) => "Hit";

        public override string GetGenericName() => "Hit";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Nuke;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            return source.GetRootSource() is IIntentProvider hand && hand.Intent == IntentType.Harm;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && targetBehaviour.GameObject.GetComponentInParent<Entity>() != null)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();

                if (entity is IRagdollable ragdoll)
                {
                    ragdoll.Knockdown(1f);
                    ragdoll.AddForceToAllRagdollParts(-interactionEvent.Source.GameObject.transform.up);
                }
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // Curently just hit the first body part of an entity if it finds one.
            // Should instead choose the body part using the target dummy doll ?
            // Also should be able to hit with other things than just hands.
            if (source.GetRootSource() is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.PlaySourceAnimation(InteractionType.Hit, interactionEvent.Target.GetComponent<NetworkObject>(), interactionEvent.Point, Delay);
            }

            return true;
        }
    }
}
