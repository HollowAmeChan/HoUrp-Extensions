using HoUrp.Extensions.Features;
using UnityEditor;
using UnityEditorInternal;

namespace HoUrp.Extensions.Editor.PostProcess
{
    [CustomEditor(typeof(ImagePostPrototypeRendererFeature))]
    internal sealed class ImagePostPrototypeRendererFeatureEditor : UnityEditor.Editor
    {
        private ReorderableList filters;

        private void OnEnable()
        {
            filters = PostLayerListDrawer.CreateList(serializedObject, serializedObject.FindProperty("filters"), "ImagePost Filters");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledForGameView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledForSceneView"));
            filters.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
