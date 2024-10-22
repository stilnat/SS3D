using FishNet.Object;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public interface IProceduralAnimation
    {

        public event Action<IProceduralAnimation> OnCompletion;

        public void ServerPerform(Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition,  ProceduralAnimationController proceduralAnimationController, float time, float delay);

        public void ClientPlay(Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay);

        public void Cancel();
    }
}
