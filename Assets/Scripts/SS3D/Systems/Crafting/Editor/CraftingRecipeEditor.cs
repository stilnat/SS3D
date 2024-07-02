using System;
using System.Linq.Expressions;
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
            _targetProperty = serializedObject.FindProperty(GetPropertyName(() => recipe.Target));
            _stepsProperty = serializedObject.FindProperty(GetPropertyName(() => recipe.Steps));
            _stepLinksProperty = serializedObject.FindProperty(GetPropertyName(() => recipe.StepLinks));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_targetProperty);
            bool hasInitialStep = HasInitialStep();

            // Draw each RecipeStep individually
            for (int i = 0; i < _stepsProperty.arraySize; i++)
            {
                SerializedProperty stepProperty = _stepsProperty.GetArrayElementAtIndex(i);
                DrawRecipeStep(stepProperty, hasInitialStep);

                EditorGUILayout.Space();
            }

            // Add a button to add a new step
            if (GUILayout.Button("Add a step"))
            {
                _stepsProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.PropertyField(_stepLinksProperty, true);
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
                switch (property.name)
                {
                    case "_isInitialState" when hasInitialStep && property.boolValue == false:
                        continue;
                    case "_isInitialState" when property.boolValue:
                        isInitialStep = true;
                        break;
                    case "_isTerminal" when property.boolValue:
                        isTerminalStep = true;
                        break;
                }
                
                switch (property.name)
                {
                    case "_isTerminal" when isInitialStep:
                    case "_customCraft" when !isTerminalStep:
                    case "_result" when !isTerminalStep:
                        continue;
                }
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
        
        /// <summary>
        /// Get the name of a static or instance property from a property access lambda.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="propertyLambda">lambda expression of the form: '() => Class.Property' or '() => object.Property'</param>
        /// <returns>The name of the property</returns>
        public static string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
        {
            MemberExpression me = propertyLambda.Body as MemberExpression;
            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }
            return me.Member.Name;
        }
    }
}

