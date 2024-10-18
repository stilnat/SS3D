using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public interface IInteractiveTool : IPlayAnimation, IGameObjectProvider
    {
        public Transform InteractionPoint { get; }
    }
}
