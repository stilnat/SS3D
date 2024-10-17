using SS3D.Systems.Animations;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public abstract class Tool : MonoBehaviour, IPlayAnimation
    {
        [SerializeField]
        private Transform _interactionPoint;

        public Transform InteractionPoint => _interactionPoint;

        public abstract void PlayAnimation();

        public abstract void StopAnimation();
    }
}
