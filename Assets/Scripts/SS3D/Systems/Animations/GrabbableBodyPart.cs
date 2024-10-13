using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class GrabbableBodyPart : NetworkBehaviour, IInteractionTarget
    {

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new GrabInteraction(Entities.Data.Animations.Humanoid.PickupReachTime, Entities.Data.Animations.Humanoid.PickupMoveItemTime),
            };
        }
    }
}
