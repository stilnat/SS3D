using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSource, IInteractionTarget
    {

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            // Ends the round, regardless of how many objectives were completed
            Subsystems.Get<GamemodeSystem>().EndRound();
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new NukeDetonateInteraction { Icon = InteractionIcons.Nuke } };
        }
    }
}
