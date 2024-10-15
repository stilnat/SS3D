using NaughtyAttributes;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class AbstractHoldable : MonoBehaviour, IHoldProvider
{
    [SerializeField]
    protected Transform _primaryRightHandHold;

    [SerializeField]
    protected Transform _primaryLeftHandHold;

    [ShowIf(nameof(CanHoldTwoHand))]
    [SerializeField]
    protected Transform _secondaryRightHandHold;

    [ShowIf(nameof(CanHoldTwoHand))]
    [SerializeField]
    protected Transform _secondaryLeftHandHold;

    public GameObject GameObject => gameObject;

    public abstract bool CanHoldTwoHand { get; }

    public abstract FingerPoseType PrimaryHandPoseType { get; }

    public abstract FingerPoseType SecondaryHandPoseType { get; }

    public abstract HandHoldType SingleHandHold { get; }

    public abstract HandHoldType TwoHandHold { get; }

    public abstract HandHoldType SingleHandHoldHarm { get; }

    public abstract HandHoldType TwoHandHoldHarm { get; }

    public abstract HandHoldType SingleHandHoldThrow { get; }

    public abstract HandHoldType TwoHandHoldThrow { get; }

    public HandHoldType GetHoldType(bool withTwoHands, IntentType intent, bool toThrow)
    {
        switch (intent, withTwoHands)
        {
            case (IntentType.Help, true):
                return TwoHandHold;
            case (IntentType.Help, false):
                return SingleHandHold;
            case (IntentType.Harm, true):
                return toThrow ? TwoHandHoldThrow : TwoHandHoldHarm;
            case (IntentType.Harm, false):
                return toThrow ? SingleHandHoldThrow : SingleHandHoldHarm;
        }

        return SingleHandHold;
    }

    public Transform GetHold(bool primary, HandType handType)
    {
        switch (primary, handType)
        {
            case (true, HandType.LeftHand):
                return _primaryLeftHandHold;
            case (false, HandType.LeftHand):
                return _secondaryLeftHandHold;
            case (true, HandType.RightHand):
                return _primaryRightHandHold;
            case (false, HandType.RightHand):
                return _secondaryRightHandHold;
            default:
                throw new ArgumentException();
        }
    }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            Mesh leftHandGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Entities/Humanoids/Human/HumanHandLeft.mesh", typeof(Mesh));
            Mesh rightHandGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Entities/Humanoids/Human/HumanHandRight.mesh", typeof(Mesh));


            if (_primaryRightHandHold != null && UnityEditor.Selection.activeTransform == _primaryRightHandHold)
            {
                DrawHand(rightHandGuide, true, _primaryRightHandHold.position, _primaryRightHandHold.localRotation, new Color32(20, 120, 255, 200));
            }
            else if (_primaryLeftHandHold != null && UnityEditor.Selection.activeTransform == _primaryLeftHandHold)
            {
                DrawHand(leftHandGuide, false,_primaryLeftHandHold.position, _primaryLeftHandHold.localRotation, new Color32(255, 120, 20, 200));
            }
            else if (_secondaryRightHandHold != null && UnityEditor.Selection.activeTransform == _secondaryRightHandHold)
            {
                DrawHand(rightHandGuide, true, _secondaryRightHandHold.position, _secondaryRightHandHold.localRotation, new Color32(255, 120, 20, 200));
            }
            else if (_secondaryLeftHandHold != null && UnityEditor.Selection.activeTransform == _secondaryLeftHandHold)
            {
                DrawHand(leftHandGuide, false, _secondaryLeftHandHold.position, _secondaryLeftHandHold.localRotation, new Color32(20, 120, 255, 200));
            }
        }

        private void DrawHand(Mesh model, bool isRight, Vector3 position, Quaternion rotation, Color color)
        {
            Gizmos.color = color;

            if (isRight)
            {
                rotation *= Quaternion.AngleAxis(-90, Vector3.back);
                position += rotation * new Vector3(0.0875f,-0.0304f,-0.0184f);
            }
            else
            {
                rotation *= Quaternion.AngleAxis(90, Vector3.back);
                position += rotation * new Vector3(-0.0875f,-0.0304f,-0.0184f);
            }

            Gizmos.DrawMesh(model, position, rotation);
        }
#endif
}
