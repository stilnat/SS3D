using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using CoroutineHelper = SS3D.Utils.CoroutineHelper;

namespace SS3D.Systems.Animations
{
    public class GrabAnimation : NetworkBehaviour
    {
        public event EventHandler<bool> OnGrab;

        private FixedJoint _fixedJoint;


        private float _jointBreakForce = 25000f;


        private HoldController _holdController;

        [SerializeField]
        private Hands _hands;

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
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (CanGrab(out GrabbableBodyPart bodyPart))
                {
                    GrabObject(bodyPart);
                }
            }
        }



        private bool CanGrab(out GrabbableBodyPart bodyPart)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);


            if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers))
            {
                Debug.DrawRay(ray.origin, ray.direction * 5, Color.green, 2f);

                if (hit.transform.gameObject.TryGetComponent(out GrabbableBodyPart bodyPart2))
                {
                    bodyPart = bodyPart2;

                    return true;
                }
            }

            bodyPart = null;
            return false;
        }

        private void GrabObject(GrabbableBodyPart bodyPart)
        {
            SetOwner(bodyPart, Owner);
            bodyPart.GetComponentInParent<Ragdoll>().SetRagdollPhysic(true);
            bodyPart.transform.position = _hands.SelectedHand.HoldTransform.position;
            _fixedJoint = _hands.SelectedHand.HandBone.gameObject.AddComponent<FixedJoint>();
            Rigidbody grabbedRb = bodyPart.GetComponent<Rigidbody>();
            _fixedJoint.connectedBody = grabbedRb;
            grabbedRb.velocity = Vector3.zero;
            _fixedJoint.breakForce = _jointBreakForce;
            grabbedRb.detectCollisions = false;
        }

        [ServerRpc]
        private void SetOwner(GrabbableBodyPart bodyPart, NetworkConnection conn = null)
        {
              bodyPart.NetworkObject.GiveOwnership(conn);
              DisableRagdollPhysics(bodyPart);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void DisableRagdollPhysics(GrabbableBodyPart bodyPart)
        {
            bodyPart.GetComponentInParent<Ragdoll>().SetRagdollPhysic(false);
        }
    }
}
