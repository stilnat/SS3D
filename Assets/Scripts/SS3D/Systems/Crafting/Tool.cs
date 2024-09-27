using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class Tool : MonoBehaviour
    {
        [SerializeField]
        private Transform _interactionPoint;

        public Transform InteractionPoint => _interactionPoint;
    }
}
