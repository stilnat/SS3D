using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Furniture;
using SS3D.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Animations
{
    public class Sit : NetworkBehaviour
    {

        [SerializeField]
        private HumanoidAnimatorController _animatorController;

        [SerializeField]
        private HumanoidController _movement;

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

            if (GetComponent<PositionController>().Position == PositionType.Standing && CanSit(out Sittable sit))
            {
                RcpSit(sit);
            }
            else if (GetComponent<PositionController>().Position == PositionType.Sitting)
            {
                RcpStopSitting();
            }
        }

        [ServerRpc]
        private void RcpSit(Sittable sit)
        {
            ObserversSit(sit);
        }

        [ObserversRpc]
        private void ObserversSit(Sittable sit)
        {
            StartCoroutine(AnimateSit(sit.transform));
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

        private bool CanSit(out Sittable sit)
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the collider belongs to a GameObject
                GameObject obj = hit.collider.gameObject;

                if (obj.TryGetComponent(out Sittable sit2) && GoodDistanceFromRootToSit(sit2.transform))
                {
                    sit = sit2;

                    return true;
                }
            }

            sit = null;

            return false;
        }

        private IEnumerator AnimateSit(Transform sitOrientation)
        {
            _movement.enabled = false;

            _animatorController.Sit(true);

            Vector3 initialRotation = transform.eulerAngles;

            Vector3 initialPosition = transform.position;

            StartCoroutine(CoroutineHelper.ModifyVector3OverTime(x => transform.eulerAngles = x, initialRotation, sitOrientation.eulerAngles, 0.5f));

            yield return CoroutineHelper.ModifyVector3OverTime(x => transform.position = x, initialPosition, sitOrientation.position, 0.5f);

            GetComponent<PositionController>().Position = PositionType.Sitting;
        }

        private void StopSitting()
        {
            _movement.enabled = true;
            _animatorController.Sit(false);
            GetComponent<PositionController>().Position = PositionType.Standing;
        }

        private bool GoodDistanceFromRootToSit(Transform sit)
        {
            return Vector3.Distance(transform.position, sit.position) < 0.8f;
        }
    }
}
