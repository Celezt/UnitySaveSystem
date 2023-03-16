using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Celezt.SaveSystem
{
    [CustomEditor(typeof(SaveBehaviour))]
    public class SaveBehaviourEditor : Editor
    {
        private SaveBehaviour _savedBehaviour;

        private bool _isSavedPropertiesOpen = true;
        private bool _isInstancePropertiesOpen = true;

        public override void OnInspectorGUI()
        {
            if (_savedBehaviour == null) 
                _savedBehaviour = (SaveBehaviour)target;

            serializedObject.Update();

            if (_savedBehaviour.IsAssetOnDisk())
                EditorGUILayout.LabelField("Persistent Object");
            else if (_savedBehaviour.IsInstancedAtRuntime)
                EditorGUILayout.LabelField("Runtime Instanced Object");
            else
                EditorGUILayout.LabelField("Scene Instanced Object");

            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Guid:", _savedBehaviour.GetGuid().ToString());

            _isSavedPropertiesOpen = EditorGUILayout.Foldout(_isSavedPropertiesOpen, "Saved Properties", true);
            if (_isSavedPropertiesOpen)
            {
                MiniLabel("Transform");
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_isPositionSaved"), new GUIContent("Position"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_isRotationSaved"), new GUIContent("Rotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_isScaleSaved"), new GUIContent("Scale"));
                if (!_savedBehaviour.IsInstancedAtRuntime)
                {
                    MiniLabel("State");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_isDestroyedSaved"), new GUIContent("Destroy (Scene instanced)"));
                }
                EditorGUI.indentLevel--;
            }

            _isInstancePropertiesOpen = EditorGUILayout.Foldout(_isInstancePropertiesOpen, "Instance Properties", true);
            if (_isInstancePropertiesOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_assetReference"), new GUIContent("Asset"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void MiniLabel(string label)
        {
            int currentIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = -7;
            EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.indentLevel = currentIndent;
        }
    }
}
