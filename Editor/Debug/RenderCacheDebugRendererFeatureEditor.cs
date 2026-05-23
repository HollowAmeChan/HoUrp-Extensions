using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.Debugging;
using UnityEditor;

namespace HoUrp.Extensions.Editor.Debugging
{
    [CustomEditor(typeof(RenderCacheDebugRendererFeature))]
    internal sealed class RenderCacheDebugRendererFeatureEditor : UnityEditor.Editor
    {
        private static readonly string[] EmptyLabels = { "None" };
        private static readonly string[] EmptyIds = { RenderCacheDebugRendererFeature.NoneViewId };

        private SerializedProperty enabledForGameView;
        private SerializedProperty enabledForSceneView;
        private SerializedProperty legacySelectedView;
        private SerializedProperty selectedDebugViewId;
        private string[] labels = EmptyLabels;
        private string[] ids = EmptyIds;

        private void OnEnable()
        {
            serializedObject.Update();
            enabledForGameView = serializedObject.FindProperty("enabledForGameView");
            enabledForSceneView = serializedObject.FindProperty("enabledForSceneView");
            legacySelectedView = serializedObject.FindProperty("selectedView");
            selectedDebugViewId = serializedObject.FindProperty("selectedDebugViewId");
            RebuildDebugViewOptions();
            MigrateLegacySelection();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(enabledForGameView);
            EditorGUILayout.PropertyField(enabledForSceneView);
            DrawDebugViewPopup();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDebugViewPopup()
        {
            int selectedIndex = FindSelectedIndex(selectedDebugViewId.stringValue);
            int nextIndex = EditorGUILayout.Popup("Debug View", selectedIndex, labels);
            selectedDebugViewId.stringValue = ids[nextIndex];
        }

        private void MigrateLegacySelection()
        {
            if (selectedDebugViewId == null
                || legacySelectedView == null
                || !string.IsNullOrEmpty(selectedDebugViewId.stringValue))
            {
                return;
            }

            string legacyName = legacySelectedView.enumNames[legacySelectedView.enumValueIndex];
            string migratedId = ResolveLegacyDebugViewId(legacyName);
            if (migratedId == RenderCacheDebugRendererFeature.NoneViewId)
            {
                return;
            }

            serializedObject.Update();
            selectedDebugViewId.stringValue = migratedId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private int FindSelectedIndex(string value)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == value)
                {
                    return i;
                }
            }

            return 0;
        }

        private void RebuildDebugViewOptions()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();
            List<string> labelList = new List<string>(registry.DebugViews.Count + 2)
            {
                "None",
                "AllRegistered"
            };
            List<string> idList = new List<string>(registry.DebugViews.Count + 2)
            {
                RenderCacheDebugRendererFeature.NoneViewId,
                RenderCacheDebugRendererFeature.AllRegisteredViewId
            };

            foreach (DebugViewDefinition debugView in registry.DebugViews.Definitions)
            {
                string id = debugView.Id.ToString();
                idList.Add(id);
                labelList.Add(FormatDebugViewLabel(debugView));
            }

            labels = labelList.ToArray();
            ids = idList.ToArray();
        }

        private static string FormatDebugViewLabel(DebugViewDefinition debugView)
        {
            string id = debugView.Id.ToString();
            if (string.IsNullOrEmpty(debugView.PreviewLabel))
            {
                return id;
            }

            return $"{debugView.PreviewLabel} ({id})";
        }

        private static string ResolveLegacyDebugViewId(string legacyName)
        {
            switch (legacyName)
            {
                case "AllRegistered":
                    return RenderCacheDebugRendererFeature.AllRegisteredViewId;
                case "Mask":
                    return HoUrpBuiltInNames.DebugViews.AovMask.ToString();
                case "ObjectId":
                    return HoUrpBuiltInNames.DebugViews.AovObjectId.ToString();
                case "LinearDepth":
                    return HoUrpBuiltInNames.DebugViews.AovLinearDepth.ToString();
                case "WorldNormal":
                    return HoUrpBuiltInNames.DebugViews.AovWorldNormal.ToString();
                case "Subject":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom0.ToString();
                case "Face":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom1.ToString();
                case "Hair":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom2.ToString();
                case "Eye":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom3.ToString();
                case "Accessory":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom4.ToString();
                case "Cloth":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom5.ToString();
                case "Prop":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom6.ToString();
                case "Reserved":
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom7.ToString();
                case "MaterialClass":
                    return HoUrpBuiltInNames.DebugViews.AovMaterialClass.ToString();
                case "SssProfile":
                    return HoUrpBuiltInNames.DebugViews.AovSssProfile.ToString();
                case "Thickness":
                    return HoUrpBuiltInNames.DebugViews.AovThickness.ToString();
                case "Curvature":
                    return HoUrpBuiltInNames.DebugViews.AovCurvature.ToString();
                case "MaterialCustom0":
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom0.ToString();
                case "MaterialCustom1":
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom1.ToString();
                case "MaterialCustom2":
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom2.ToString();
                case "MaterialCustom3":
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom3.ToString();
                case "Diffuse":
                    return HoUrpBuiltInNames.DebugViews.AovDiffuse.ToString();
                case "SssMask":
                    return HoUrpBuiltInNames.DebugViews.SssMask.ToString();
                case "SssPreparedSource":
                    return HoUrpBuiltInNames.DebugViews.SssSource.ToString();
                case "SssDiffusion":
                    return HoUrpBuiltInNames.DebugViews.SssDiffusion.ToString();
                case "SssCompositeWeight":
                    return HoUrpBuiltInNames.DebugViews.SssCompositeWeight.ToString();
                case "PostReceiver":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag1.ToString();
                case "Flag0Reserved":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag0.ToString();
                case "Flag2":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag2.ToString();
                case "Flag3":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag3.ToString();
                case "Flag4":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag4.ToString();
                case "Flag5":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag5.ToString();
                case "Flag6":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag6.ToString();
                case "Flag7":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag7.ToString();
                case "Flag8":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag8.ToString();
                case "Flag9":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag9.ToString();
                case "Flag10":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag10.ToString();
                case "Flag11":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag11.ToString();
                case "Flag12":
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag12.ToString();
                default:
                    return RenderCacheDebugRendererFeature.NoneViewId;
            }
        }
    }
}
