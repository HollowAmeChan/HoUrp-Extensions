using HoUrp.Extensions.Features;
using UnityEditor;
using UnityEditorInternal;

namespace HoUrp.Extensions.Editor.PostProcess
{
    [CustomEditor(typeof(ScreenPostPrototypeRendererFeature))]
    internal sealed class ScreenPostPrototypeRendererFeatureEditor : UnityEditor.Editor
    {
        private ReorderableList layers;

        private void OnEnable()
        {
            layers = PostLayerListDrawer.CreateList(serializedObject, serializedObject.FindProperty("layers"), "ScreenPost Layers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledForGameView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabledForSceneView"));
            layers.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
