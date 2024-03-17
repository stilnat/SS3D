using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyInteract : MonoBehaviour
{
    
    public DummyHands hands;
    public Transform lookAtTargetLocker;
    public Transform hips;

    public float interactionMoveDuration;
    
    public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.3f;
    

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        TryInteract();
    }
    
    private void TryInteract()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit) && hands.SelectedHand.Full &&
            UnderMaxDistanceFromHips(hit.point))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;
            StartCoroutine(Interact(obj.transform, hands.SelectedHand));
        }
        
        
    }

    private IEnumerator Interact(Transform interactionTarget, DummyHand mainHand)
    {
        DummyItem tool = hands.SelectedHand.item;
        
      //  Vector3 directionFromTransformToTarget = interactionTarget.position - transform.position;

       // Quaternion initialPlayerRotation = transform.rotation;
       // transform.rotation = Quaternion.LookRotation(directionFromTransformToTarget);
        
        Vector3 startPosition = tool.transform.position;

        Transform initialParent = tool.transform.parent;

        Quaternion initialRotation = tool.transform.rotation;

        Vector3 fromShoulderToTarget = (interactionTarget.transform.position - mainHand.upperArm.transform.position).normalized;
        
        // rotate the tool such that its interaction transform Z axis align with the fromShoulderToTarget vector.
        Quaternion rotation = Quaternion.FromToRotation(tool.interactionPoint.TransformDirection(Vector3.forward), fromShoulderToTarget.normalized);
        
        // Apply the rotation to transform A
        tool.transform.rotation = rotation * tool.transform.rotation;
        
        // Calculate the difference between the tool position and its interaction point.
        // Warning : do it only after applying the rotation.
        Vector3 difference = tool.interactionPoint.position - tool.transform.position;

        // Compute the desired position for the tool
        Vector3 endPosition = interactionTarget.position - difference;

       // transform.rotation = initialPlayerRotation;
        
        // Rotate player toward item
        //StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
       //     transform, interactionTarget, interactionMoveDuration, false, true));

        yield return CoroutineHelper.ModifyVector3OverTime(x => 
            tool.transform.position = x,  startPosition, endPosition, interactionMoveDuration);

        yield return new WaitForSeconds(1f);
        
        yield return CoroutineHelper.ModifyVector3OverTime(x => 
            tool.transform.position = x,  endPosition, startPosition, interactionMoveDuration);

        tool.transform.rotation = initialRotation;
        tool.transform.parent = initialParent;
    }
}
