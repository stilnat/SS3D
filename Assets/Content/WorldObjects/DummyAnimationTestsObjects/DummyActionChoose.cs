using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyActionChoose : MonoBehaviour
    {
        [SerializeField]
        private DummyPickUp _pickup;

        [SerializeField]
        private DummyPlace _place;

        [SerializeField]
        private DummyHands _hands;

        protected void Update()
        {
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (_hands.SelectedHand.Empty)
            {
                    _pickup.TryPickUp();
            }
            else
            {
                       _place.TryPlace();
            }
        }
    }
}
