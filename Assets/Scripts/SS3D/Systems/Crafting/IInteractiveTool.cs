using FishNet.Object;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public interface IInteractiveTool : IPlayAnimation, IGameObjectProvider, INetworkObjectProvider
    {
        public Transform InteractionPoint { get; }

        public NetworkBehaviour NetworkBehaviour { get; }
    }
}
