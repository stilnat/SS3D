using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
public class DummyHands : MonoBehaviour
{
    public event EventHandler<DummyHand> OnSelectedHandChange;

    [FormerlySerializedAs("leftHand")]
    [SerializeField]
    private DummyHand _leftHand;

    [FormerlySerializedAs("rightHand")]
    [SerializeField]
    private DummyHand _rightHand;

    private HandType _selectedHand = HandType.LeftHand;

    public bool BothHandFull => _leftHand.Full && _rightHand.Full;

    public bool BothHandEmpty => _leftHand.Empty && _rightHand.Empty;

    public DummyHand SelectedHand => _selectedHand == HandType.LeftHand ? _leftHand : _rightHand;

    public DummyHand UnselectedHand => _selectedHand == HandType.LeftHand ? _rightHand : _leftHand;

    public DummyHand GetHand(HandType hand) => hand == HandType.LeftHand ? _leftHand : _rightHand;

    public DummyHand GetOtherHand(HandType hand) => hand == HandType.LeftHand ? _rightHand : _leftHand;

    public IHoldProvider GetItem(bool secondary, DummyHand hand)
    {
        return secondary ? GetOtherHand(hand.HandType).Item : hand.Item;
    }

    // Update is called once per frame
    protected void Update()
    {
        if (!Input.GetKeyDown(KeyCode.X))
        {
            return;
        }

        _selectedHand = _selectedHand == HandType.LeftHand ? HandType.RightHand : HandType.LeftHand;

        OnSelectedHandChange?.Invoke(this, SelectedHand);

        Debug.Log($"Selected hand is {_selectedHand}");
    }
}
}
