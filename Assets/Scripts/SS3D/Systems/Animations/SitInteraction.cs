using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Interaction to allow sitting
    /// </summary>
    public sealed class SitInteraction : Interaction
    {

        public float TimeToSit{ get; private set; }

        public override InteractionType InteractionType => InteractionType.Sit;

        public SitInteraction(float timeToSit)
        {
            TimeToSit = timeToSit;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Sit";
        }

        public override string GetGenericName() => "Sit";

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIcons.Discard;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!interactionEvent.Target.GetGameObject().TryGetComponent(out Sittable sit))
            {
                return false;
            }

            if (!GoodDistanceFromRootToSit(sit.transform, interactionEvent.Source.GameObject.transform))
            {
                return false;
            }

            return true;
        }

        private bool GoodDistanceFromRootToSit(Transform sit, Transform playerRoot)
        {
            return Vector3.Distance(playerRoot.position, sit.position) < 2f;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;

            if (interactionEvent.Target is not Sittable sit || !hand)
            {
                return false;
            }

            hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Sit, hand, sit, interactionEvent.Point, TimeToSit);
            
            return false;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("attempting to cancel animation");

            if (interactionEvent.Source.GetRootSource() is Hand hand)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);
            }
        }
    }
}
