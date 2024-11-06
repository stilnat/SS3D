using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Data.Generated;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Interaction to hit another player.
    /// </summary>
    public class HitInteraction : Interaction
    {

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Hit";
        }

        public override string GetGenericName()
        {
            return "Hit";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Nuke;
        }

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

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // Curently just hit the first body part of an entity if it finds one.
            // Should instead choose the body part using the target dummy doll ?
            // Also should be able to hit with other things than just hands.
            if (source.GetRootSource() is Hand hand)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Hit, hand, null, interactionEvent.Point, 0.5f);

                if (target is IGameObjectProvider targetBehaviour && targetBehaviour.GameObject.GetComponentInParent<Entity>() != null )
                {
                    Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
                    entity.GetComponent<Ragdoll>().KnockDown(2f);
                    //BodyPart bodyPart = entity.GetComponentInChildren<BodyPart>();

                    // Inflict a fix amount and type of damages for now. Long term, should be passed in parameter and depends on weapon type, velocity ...
                    //bodyPart.InflictDamageToAllLayer(new DamageTypeQuantity(DamageType.Slash, 50));
                }
            }

            return false;
        }
    }
}
