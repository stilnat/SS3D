using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Component used for item hit animation, it holds data about where the hit should occur, and how the item should be rotated upon hit.
    /// </summary>
    public class ItemHitPoint : MonoBehaviour
    {

        [SerializeField]
        [Dropdown(nameof(GetDirectionValues))]
        private Vector3 _forwardHit = Vector3.forward;

        [SerializeField]
        [Dropdown(nameof(GetDirectionValues))]
        private Vector3 _upHit = Vector3.up;

        [SerializeField]
        private Transform _hitPoint;

        /// <summary>
        /// The position of the hit.
        /// </summary>
        public Transform HitPoint => _hitPoint;

        /// <summary>
        /// The forward direction at the time of the hit, which axis of the item is aligned with the forward direction of the hit
        /// </summary>
        public Vector3 ForwardHit => _forwardHit;

        /// <summary>
        /// The up direction at the time of the hit, which axis of the item is aligned with the up direction of the hit
        /// </summary>
        public Vector3 UpHit => _upHit;

        [NotNull]
        private DropdownList<Vector3> GetDirectionValues() => new()
        {
            { "Right",   Vector3.right },
            { "Left",    Vector3.left },
            { "Up",      Vector3.up },
            { "Down",    Vector3.down },
            { "Forward", Vector3.forward },
            { "Back",    Vector3.back },
        };
    }
}
