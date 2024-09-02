using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummySittable : NetworkBehaviour
    {
        [FormerlySerializedAs("orientation")]
        [SerializeField]
        private Transform _orientation;

        public Transform Orientation => _orientation;
    }
}
