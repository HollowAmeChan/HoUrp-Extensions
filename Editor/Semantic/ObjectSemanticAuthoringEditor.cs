using HoUrp.Extensions.Semantic;
using UnityEditor;
using UnityEngine;

namespace HoUrp.Extensions.Editor.Semantic
{
    [CustomEditor(typeof(ObjectSemanticAuthoring))]
    [CanEditMultipleObjects]
    public sealed class ObjectSemanticAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty includeChildren;
        private SerializedProperty writesAov;
        private SerializedProperty receivesSemanticPost;
        private SerializedProperty maskWeight;
        private SerializedProperty objectCustomMask;
        private SerializedProperty objectId;
        private SerializedProperty groupId;
        private SerializedProperty flags;
        private SerializedProperty custom0Subject;
        private SerializedProperty custom1Face;
        private SerializedProperty custom2Hair;
        private SerializedProperty custom3Eye;
        private SerializedProperty custom4Accessory;
        private SerializedProperty custom5Cloth;
        private SerializedProperty custom6Prop;
        private SerializedProperty custom7Reserved;
        private SerializedProperty flag1;
        private SerializedProperty flag2;
        private SerializedProperty flag3;
        private SerializedProperty flag4;
        private SerializedProperty flag5;
        private SerializedProperty flag6;
        private SerializedProperty flag7;
        private bool showAdvancedIdentity;

        private void OnEnable()
        {
            includeChildren = serializedObject.FindProperty("includeChildren");
            writesAov = serializedObject.FindProperty("writesAov");
            receivesSemanticPost = serializedObject.FindProperty("receivesSemanticPost");
            maskWeight = serializedObject.FindProperty("maskWeight");
            objectCustomMask = serializedObject.FindProperty("objectCustomMask");
            objectId = serializedObject.FindProperty("objectId");
            groupId = serializedObject.FindProperty("groupId");
            flags = serializedObject.FindProperty("flags");
            custom0Subject = serializedObject.FindProperty("custom0Subject");
            custom1Face = serializedObject.FindProperty("custom1Face");
            custom2Hair = serializedObject.FindProperty("custom2Hair");
            custom3Eye = serializedObject.FindProperty("custom3Eye");
            custom4Accessory = serializedObject.FindProperty("custom4Accessory");
            custom5Cloth = serializedObject.FindProperty("custom5Cloth");
            custom6Prop = serializedObject.FindProperty("custom6Prop");
            custom7Reserved = serializedObject.FindProperty("custom7Reserved");
            flag1 = serializedObject.FindProperty("flag1");
            flag2 = serializedObject.FindProperty("flag2");
            flag3 = serializedObject.FindProperty("flag3");
            flag4 = serializedObject.FindProperty("flag4");
            flag5 = serializedObject.FindProperty("flag5");
            flag6 = serializedObject.FindProperty("flag6");
            flag7 = serializedObject.FindProperty("flag7");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPresetToolbar();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeChildren, new GUIContent("Include Children"));
            EditorGUILayout.PropertyField(writesAov, new GUIContent("Writes AOV"));
            EditorGUILayout.PropertyField(receivesSemanticPost, new GUIContent("Receives Semantic Post"));
            EditorGUILayout.PropertyField(maskWeight, new GUIContent("Mask Weight"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Semantics", EditorStyles.boldLabel);
            DrawObjectCustomBits();

            EditorGUILayout.Space();
            showAdvancedIdentity = EditorGUILayout.Foldout(showAdvancedIdentity, "Advanced Identity", true);
            if (showAdvancedIdentity)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(objectId, new GUIContent("Object Id"));
                EditorGUILayout.PropertyField(groupId, new GUIContent("Group Id"));
                DrawFlagBits();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Packed Object Bits", GetObjectBitsPreview());
                    EditorGUILayout.IntField("Effective Flags", GetEffectiveFlagsPreview());
                }

                EditorGUI.indentLevel--;
            }

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
                DrawPresetButton("Subject", ObjectSemanticPreset.Subject);
                DrawPresetButton("Face", ObjectSemanticPreset.Face);
                DrawPresetButton("Hair", ObjectSemanticPreset.Hair);
                DrawPresetButton("Eye", ObjectSemanticPreset.Eye);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("Accessory", ObjectSemanticPreset.Accessory);
                DrawPresetButton("Cloth", ObjectSemanticPreset.Cloth);
                DrawPresetButton("Prop", ObjectSemanticPreset.Prop);
                DrawPresetButton("Clear", ObjectSemanticPreset.Clear);
            }
        }

        private void DrawPresetButton(string label, ObjectSemanticPreset preset)
        {
            if (!GUILayout.Button(label))
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            foreach (Object targetObject in targets)
            {
                ObjectSemanticAuthoring authoring = (ObjectSemanticAuthoring)targetObject;
                Undo.RecordObject(authoring, "Apply Object Semantic Preset");
                authoring.ApplyPreset(preset);
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }

        private void DrawObjectCustomBits()
        {
            EditorGUILayout.PropertyField(custom0Subject, new GUIContent("Subject"));
            EditorGUILayout.PropertyField(custom1Face, new GUIContent("Face"));
            EditorGUILayout.PropertyField(custom2Hair, new GUIContent("Hair"));
            EditorGUILayout.PropertyField(custom3Eye, new GUIContent("Eye"));
            EditorGUILayout.PropertyField(custom4Accessory, new GUIContent("Accessory"));
            EditorGUILayout.PropertyField(custom5Cloth, new GUIContent("Cloth"));
            EditorGUILayout.PropertyField(custom6Prop, new GUIContent("Prop"));
            EditorGUILayout.PropertyField(custom7Reserved, new GUIContent("Reserved"));
        }

        private void DrawFlagBits()
        {
            EditorGUILayout.LabelField("Flags", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ToggleLeft("Semantic Post Receiver", receivesSemanticPost.boolValue);
            }

            EditorGUILayout.PropertyField(flag1, new GUIContent("Flag 1"));
            EditorGUILayout.PropertyField(flag2, new GUIContent("Flag 2"));
            EditorGUILayout.PropertyField(flag3, new GUIContent("Flag 3"));
            EditorGUILayout.PropertyField(flag4, new GUIContent("Flag 4"));
            EditorGUILayout.PropertyField(flag5, new GUIContent("Flag 5"));
            EditorGUILayout.PropertyField(flag6, new GUIContent("Flag 6"));
            EditorGUILayout.PropertyField(flag7, new GUIContent("Flag 7"));
        }

        private int GetEffectiveFlagsPreview()
        {
            int packed = 0;
            if (flag1.boolValue) packed |= 1 << 1;
            if (flag2.boolValue) packed |= 1 << 2;
            if (flag3.boolValue) packed |= 1 << 3;
            if (flag4.boolValue) packed |= 1 << 4;
            if (flag5.boolValue) packed |= 1 << 5;
            if (flag6.boolValue) packed |= 1 << 6;
            if (flag7.boolValue) packed |= 1 << 7;
            return receivesSemanticPost.boolValue
                ? packed | ObjectSemanticAuthoring.SemanticPostReceiverFlag
                : packed;
        }

        private int GetObjectBitsPreview()
        {
            int packed = 0;
            if (custom0Subject.boolValue) packed |= 1 << 0;
            if (custom1Face.boolValue) packed |= 1 << 1;
            if (custom2Hair.boolValue) packed |= 1 << 2;
            if (custom3Eye.boolValue) packed |= 1 << 3;
            if (custom4Accessory.boolValue) packed |= 1 << 4;
            if (custom5Cloth.boolValue) packed |= 1 << 5;
            if (custom6Prop.boolValue) packed |= 1 << 6;
            if (custom7Reserved.boolValue) packed |= 1 << 7;
            return packed;
        }

        private void ApplyToTargets()
        {
            serializedObject.ApplyModifiedProperties();
            foreach (Object targetObject in targets)
            {
                ObjectSemanticAuthoring authoring = (ObjectSemanticAuthoring)targetObject;
                authoring.ApplyToRenderers();
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }

        private void ApplyResetToTargets()
        {
            foreach (Object targetObject in targets)
            {
                ObjectSemanticAuthoring authoring = (ObjectSemanticAuthoring)targetObject;
                Undo.RecordObject(authoring, "Reset Object Semantic Values");
                authoring.ResetObjectSemantics();
                EditorUtility.SetDirty(authoring);
            }

            serializedObject.Update();
        }
    }
}
