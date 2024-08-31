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

    [SerializeField]
    private HandType _handType;

    [SerializeField]
    private Transform _handHoldTargetLocker;

    [SerializeField]
    private Transform _pickupTargetLocker;

    [SerializeField]
    private Transform _placeTarget;

    [SerializeField]
    private Transform _itemPositionTargetLocker;

    [SerializeField]
    private Transform _shoulderWeaponPivot;

    [SerializeField]
    private TwoBoneIKConstraint _holdIkConstraint;

    [SerializeField]
    private ChainIKConstraint _pickupIkConstraint;

    [SerializeField]
    private MultiPositionConstraint _itemPositionConstraint;

    [SerializeField]
    private Transform _upperArm;

    [SerializeField]
    private Transform _handBone;

    [SerializeField]
    private Transform _holdTransform;

    public HandType HandType => _handType;

    public Transform PickupTargetLocker => _pickupTargetLocker;

    public Transform PlaceTarget => _placeTarget;

    public Transform ItemPositionTargetLocker => _itemPositionTargetLocker;

    public Transform ShoulderWeaponPivot => _shoulderWeaponPivot;

    public TwoBoneIKConstraint HoldIkConstraint => _holdIkConstraint;

    public ChainIKConstraint PickupIkConstraint => _pickupIkConstraint;

    public MultiPositionConstraint ItemPositionConstraint => _itemPositionConstraint;

    public Transform UpperArm => _upperArm;

    public Transform HandBone => _handBone;

    public Transform HoldTransform => _holdTransform;

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
        Transform targetToSet = type switch
        {
            TargetLockerType.Pickup => _pickupTargetLocker,
            TargetLockerType.Hold => _handHoldTargetLocker,
            TargetLockerType.ItemPosition => _itemPositionTargetLocker,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        return targetToSet;
    }

    public void SetParentTransformTargetLocker(TargetLockerType type, Transform parent, bool resetPosition = true, bool resetRotation = true)
    {
        Transform targetToSet = ChooseTargetLocker(type);
        targetToSet.parent = parent;
        if (resetPosition)
        {
            targetToSet.localPosition = Vector3.zero;
        }

        if (resetRotation)
        {
            targetToSet.localRotation = Quaternion.identity;
        }
    }
}
}
