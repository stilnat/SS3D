using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Necessary class to initialize the rig builder, because some target for the rigs need to be taken
/// out of the Human prefab, as they should not depend on the player movement, but also it's convenient to
/// pack them in the human prefab.
/// </summary>
public class RigBuild : MonoBehaviour
{
    [SerializeField]
    private Transform rightPickupTargetLocker;
    
    [SerializeField]
    private Transform leftPickupTargetLocker;
    
    [SerializeField]
    private Transform rightHoldTargetLocker;
    
    [SerializeField]
    private Transform leftHoldTargetLocker;
    
    [SerializeField]
    private Transform lookAtTargetLocker;
    
    [SerializeField]
    private Transform leftPlaceTarget;
    
    [SerializeField]
    private Transform rightPlaceTarget;
    
    // Start is called before the first frame update
    void Start()
    {
        rightPickupTargetLocker.transform.parent = null;
        leftPickupTargetLocker.transform.parent = null;
        rightHoldTargetLocker.transform.parent = null;
        leftHoldTargetLocker.transform.parent = null;
        lookAtTargetLocker.transform.parent = null;
        leftPlaceTarget.transform.parent = null;
        rightPlaceTarget.transform.parent = null;
        
        var animator = GetComponent<Animator>();
        var rigBuilder = GetComponent<RigBuilder>();
     
        rigBuilder.Build();
        animator.Rebind();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
