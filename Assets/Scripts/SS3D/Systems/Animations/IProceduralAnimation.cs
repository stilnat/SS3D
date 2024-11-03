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
        public void ClientPlay();

        /// <summary>
        /// Cancel the playing animation and return player to its state before the animation started playing.
        /// </summary>
        public void Cancel();
    }
}
