using DummyStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Grab : MonoBehaviour
{
    public float grabRange = 2f;
    public LayerMask grabbableLayer;

    private GameObject grabbedObject;
    
    private FixedJoint fixedJoint;

    [SerializeField]
    private float _jointBreakForce = 250f;

    [SerializeField]
    private DummyHands _hands;

    [SerializeField]
    private HoldController holdController;

    [SerializeField]
    private float itemReachDuration;
    
    [SerializeField]
    private float itemMoveDuration;
    
    [SerializeField]
    private DummyHands hands;
    
    [SerializeField]
    private Transform lookAtTargetLocker;
    
    public MultiAimConstraint lookAtConstraint;
    
    public event EventHandler<bool> OnGrab; 

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (grabbedObject == null)
            {
                TryGrab();
            }
            else
            {
                ReleaseGrab();
            }
        }
    }

    void TryGrab()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, grabbableLayer))
        {
                
                grabbedObject = hit.transform.gameObject;

                if (grabbedObject.TryGetComponent(out GrabbableBodyPart bodyPart))
                {
                    StartCoroutine(GrabObject(_hands.SelectedHand, _hands.UnselectedHand, bodyPart));
                }
                
        }
    }
    

    private IEnumerator GrabObject(DummyHand mainHand, DummyHand secondaryHand, GrabbableBodyPart bodyPart)
    {
        SetUpGrab(bodyPart, mainHand, secondaryHand, false);

        yield return GrabReach(bodyPart, mainHand, secondaryHand, false);
        yield return GrabPullBack(bodyPart, mainHand, secondaryHand, false);
        
    }
    
    private void OnJointBreak(float breakForce)
    {
        Debug.Log("A joint has just been broken!, force: " + breakForce);
        ReleaseGrab();
    }

    void ReleaseGrab()
    {
        if (grabbedObject != null)
        {
            Rigidbody grabbedRb = grabbedObject.GetComponent<Rigidbody>();
            if (grabbedRb != null)
            {
                grabbedRb.detectCollisions = true; // Enable collisions again
            }
            Destroy(fixedJoint);
            //grabbedObject.transform.parent = null;
            grabbedObject = null;
            
            OnGrab?.Invoke(this, false);
        }
    }

    private void SetUpGrab(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        
        mainHand.SetParentTransformTargetLocker(TargetLockerType.Pickup, grabbedObject.transform);
        
        // Needed if this has been changed elsewhere 
        mainHand.pickupIkConstraint.data.tipRotationWeight = 1f;

        // Reproduce changes on secondary hand if necessary.
        if (withTwoHands)
        {
            secondaryHand.pickupIkConstraint.data.tipRotationWeight = 1f;
        }

        // Set up the look at target locker on the item to pick up.
        lookAtTargetLocker.parent = item.transform;
        lookAtTargetLocker.localPosition = Vector3.zero;
        lookAtTargetLocker.localRotation = Quaternion.identity;

        OrientTargetForHandRotation(mainHand);
    }

    private IEnumerator GrabReach(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
        {
            StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(transform, item.transform, itemReachDuration, false, true));
        }
        
        if (mainHand.handBone.transform.position.y - item.transform.position.y > 0.3)
        {
            GetComponent<DummyAnimatorController>().Crouch(true);

            yield return new WaitForSeconds(0.25f);
        }
        
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight = x, 0f, 1f, itemReachDuration));
        
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            0f, 1f, itemReachDuration);
    }

    private IEnumerator GrabPullBack(GrabbableBodyPart item, DummyHand mainHand, DummyHand secondaryHand, bool withTwoHands)
    {
        mainHand.SetParentTransformTargetLocker(TargetLockerType.Pickup, null, false, false);
        mainHand.pickupTargetLocker.transform.position = item.transform.position;
        
        GetComponent<DummyAnimatorController>().Crouch(false);
        grabbedObject.transform.position = mainHand.handBone.position;
        fixedJoint = mainHand.handBone.gameObject.AddComponent<FixedJoint>();
        Rigidbody grabbedRb = grabbedObject.GetComponent<Rigidbody>();
        fixedJoint.connectedBody = grabbedRb;
        grabbedRb.velocity = Vector3.zero;
        fixedJoint.breakForce = _jointBreakForce;
        grabbedRb.detectCollisions = false; // Disable collisions between the two characters
    
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight = x, 1f, 0f, itemReachDuration));
    
        yield return CoroutineHelper.ModifyValueOverTime(x => mainHand.pickupIkConstraint.weight = x,
            1f, 0f, itemMoveDuration);
    
        Debug.Log("Grabbed object is " + grabbedObject.name);
        OnGrab?.Invoke(this, true);
        
    }
    
    /// <summary>
    /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
    /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
    /// </summary>
    private void OrientTargetForHandRotation(DummyHand hand)
    {
        Vector3 armTargetDirection = hand.pickupTargetLocker.position - hand.upperArm.position;

        Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);

        targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

        hand.pickupTargetLocker.rotation = targetRotation;
    }
    
 
}
