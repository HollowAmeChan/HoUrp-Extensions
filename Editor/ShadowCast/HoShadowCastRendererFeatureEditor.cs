using HoUrp.Extensions.ShadowCast;
using UnityEditor;
using UnityEngine;

namespace HoUrp.Extensions.Editor.ShadowCast
{
    [CustomEditor(typeof(HoShadowCastRendererFeature))]
    internal sealed class HoShadowCastRendererFeatureEditor : UnityEditor.Editor
    {
        private static bool showAdvancedLights;
        private static bool showAdvancedPasses;

        private SerializedProperty settings;

        private void OnEnable()
        {
            settings = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("找不到 ShadowCast 设置。", MessageType.Error);
                return;
            }

            serializedObject.Update();
            DrawSettings();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8.0f);
            DrawRuntimeReport(((HoShadowCastRendererFeature)target).RuntimeReport);
        }

        private void DrawSettings()
        {
            DrawSection("开关");
            DrawProperty("enabled", "启用 ShadowCast");
            DrawProperty("enabledForGameView", "游戏视图");
            DrawProperty("enabledForSceneView", "场景视图");

            DrawSection("灯光来源");
            DrawProperty("collectVisibleSceneLights", "自动收集可见灯光");
            DrawProperty("lightLayerMask", "参与灯光层");
            DrawProperty("casterLayerMask", "投射物体层");

            showAdvancedLights = EditorGUILayout.Foldout(showAdvancedLights, "高级：手动补充灯光列表", true);
            if (showAdvancedLights)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.HelpBox("默认不需要在这里拖灯。RendererFeature 资产无法稳定引用场景对象，推荐使用自动收集；这些列表只作为高级补充入口。", MessageType.Info);
                    DrawProperty("spotLights", "手动 Spot Light");
                    DrawProperty("pointLights", "手动 Point Light");
                    DrawProperty("secondDirectionalLights", "手动次方向光");
                }
            }

            DrawSection("接收强度");
            DrawProperty("receiverStrength", "总接收强度");
            DrawProperty("punctualShadowStrength", "点/聚光阴影强度");
            DrawProperty("punctualShadowFadeSpeed", "点/聚光衰减速度");
            DrawProperty("secondDirectionalShadowStrength", "次方向光阴影强度");

            DrawSection("Atlas 分辨率");
            DrawProperty("atlasSize", "点/聚光 Atlas 尺寸");
            DrawProperty("spotResolution", "Spot 单片分辨率");
            DrawProperty("pointFaceResolution", "Point 单面分辨率");
            DrawProperty("secondDirectionalAtlasSize", "次方向光 Atlas 尺寸");
            DrawProperty("secondDirectionalCascadeResolution", "次方向光级联分辨率");

            DrawSection("次方向光级联");
            DrawProperty("secondDirectionalCascadeCount", "级联数量");
            DrawProperty("secondDirectionalMaxDistance", "最大距离");
            DrawProperty("secondDirectionalShadowDepth", "阴影深度");
            DrawProperty("secondDirectionalCascadeSplits", "级联分割");

            DrawSection("软阴影");
            DrawProperty("pcssEnabled", "启用 PCSS");
            DrawProperty("pcssQuality", "PCSS 质量");
            DrawProperty("punctualPcssSoftness", "点/聚光软度");
            DrawProperty("secondDirectionalPcssSoftness", "次方向光软度");
            DrawProperty("pcssBlockerSearchRadius", "遮挡搜索半径");
            DrawProperty("pcssMaxPenumbraRadius", "最大半影半径");
            DrawProperty("pcssDepthBias", "深度偏移");

            DrawSection("调试");
            DrawProperty("debugMode", "调试视图");
            DrawProperty("debugShader", "调试 Shader");

            showAdvancedPasses = EditorGUILayout.Foldout(showAdvancedPasses, "高级：Render Pass 时机", true);
            if (showAdvancedPasses)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawProperty("passEvent", "ShadowCast Pass 时机");
                    DrawProperty("debugPassEvent", "Debug Pass 时机");
                }
            }
        }

        private void DrawProperty(string propertyName, string label)
        {
            SerializedProperty property = settings.FindPropertyRelative(propertyName);
            if (property == null)
            {
                EditorGUILayout.HelpBox($"缺少设置字段：{propertyName}", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private static void DrawSection(string label)
        {
            EditorGUILayout.Space(6.0f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static void DrawRuntimeReport(HoShadowCastRuntimeReport report)
        {
            DrawSection("运行时参与状态");
            if (report == null)
            {
                EditorGUILayout.HelpBox("暂无运行时状态。", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("相机", string.IsNullOrEmpty(report.cameraName) ? "无" : report.cameraName);
                EditorGUILayout.EnumPopup("相机类型", report.cameraType);
                EditorGUILayout.Toggle("已发布阴影数据", report.rendered);
                EditorGUILayout.TextField("状态", TranslateStatus(report.status));
                EditorGUILayout.IntField("URP 可见灯数量", report.visibleLightCount);
                EditorGUILayout.IntField("点/聚光请求切片", report.requestedPunctualSlices);
                EditorGUILayout.IntField("点/聚光参与灯数", report.punctualLightCount);
                EditorGUILayout.IntField("点/聚光实际切片", report.punctualSliceCount);
                EditorGUILayout.IntField("次方向光请求切片", report.requestedSecondDirectionalSlices);
                EditorGUILayout.IntField("次方向光参与灯数", report.secondDirectionalLightCount);
                EditorGUILayout.IntField("次方向光实际切片", report.secondDirectionalSliceCount);
                EditorGUILayout.IntField("跳过：层/状态/类型", report.skippedNotCollectableCount);
                EditorGUILayout.IntField("跳过：主方向光", report.skippedMainDirectionalCount);
                EditorGUILayout.IntField("跳过：重复灯光", report.skippedDuplicateCount);
                EditorGUILayout.IntField("跳过：Atlas 容量不足", report.skippedCapacityCount);
                EditorGUILayout.IntField("跳过：矩阵构建失败", report.skippedMatrixCount);
            }

            DrawLightList("点/聚光参与列表", report.punctualLights, report.punctualLightCount);
            DrawLightList("次方向光参与列表", report.secondDirectionalLights, report.secondDirectionalLightCount);
        }

        private static void DrawLightList(string title, HoShadowCastRuntimeLight[] lights, int count)
        {
            EditorGUILayout.Space(4.0f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (lights == null || count <= 0)
            {
                EditorGUILayout.HelpBox("当前没有参与灯光。", MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                int clampedCount = Mathf.Min(count, lights.Length);
                for (int i = 0; i < clampedCount; i++)
                {
                    HoShadowCastRuntimeLight item = lights[i];
                    EditorGUILayout.ObjectField(
                        $"{i}: {item.name} [{TranslateLightType(item.lightType)}] {TranslateSource(item.source)}，切片 {item.firstSlice}-{item.firstSlice + item.sliceCount - 1}",
                        item.light,
                        typeof(Light),
                        true);
                }
            }
        }

        private static string TranslateStatus(string status)
        {
            switch (status)
            {
                case "Not rendered yet.":
                    return "尚未渲染。";
                case "Skipped by camera type or disabled settings.":
                    return "因相机类型或开关设置跳过。";
                case "Collected, no atlases rendered yet.":
                    return "已收集，尚未写入 Atlas。";
                case "No eligible ShadowCast lights after collection.":
                    return "收集后没有符合条件的 ShadowCast 灯光。";
                case "ShadowCast globals published.":
                    return "ShadowCast 全局数据已发布。";
                default:
                    return string.IsNullOrEmpty(status) ? "无" : status;
            }
        }

        private static string TranslateLightType(LightType type)
        {
            switch (type)
            {
                case LightType.Spot:
                    return "聚光";
                case LightType.Point:
                    return "点光";
                case LightType.Directional:
                    return "方向光";
                default:
                    return type.ToString();
            }
        }

        private static string TranslateSource(string source)
        {
            switch (source)
            {
                case "Visible":
                    return "自动收集";
                case "Explicit":
                    return "手动补充";
                default:
                    return string.IsNullOrEmpty(source) ? "未知来源" : source;
            }
        }
    }
}
