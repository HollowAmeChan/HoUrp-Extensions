using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HoUrp.Extensions.Editor.PostProcess
{
    internal static class PostLayerListDrawer
    {
        public static ReorderableList CreateList(SerializedObject serializedObject, SerializedProperty property, string header)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
            list.elementHeightCallback = index => GetElementHeight(property, index);
            list.drawElementCallback = (rect, index, active, focused) => DrawElement(rect, property.GetArrayElementAtIndex(index), index);
            list.onAddCallback = _ =>
            {
                int index = property.arraySize;
                property.arraySize++;
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                SetDefaultElement(element, index);
                serializedObject.ApplyModifiedProperties();
            };

            return list;
        }

        private static float GetElementHeight(SerializedProperty property, int index)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2.0f;
            if (!element.isExpanded)
            {
                return height;
            }

            SerializedProperty iterator = element.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }

            return height;
        }

        private static void DrawElement(Rect rect, SerializedProperty element, int index)
        {
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            Rect header = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty enabled = element.FindPropertyRelative("enabled");
            SerializedProperty name = element.FindPropertyRelative("name");

            Rect foldoutRect = new Rect(header.x, header.y, 14.0f, header.height);
            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, GUIContent.none, true);

            Rect enabledRect = new Rect(header.x + 18.0f, header.y, 18.0f, header.height);
            if (enabled != null)
            {
                enabled.boolValue = EditorGUI.Toggle(enabledRect, enabled.boolValue);
            }

            Rect labelRect = new Rect(header.x + 42.0f, header.y, header.width - 42.0f, header.height);
            string displayName = name != null && !string.IsNullOrWhiteSpace(name.stringValue)
                ? name.stringValue
                : "Layer";
            EditorGUI.LabelField(labelRect, index.ToString("00") + "  " + displayName);

            if (!element.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            float y = header.yMax + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty iterator = element.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect fieldRect = new Rect(rect.x, y, rect.width, height);
                EditorGUI.PropertyField(fieldRect, iterator, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }

        private static void SetDefaultElement(SerializedProperty element, int index)
        {
            SerializedProperty enabled = element.FindPropertyRelative("enabled");
            SerializedProperty name = element.FindPropertyRelative("name");
            if (enabled != null)
            {
                enabled.boolValue = true;
            }

            if (name != null)
            {
                name.stringValue = "Layer " + index;
            }
        }
    }
}
