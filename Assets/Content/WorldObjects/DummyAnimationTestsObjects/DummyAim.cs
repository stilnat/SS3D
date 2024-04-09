using Coimbra;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace DummyStuff
{
    public sealed class DummyAim : MonoBehaviour
    {

        public Transform aimTarget;

        public DummyHands hands;

        public IntentController intents;

        public HoldController holdController;

        public Rig bodyAimRig;

        public float rotationSpeed = 5f;

        public bool canAim;

        public bool isAiming;
        
        public event EventHandler<bool> OnAim; 


        private void Update()
        {
            UpdateAimAbility(hands.SelectedHand);

            if (canAim && Input.GetMouseButton(1))
            {
                UpdateAimTargetPosition();

                if (!isAiming)
                {
                    Aim(hands.SelectedHand, hands.SelectedHand.Item.GameObject.GetComponent<DummyGun>());
                    isAiming = true;
                }

                if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
                {
                    RotatePlayerTowardTarget();
                }
            }
            else if (isAiming && (!canAim || !Input.GetMouseButton(1)))
            {
                StopAiming(hands.SelectedHand);
            }
            
            

            if (Input.GetKey(KeyCode.E) && hands.SelectedHand.Full 
                && isAiming && hands.SelectedHand.Item.GameObject.TryGetComponent(out DummyGun gun))
            {
                gun.GetComponent<DummyFire>().Fire();
            }

        }

        private void Aim(DummyHand hand, DummyGun gun)
        {
            bodyAimRig.weight = 0.3f;
            gun.transform.parent = hands.SelectedHand.shoulderWeaponPivot;

            // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
            gun.transform.localPosition = -gun.rifleButt.localPosition;
            gun.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, true);
        }

        private void StopAiming(DummyHand hand)
        {
            isAiming = false;
            bodyAimRig.weight = 0f;

            if (!hand.Full)
                return;

            hand.Item.GameObject.transform.parent = hand.itemPositionTargetLocker;
            holdController.UpdateItemPositionConstraintAndRotation(hand, hand.Item,
                true, 0.5f, false);
            hand.Item.GameObject.transform.localPosition = Vector3.zero;
            hand.Item.GameObject.transform.localRotation = Quaternion.identity;
            OnAim?.Invoke(this, false);
        }

        private void UpdateAimAbility(DummyHand selectedHand)
        {
            if (intents.Intent == Intent.Harm && selectedHand.Full
                && selectedHand.Item.GameObject.HasComponent<DummyGun>())
            {
                canAim = true;
            }
            else
            {
                canAim = false;
            }
        }

        private void UpdateAimTargetPosition()
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check if the ray hits any collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                aimTarget.position = hit.point;
            }
        }

        private void RotatePlayerTowardTarget()
        {
            // Get the direction to the target
            Vector3 direction = aimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }


    }
}
