using HoUrp.Extensions.Semantic;
using UnityEditor;
using UnityEngine;

namespace HoUrp.Extensions.Editor.Semantic
{
    [CustomEditor(typeof(ObjectSemanticAuthoring))]
    [CanEditMultipleObjects]
    public sealed class ObjectSemanticAuthoringEditor : UnityEditor.Editor
    {
        private static readonly string[] BindingModeLabels =
        {
            "禁用 RSUV",
            "优先 RSUV，失败回退 MPB",
            "仅 MPB"
        };

        private SerializedProperty includeChildren;
        private SerializedProperty writesAov;
        private SerializedProperty receivesSemanticPost;
        private SerializedProperty rendererStaticBindingMode;
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
            rendererStaticBindingMode = serializedObject.FindProperty("rendererStaticBindingMode");
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
            EditorGUILayout.LabelField("能力", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeChildren, new GUIContent("包含子级 Renderer"));
            EditorGUILayout.PropertyField(writesAov, new GUIContent("写入 AOV"));
            EditorGUILayout.PropertyField(receivesSemanticPost, new GUIContent("允许 SemanticPost 消费"));
            DrawBindingModeField();
            DrawBindingStatus();
            EditorGUILayout.PropertyField(maskWeight, new GUIContent("遮罩权重"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("对象语义", EditorStyles.boldLabel);
            DrawObjectCustomBits();

            EditorGUILayout.Space();
            showAdvancedIdentity = EditorGUILayout.Foldout(showAdvancedIdentity, "高级身份", true);
            if (showAdvancedIdentity)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(objectId, new GUIContent("对象 ID"));
                EditorGUILayout.PropertyField(groupId, new GUIContent("分组 ID"));
                DrawFlagBits();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("对象语义位打包值", GetObjectBitsPreview());
                    EditorGUILayout.IntField("最终 Flags", GetEffectiveFlagsPreview());
                }

                EditorGUI.indentLevel--;
            }

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
                DrawPresetButton("主体", ObjectSemanticPreset.Subject);
                DrawPresetButton("脸部", ObjectSemanticPreset.Face);
                DrawPresetButton("头发", ObjectSemanticPreset.Hair);
                DrawPresetButton("眼睛", ObjectSemanticPreset.Eye);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPresetButton("配件", ObjectSemanticPreset.Accessory);
                DrawPresetButton("衣物", ObjectSemanticPreset.Cloth);
                DrawPresetButton("道具", ObjectSemanticPreset.Prop);
                DrawPresetButton("清空", ObjectSemanticPreset.Clear);
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
            EditorGUILayout.PropertyField(custom0Subject, new GUIContent("主体"));
            EditorGUILayout.PropertyField(custom1Face, new GUIContent("脸部"));
            EditorGUILayout.PropertyField(custom2Hair, new GUIContent("头发"));
            EditorGUILayout.PropertyField(custom3Eye, new GUIContent("眼睛"));
            EditorGUILayout.PropertyField(custom4Accessory, new GUIContent("配件"));
            EditorGUILayout.PropertyField(custom5Cloth, new GUIContent("衣物"));
            EditorGUILayout.PropertyField(custom6Prop, new GUIContent("道具"));
            EditorGUILayout.PropertyField(custom7Reserved, new GUIContent("预留"));
        }

        private void DrawFlagBits()
        {
            EditorGUILayout.LabelField("Flags", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ToggleLeft("SemanticPost 接收者", receivesSemanticPost.boolValue);
            }

            EditorGUILayout.PropertyField(flag1, new GUIContent("Flag 1"));
            EditorGUILayout.PropertyField(flag2, new GUIContent("Flag 2"));
            EditorGUILayout.PropertyField(flag3, new GUIContent("Flag 3"));
            EditorGUILayout.PropertyField(flag4, new GUIContent("Flag 4"));
            EditorGUILayout.PropertyField(flag5, new GUIContent("Flag 5"));
            EditorGUILayout.PropertyField(flag6, new GUIContent("Flag 6"));
            EditorGUILayout.PropertyField(flag7, new GUIContent("Flag 7"));
        }

        private void DrawBindingModeField()
        {
            rendererStaticBindingMode.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent("静态语义绑定"),
                rendererStaticBindingMode.enumValueIndex,
                BindingModeLabels);
        }

        private void DrawBindingStatus()
        {
            if (targets.Length != 1)
            {
                EditorGUILayout.HelpBox("多选时不显示 RSUV / MPB 实际绑定统计。", MessageType.Info);
                return;
            }

            var authoring = (ObjectSemanticAuthoring)target;
            string modeText = GetBindingModeLabel((RendererStaticSemanticBindingMode)rendererStaticBindingMode.enumValueIndex);
            string status = $"当前模式：{modeText}\n"
                + $"目标 Renderer：{authoring.LastBindingTargetCount}\n"
                + $"RSUV 生效：{authoring.LastRendererUserValueBindingCount}\n"
                + $"MPB 回退/来源：{authoring.LastMaterialPropertyBlockSourceCount}";
            EditorGUILayout.HelpBox(status, MessageType.Info);
            if (GUILayout.Button("刷新绑定状态"))
            {
                serializedObject.ApplyModifiedProperties();
                authoring.ApplyToRenderers();
                EditorUtility.SetDirty(authoring);
                serializedObject.Update();
            }
        }

        private static string GetBindingModeLabel(RendererStaticSemanticBindingMode mode)
        {
            switch (mode)
            {
                case RendererStaticSemanticBindingMode.Disabled:
                    return "禁用 RSUV，仅保留 MPB 属性";
                case RendererStaticSemanticBindingMode.MaterialPropertyBlockOnly:
                    return "仅 MPB";
                default:
                    return "优先 RSUV，不支持时回退 MPB";
            }
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
