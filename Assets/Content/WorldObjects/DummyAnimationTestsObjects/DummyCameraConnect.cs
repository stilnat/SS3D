using FishNet.Object;
using SS3D.Systems.Screens;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyCameraConnect : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                return;
            }

            FindObjectOfType<CameraFollow>().SetTarget(gameObject);
        }
    }
}
