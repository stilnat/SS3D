using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyTool : MonoBehaviour
    {
        [FormerlySerializedAs("interactionPoint")]
        [SerializeField]
        private Transform _interactionPoint;

        public Transform InteractionPoint => _interactionPoint;
    }
}
