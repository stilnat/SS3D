using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyActionChoose : NetworkBehaviour
    {
        [SerializeField]
        private DummyPickUp _pickup;

        [SerializeField]
        private DummyPlace _place;

        [SerializeField]
        private DummyHands _hands;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }
        }

        protected void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (_hands.SelectedHand.Empty)
            {
                if (_pickup.CanPickUp(out DummyItem item))
                {
                    RpcTryPickUp(item);
                }
            }
            else if (_place.CanPlace(out Vector3 point))
            {
                RpcTryPlace(point);
            }
        }

        [ServerRpc]
        private void RpcTryPickUp(DummyItem item)
        {
            ObserversTryPickUp(item);
        }

        [ServerRpc]
        private void RpcTryPlace(Vector3 point)
        {
            ObserversTryPlace(point);
        }

        [ObserversRpc]
        private void ObserversTryPickUp(DummyItem item)
        {
            StartCoroutine(_pickup.PickUp(item));
        }

        [ObserversRpc]
        private void ObserversTryPlace(Vector3 point)
        {
            StartCoroutine(_place.Place(point));
        }
    }
}
