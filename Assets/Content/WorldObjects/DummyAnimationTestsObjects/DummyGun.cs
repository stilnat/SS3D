using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyGun : NetworkBehaviour
    {
        [FormerlySerializedAs("rifleButt")]
        [SerializeField]
        private Transform _rifleButt;

        public Transform RifleButt => _rifleButt;
    }
}
