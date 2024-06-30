using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    [CustomEditor(typeof(CraftingRecipe))]
    public class CraftingRecipeEditor : Editor
    {
        private SerializedProperty _targetProperty;
        private SerializedProperty _stepsProperty;
        private SerializedProperty _stepLinksProperty;

        private void OnEnable()
        {
            // Initialize SerializedProperties
            CraftingRecipe recipe = serializedObject.targetObject as CraftingRecipe;
            _targetProperty = serializedObject.FindProperty(CraftingRecipe.GetPropertyName(() => recipe.Target));
            _stepsProperty = serializedObject.FindProperty(CraftingRecipe.GetPropertyName(() => recipe.steps));
            _stepLinksProperty = serializedObject.FindProperty(CraftingRecipe.GetPropertyName(() => recipe.stepLinks));
        }

        public override void OnInspectorGUI()
        {
            // Update SerializedObject
            serializedObject.Update();

            // Draw target property
            EditorGUILayout.PropertyField(_targetProperty);

            // Check if any step has _isInitial set to true
            bool hasInitialStep = HasInitialStep();

            // Draw each RecipeStep individually
            for (int i = 0; i < _stepsProperty.arraySize; i++)
            {
                SerializedProperty stepProperty = _stepsProperty.GetArrayElementAtIndex(i);
                DrawRecipeStep(stepProperty, hasInitialStep);

                EditorGUILayout.Space();
            }

            // Add a button to add a new step
            if (GUILayout.Button("Add Step"))
            {
                // Increase the array size by 1 and get the new element
                _stepsProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            // Draw stepLinks list property
            EditorGUILayout.PropertyField(_stepLinksProperty, true); // 'true' means to draw children

            // Apply changes to SerializedObject
            serializedObject.ApplyModifiedProperties();
        }

        // Custom drawer for RecipeStep
        private void DrawRecipeStep(SerializedProperty stepProperty, bool hasInitialStep)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isInitialStep = false;

            bool isTerminalStep = false;

            // Iterate over properties of RecipeStep
            foreach (SerializedProperty property in stepProperty)
            {

                if (property.name == "_isInitialState" && hasInitialStep && property.boolValue == false)
                    continue;

                if (property.name == "_isInitialState" && property.boolValue == true) isInitialStep = true;

                if (property.name == "_isTerminal" && property.boolValue == true) isTerminalStep = true;

                if (isInitialStep && property.name == "_isTerminal")
                    continue;

                if (!isTerminalStep && property.name == "_customCraft")
                    continue;

                if (!isTerminalStep && property.name == "_result")
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            EditorGUILayout.EndVertical();
        }

        private bool HasInitialStep()
        { 
            for (int i = 0; i < _stepsProperty.arraySize; i++)
            {
                SerializedProperty stepProperty = _stepsProperty.GetArrayElementAtIndex(i);
                SerializedProperty isInitialProperty = stepProperty.FindPropertyRelative("_isInitialState");
                if (isInitialProperty != null && isInitialProperty.boolValue)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}

