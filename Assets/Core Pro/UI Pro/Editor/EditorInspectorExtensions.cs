using UnityEditor;
using UnityEngine;

namespace CorePro.Editor
{
    public static class EditorInspectorExtensions
    {
        public static void DrawPropertyWithToggle(SerializedProperty colorProperty, SerializedProperty toggleProperty, string label)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Drawing a checkbox (toggle) or standard spacing when the checkbox is not visible
            if (toggleProperty != null)
            {
                EditorGUILayout.PropertyField(toggleProperty, GUIContent.none, GUILayout.Width(25));
            }
            else
            {
                
                GUILayout.Space(31);
            }

            if (colorProperty != null)
                EditorGUILayout.PropertyField(colorProperty, new GUIContent(label));

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawCustomToggle(SerializedProperty toggleProperty, string label, SerializedProperty colorProperty = null, float spaceBetween = 5f,
            float spaceAfterProp = 5f, float spaceBeforeProp = 5f)
        {
            if (spaceBeforeProp != 0)
                EditorGUILayout.Space(spaceBeforeProp);
            
            EditorGUILayout.BeginHorizontal();

            // Drawing a checkbox (toggle) or standard spacing when the checkbox is not visible
            if (toggleProperty != null)
            {
                EditorGUILayout.PropertyField(toggleProperty, GUIContent.none, GUILayout.Width(20));
            }
            else
            {
                GUILayout.Space(23);
            }

            // Optional space between checkbox and label
            if (spaceBetween > 0)
            {
                GUILayout.Space(spaceBetween);
            }

            // Drawing a label
            EditorGUILayout.LabelField(label, EditorStyles.label);

            // Optional colour field
            if (colorProperty != null)
            {
                EditorGUILayout.PropertyField(colorProperty, GUIContent.none);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(spaceAfterProp);
        }
    }
}