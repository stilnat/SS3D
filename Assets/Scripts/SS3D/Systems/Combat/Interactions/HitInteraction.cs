using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Data.Generated;
using SS3D.Intents;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;

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

        public override string GetName(InteractionEvent interactionEvent) => "Hit";

        public override string GetGenericName() => "Hit";

        public override InteractionType InteractionType => InteractionType.Hit;

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Nuke;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (source.GetRootSource() is not Hand hand)
            {
                return false;
            }

            if (hand.GetComponentInParent<IntentController>().Intent != IntentType.Harm)
            {
                return false;
            }

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && targetBehaviour.GameObject.GetComponentInParent<Entity>() != null && source.GetRootSource() is Hand hand)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
                entity.GetComponent<Ragdoll>().KnockDown(1f);
                entity.GetComponent<Ragdoll>().AddForceToAllParts(-hand.Up);
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // Curently just hit the first body part of an entity if it finds one.
            // Should instead choose the body part using the target dummy doll ?
            // Also should be able to hit with other things than just hands.
            if (source.GetRootSource() is Hand hand)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Hit, hand, null, interactionEvent.Point, Delay);
            }

            return true;
        }
    }
}
