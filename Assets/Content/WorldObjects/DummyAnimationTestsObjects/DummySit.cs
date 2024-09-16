using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummySit : NetworkBehaviour
    {
        [FormerlySerializedAs("animatorController")]
        [SerializeField]
        private DummyAnimatorController _animatorController;

        [FormerlySerializedAs("movement")]
        [SerializeField]
        private DummyMovement _movement;

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
            if (!Input.GetKeyDown(KeyCode.J))
            {
                return;
            }

            if (GetComponent<DummyPositionController>().Position == PositionType.Standing && CanSit(out DummySittable sit))
            {
                RcpSit(sit);
            }
            else if (GetComponent<DummyPositionController>().Position == PositionType.Sitting)
            {
                RcpStopSitting();
            }
        }

        [ServerRpc]
        private void RcpSit(DummySittable sit)
        {
            ObserversSit(sit);
        }

        [ObserversRpc]
        private void ObserversSit(DummySittable sit)
        {
            StartCoroutine(Sit(sit.transform));
        }

        [ServerRpc]
        private void RcpStopSitting()
        {
             ObserversStopSitting();
        }

        [ServerRpc]
        private void ObserversStopSitting()
        {
            StopSitting();
        }

        private bool CanSit(out DummySittable sit)
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the collider belongs to a GameObject
                GameObject obj = hit.collider.gameObject;

                if (obj.TryGetComponent(out DummySittable sit2) && GoodDistanceFromRootToSit(sit2.transform))
                {
                    sit = sit2;

                    return true;
                }
            }

            sit = null;

            return false;
        }

        private IEnumerator Sit(Transform sitOrientation)
        {
            _movement.enabled = false;

            _animatorController.Sit(true);

            Vector3 initialRotation = transform.eulerAngles;

            Vector3 initialPosition = transform.position;

            StartCoroutine(CoroutineHelper.ModifyVector3OverTime(x => transform.eulerAngles = x, initialRotation, sitOrientation.eulerAngles, 0.5f));

            yield return CoroutineHelper.ModifyVector3OverTime(x => transform.position = x, initialPosition, sitOrientation.position, 0.5f);

            GetComponent<DummyPositionController>().Position = PositionType.Sitting;
        }

        private void StopSitting()
        {
            _movement.enabled = true;
            _animatorController.Sit(false);
            GetComponent<DummyPositionController>().Position = PositionType.Standing;
        }

        private bool GoodDistanceFromRootToSit(Transform sit)
        {
            return Vector3.Distance(transform.position, sit.position) < 0.8f;
        }
    }
}
