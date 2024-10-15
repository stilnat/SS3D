using JetBrains.Annotations;
using NaughtyAttributes;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public class Holdable : MonoBehaviour,  IHoldProvider
    {
        [SerializeField]
        private bool _canHoldTwoHand;

        [SerializeField]
        private HandHoldType _singleHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHold;

        [SerializeField]
        private HandHoldType _singleHandHoldHarm;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldHarm;

        [SerializeField]
        private HandHoldType _singleHandHoldThrow;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private HandHoldType _twoHandHoldThrow;

        [SerializeField]
        private Transform _primaryRightHandHold;

        [SerializeField]
        private Transform _primaryLeftHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryRightHandHold;

        [ShowIf(nameof(_canHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryLeftHandHold;

        public bool CanHoldTwoHand => _canHoldTwoHand;

        [NotNull]
        public GameObject GameObject => gameObject;

        public HandHoldType GetHoldType(bool withTwoHands, IntentType intent, bool toThrow)
        {
            switch (intent, withTwoHands)
            {
                case (IntentType.Help, true):
                    return _twoHandHold;
                case (IntentType.Help, false):
                    return _singleHandHold;
                case (IntentType.Harm, true):
                    return toThrow ? _twoHandHoldThrow : _twoHandHoldHarm;
                case (IntentType.Harm, false):
                    return toThrow ? _singleHandHoldThrow : _singleHandHoldHarm;
            }

            return _singleHandHold;
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
}
