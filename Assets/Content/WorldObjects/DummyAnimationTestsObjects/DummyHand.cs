using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace DummyStuff
{

public class DummyHand : MonoBehaviour
{
    private IHoldProvider _item;

    public HandType handType;

    public Transform handHoldTargetLocker;

    public Transform pickupTargetLocker;

    public Transform placeTarget;

    public Transform itemPositionTargetLocker;

    public Transform shoulderWeaponPivot;

    public TwoBoneIKConstraint holdIkConstraint;

    public ChainIKConstraint pickupIkConstraint;

    public MultiPositionConstraint itemPositionConstraint;
    
    public Transform upperArm;

    public Transform handBone;

    public IHoldProvider Item => _item;

    public bool Full => _item != null;

    public bool Empty => _item == null;

    public void RemoveItem()
    {
        _item.GameObject.transform.parent = null;
        _item.GameObject.GetComponent<Rigidbody>().isKinematic = false;
        _item.GameObject.GetComponent<Collider>().enabled = true;
        _item = null;
    }

    public void AddItem(IHoldProvider itemAdded)
    {
        _item = itemAdded;
        _item.GameObject.GetComponent<Rigidbody>().isKinematic = true;
        _item.GameObject.GetComponent<Collider>().enabled = false;
    }
    
    public Transform ChooseTargetLocker(TargetLockerType type)
    {
        Transform targetToSet;
        
        switch (type)
        {
            case TargetLockerType.Pickup:
                targetToSet = pickupTargetLocker;
                break;
            case TargetLockerType.Hold:
                targetToSet = handHoldTargetLocker;
                break;
            case TargetLockerType.ItemPosition:
                targetToSet = itemPositionTargetLocker;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return targetToSet;
    }

    public void SetParentTransformTargetLocker(TargetLockerType type, Transform parent, bool resetPosition = true,
        bool resetRotation = true)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        
        targetToSet.parent = parent;
        if(resetPosition)
            targetToSet.localPosition = Vector3.zero;
        if(resetRotation)
            targetToSet.localRotation = Quaternion.identity;
    }
    
}
    
}
