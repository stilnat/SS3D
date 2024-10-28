using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Animations
{
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

        public Transform HitPoint => _hitPoint;

        public Vector3 ForwardHit => _forwardHit;

        public Vector3 UpHit => _upHit;

        private DropdownList<Vector3> GetDirectionValues()
        {
            return new DropdownList<Vector3>()
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
}
