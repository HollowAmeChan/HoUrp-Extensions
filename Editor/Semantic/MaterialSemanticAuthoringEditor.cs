using HoUrp.Extensions.Semantic;
using UnityEditor;
using UnityEngine;

namespace HoUrp.Extensions.Editor.Semantic
{
    [CustomEditor(typeof(MaterialSemanticAuthoring))]
    [CanEditMultipleObjects]
    public sealed class MaterialSemanticAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty includeChildren;
        private SerializedProperty materialClass;
        private SerializedProperty sssProfile;
        private SerializedProperty thickness;
        private SerializedProperty curvature;
        private SerializedProperty materialCustom0_3;
        private SerializedProperty sssSourceColor;
        private SerializedProperty sssWeight;

        private void OnEnable()
        {
            includeChildren = serializedObject.FindProperty("includeChildren");
            materialClass = serializedObject.FindProperty("materialClass");
            sssProfile = serializedObject.FindProperty("sssProfile");
            thickness = serializedObject.FindProperty("thickness");
            curvature = serializedObject.FindProperty("curvature");
            materialCustom0_3 = serializedObject.FindProperty("materialCustom0_3");
            sssSourceColor = serializedObject.FindProperty("sssSourceColor");
            sssWeight = serializedObject.FindProperty("sssWeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPresetToolbar();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeChildren, new GUIContent("Include Children"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Class", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialClass, new GUIContent("Class Id"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SSS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sssProfile, new GUIContent("Profile"));
            EditorGUILayout.PropertyField(thickness, new GUIContent("Thickness"));
            EditorGUILayout.PropertyField(curvature, new GUIContent("Curvature"));
            EditorGUILayout.PropertyField(sssSourceColor, new GUIContent("Source Color"));
            EditorGUILayout.PropertyField(sssWeight, new GUIContent("Weight"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Custom", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialCustom0_3, new GUIContent("Custom 0-3"));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplyToTargets();
                }

                if (GUILayout.Button("Reset"))
                {
                    ApplyResetToTargets();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetToolbar()
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("Default", MaterialSemanticPreset.DefaultOpaque);
                DrawPresetButton("Skin SSS", MaterialSemanticPreset.SkinSss);
                DrawPresetButton("Hair", MaterialSemanticPreset.Hair);
                DrawPresetButton("Eye", MaterialSemanticPreset.Eye);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("Cloth", MaterialSemanticPreset.Cloth);
                DrawPresetButton("Metal", MaterialSemanticPreset.Metal);
                DrawPresetButton("Clear", MaterialSemanticPreset.Clear);
            }
        }

        private void DrawPresetButton(string label, MaterialSemanticPreset preset)
        {
            if (!GUILayout.Button(label))
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            foreach (Object targetObject in targets)
            {
                MaterialSemanticAuthoring authoring = (MaterialSemanticAuthoring)targetObject;
                Undo.RecordObject(authoring, "Apply Material Semantic Preset");
                authoring.ApplyPreset(preset);
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }

        private void ApplyToTargets()
        {
            serializedObject.ApplyModifiedProperties();
            foreach (Object targetObject in targets)
            {
                MaterialSemanticAuthoring authoring = (MaterialSemanticAuthoring)targetObject;
                authoring.ApplyToRenderers();
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }

        private void ApplyResetToTargets()
        {
            foreach (Object targetObject in targets)
            {
                MaterialSemanticAuthoring authoring = (MaterialSemanticAuthoring)targetObject;
                Undo.RecordObject(authoring, "Reset Material Semantic Values");
                authoring.ResetMaterialSemantics();
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }
    }
}
