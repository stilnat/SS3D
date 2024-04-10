using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = System.Object;

namespace DummyStuff
{

    public class DummyThrow : MonoBehaviour
    {
        public DummyHands hands;
        public DummyAnimatorController animatorController;

        [SerializeField]
        private DummyMovement movementController;
        
        [SerializeField]
        private Transform aimTarget;

        [SerializeField]
        private float maxForce = 20;

        [SerializeField]
        private float secondPerMeterFactorDef = 0.22f;
        
        [SerializeField]
        private float secondPerMeterFactorHarm = 0.15f;

        private bool _canAim;

        private bool _isAiming;

        public HoldController holdController;

        public Rig bodyAimRig;

        [SerializeField]
        private IntentController intents;

        public bool IsAiming => _isAiming;
        
        public event EventHandler<bool> OnAim;

        
        


        // Update is called once per frame
        private void Update()
        {
            UpdateAimAbility(hands.SelectedHand);

            if (_canAim && Input.GetKeyDown(KeyCode.R))
            {
                if (!_isAiming)
                {
                    Aim();
                }
                else
                {
                    StopAiming(hands.SelectedHand);
                }
            }

            if (_isAiming)
            {
                UpdateAimTargetPosition();

                if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
                {
                    movementController.RotatePlayerTowardTarget();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Y) && hands.SelectedHand.Full && _isAiming)
            {
                StartCoroutine(Throw());
            }
        }

        private IEnumerator Throw()
        {
            IHoldProvider item = hands.SelectedHand.Item;
            hands.SelectedHand.itemPositionConstraint.weight = 0f;
            hands.SelectedHand.holdIkConstraint.weight = 0f;
            hands.SelectedHand.pickupIkConstraint.weight = 0f;

            if (item.CanHoldTwoHand && hands.UnselectedHand.Empty)
            {
                hands.UnselectedHand.itemPositionConstraint.weight = 0f;
                hands.UnselectedHand.holdIkConstraint.weight = 0f;
                hands.UnselectedHand.pickupIkConstraint.weight = 0f;
            }

            item.GameObject.transform.parent = hands.SelectedHand.handBone.transform;

            animatorController.Throw(hands.SelectedHand.handType);

            StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, aimTarget.transform, 0.18f, false, true));

            yield return new WaitForSeconds(0.18f);

            AddForceToItem(item.GameObject);

            StopAiming(hands.SelectedHand);
        }

        private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight, float initialHorizontalPosition)
        {
            // Those computations assume gravity is pulling in the same plane as the throw.
            // it works with any vertical gravity but not if there's a horizontal component to it.
            // be careful as g = -9.81 and not 9.81
            float g = Physics.gravity.y;
            float initialHorizontalVelocity = (targetCoordinates.x - initialHorizontalPosition) / timeToReachTarget;

            float initialVerticalVelocity = (targetCoordinates.y - initialHeight - 0.5f * g * (math.pow(targetCoordinates.x - initialHorizontalPosition, 2) / math.pow(initialHorizontalVelocity, 2))) * initialHorizontalVelocity / (targetCoordinates.x - initialHorizontalPosition);

            return new Vector2(initialHorizontalVelocity, initialVerticalVelocity);

        }

        /// <summary>
        /// Compute coordinates in the local coordinate system of the throwing hand
        /// This method assumes that the target position is in the same plane as the plane defined by the
        /// player y and z local axis.
        /// return vector2 with components in order z and y, as z is forward and y upward.
        /// </summary>
        private Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
        {
            Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

            if (rootRelativeTargetPosition.x > 0.1f)
            {
                Debug.LogError("target not in the same plane as the player root : " + rootRelativeTargetPosition.x);
            }

            return new Vector2(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
        }

        private Vector2 ComputeItemInitialCoordinates(Vector3 itemPosition, Transform playerRoot)
        {
            Vector3 rootRelativeItemPosition = playerRoot.InverseTransformPoint(itemPosition);

            return new Vector2(rootRelativeItemPosition.z, rootRelativeItemPosition.y);
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
        
        private void Aim()
        {
            _isAiming = true;
            bodyAimRig.weight = 0.3f;

            if (intents.Intent == Intent.Harm)
            {
                holdController.UpdateItemPositionConstraintAndRotation(hands.SelectedHand,
                    hands.SelectedHand.Item, false, 0.2f, true);
            }
            
            OnAim?.Invoke(this, true);
        }

        private void StopAiming(DummyHand hand)
        {
            _isAiming = false;
            bodyAimRig.weight = 0f;
            holdController.UpdateItemPositionConstraintAndRotation(hands.SelectedHand,
                hands.SelectedHand.Item,false, 0.2f, false);
            OnAim?.Invoke(this, false);
        }

        private void UpdateAimAbility(DummyHand selectedHand)
        {
            if (selectedHand.Full)
            {
                _canAim = true;
            }
            else
            {
                _canAim = false;
            }
        }

        private void AddForceToItem(GameObject item)
        {
            Vector2 targetCoordinates = ComputeTargetCoordinates(aimTarget.position, transform);

            Vector2 initialItemCoordinates = ComputeItemInitialCoordinates(item.transform.position, transform);

            Vector2 initialVelocity = ComputeInitialVelocity( ComputeTimeToReach(intents.Intent, aimTarget.position),
                targetCoordinates, initialItemCoordinates.y, initialItemCoordinates.x);

            Vector3 initialVelocityInRootCoordinate = new Vector3(0, initialVelocity.y, initialVelocity.x);

            Vector3 initialVelocityInWorldCoordinate = transform.TransformDirection(initialVelocityInRootCoordinate);

            hands.SelectedHand.RemoveItem();

            if (initialVelocityInWorldCoordinate.magnitude > maxForce)
            {
                initialVelocityInWorldCoordinate = initialVelocityInWorldCoordinate.normalized * maxForce;
            }

            item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
        }

        private float ComputeTimeToReach(Intent intent, Vector3 targetPosition)
        {
            float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

            return intent == Intent.Def ? 
                distanceToTarget * secondPerMeterFactorDef : distanceToTarget * secondPerMeterFactorHarm;
        }
    }
}