﻿using UnityEditor;

namespace SS3D.Systems.Crafting
{
    public partial class CraftingRecipe
    {
        [CustomEditor(typeof(CraftingRecipe))]
        public class CraftingRecipeEditor : Editor
        {
            private SerializedProperty _targetProperty;
            private SerializedProperty _stepsProperty;
            private SerializedProperty _stepLinksProperty;
            private RecipeStep _recipeStep;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(_targetProperty);
                EditorGUILayout.PropertyField(_stepsProperty, true);
                EditorGUILayout.PropertyField(_stepLinksProperty, true);
                serializedObject.ApplyModifiedProperties();
            }

            protected void OnEnable()
            {
                // Initialize SerializedProperties
                CraftingRecipe recipe = serializedObject.targetObject as CraftingRecipe;
                _targetProperty = serializedObject.FindProperty(nameof(recipe._target));
                _stepsProperty = serializedObject.FindProperty(nameof(recipe._steps));
                _stepLinksProperty = serializedObject.FindProperty(nameof(recipe._stepLinks));
            }
        }
    }
}
