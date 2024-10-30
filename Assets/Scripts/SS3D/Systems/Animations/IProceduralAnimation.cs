using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public interface IProceduralAnimation
    {

        /// <summary>
        /// Event that should be called when the animation ends.
        /// </summary>
        public event Action<IProceduralAnimation> OnCompletion;

        /// <summary>
        /// Play the animation, this method should be executed on client.
        /// </summary>
        /// <param name="interactionType"> Type of the interaction, useful for some animations that can adapt to it to display differently.</param>
        /// <param name="mainHand"> The main hand involved in the interaction, can be null if not relevant.</param>
        /// <param name="secondaryHand">The secondary hand involved in the interaction, can be null if not relevant.</param>
        /// <param name="target"> The interaction target, can be null too if not relevant.</param>
        /// <param name="targetPosition"> The target position of the interaction, for instance, to pick up, would be the hold on the item.</param>
        /// <param name="proceduralAnimationController"> The procedural animation controller, that gives access to other components procedural animations often need.</param>
        /// <param name="time"> The time the animation should take.</param>
        /// <param name="delay"> An eventual delay before playing the animation.</param>
        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay);

        /// <summary>
        /// Cancel the playing animation and return player to its state before the animation started playing.
        /// </summary>
        public void Cancel();
    }
}
