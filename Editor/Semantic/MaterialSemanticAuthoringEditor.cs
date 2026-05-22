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
            EditorGUILayout.LabelField("目标", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeChildren, new GUIContent("包含子级 Renderer"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("材质分类", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialClass, new GUIContent("分类 ID"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SSS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sssProfile, new GUIContent("Profile ID"));
            EditorGUILayout.PropertyField(thickness, new GUIContent("厚度"));
            EditorGUILayout.PropertyField(curvature, new GUIContent("曲率"));
            EditorGUILayout.PropertyField(sssSourceColor, new GUIContent("源颜色"));
            EditorGUILayout.PropertyField(sssWeight, new GUIContent("权重"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("材质自定义通道", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialCustom0_3, new GUIContent("Custom 0-3"));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用"))
                {
                    ApplyToTargets();
                }

                if (GUILayout.Button("重置"))
                {
                    ApplyResetToTargets();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetToolbar()
        {
            EditorGUILayout.LabelField("预设", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("默认", MaterialSemanticPreset.DefaultOpaque);
                DrawPresetButton("皮肤 SSS", MaterialSemanticPreset.SkinSss);
                DrawPresetButton("头发", MaterialSemanticPreset.Hair);
                DrawPresetButton("眼睛", MaterialSemanticPreset.Eye);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("布料", MaterialSemanticPreset.Cloth);
                DrawPresetButton("金属", MaterialSemanticPreset.Metal);
                DrawPresetButton("清空", MaterialSemanticPreset.Clear);
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
