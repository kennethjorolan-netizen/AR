using System;
using UnityEditor;
using UnityEngine;

namespace CorePro.Editor
{
    [Serializable]
    public class GUIFoldoutPro
    {
        #region Variables
        private GUIStyle foldoutStyle;
        private GUIStyle backgroundStyle;
        private GUIStyle borderStyle;
        private GUIStyle statusLabelStyle;

        private const int IndentLevel = 2;
        private const string Enabled = "enabled";
        private const string Disabled = "disabled";

        private static Color _enabledColor = Color.green;
        private static Color _disabledColor = Color.gray;

        // Cache for textures and pixels
        private Texture2D cachedTexture;
        private Texture2D cachedBackgroundTexture;
        private Texture2D cachedBorderTexture;
        private Color cachedColor;
        private Color[] cachedPixels;
     
        
        #endregion

        /// <summary>
        /// Initialises GUI styles.
        /// </summary>
        private void InitializeStyles()
        {
            foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                normal = { textColor = UnityEditorPalette.HelpBoxText}
            };

            backgroundStyle = new GUIStyle
            {
                padding = new RectOffset(10, 10, 4, 4),
                normal = { background = GetCachedTexture(UnityEditorPalette.TabBackground, ref cachedBackgroundTexture) }
            };

            borderStyle = new GUIStyle
            {
                padding = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(5, 5, 5, 5),
                normal = { background = GetCachedTexture(UnityEditorPalette.DefaultBorder, ref cachedBorderTexture) }
            };

            statusLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = _disabledColor }
            };
        }

        public void UpdateColorEnabled(Color newColor)
        {
            _enabledColor = newColor;
        }

        private void UpdateStatusLabelStyle(bool isEnabled)
        {
            statusLabelStyle.normal.textColor = isEnabled ? _enabledColor : _disabledColor;
        }

        public bool Draw(ref bool foldout, SerializedProperty enabledProperty, string title, string prefsKey = null)
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical(borderStyle);
            EditorGUILayout.BeginVertical(backgroundStyle);
            EditorGUILayout.BeginHorizontal();

            // Drawing a checkbox
            if (enabledProperty != null)
            {
                enabledProperty.boolValue = EditorGUILayout.Toggle(enabledProperty.boolValue, GUILayout.Width(30));
                UpdateStatusLabelStyle(enabledProperty.boolValue);
            }
            else
            {
                GUILayout.Space(34);
            }

            // Foldout state management with EditorPrefs
            if (prefsKey != null && EditorPrefs.HasKey(prefsKey))
            {
                foldout = EditorPrefs.GetBool(prefsKey, foldout);
            }

            bool newFoldoutState = EditorGUILayout.Foldout(foldout, title, true, foldoutStyle);
            if (newFoldoutState != foldout)
            {
                foldout = newFoldoutState;
                if (prefsKey != null)
                {
                    EditorPrefs.SetBool(prefsKey, foldout);
                }
            }

            // Status display (enabled/disabled)
            if (enabledProperty != null)
            {
                string statusText = enabledProperty.boolValue ? Enabled : Disabled;
                EditorGUILayout.LabelField(statusText, statusLabelStyle, GUILayout.MaxWidth(45));
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUI.indentLevel += IndentLevel;
            }
            else
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            return foldout;
        }

        public void EndFoldout(bool foldout = false)
        {
            EditorGUI.indentLevel -= IndentLevel;
            if (foldout)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }

        private Texture2D GetCachedTexture(Color col, ref Texture2D texture)
        {
            if (texture != null && cachedColor == col && cachedPixels != null)
            {
                return texture;
            }

            // Create new texture and cache its color and pixels
            cachedColor = col;
            int width = 2, height = 2;

            // Cache pixels
            if (cachedPixels == null || cachedPixels.Length != width * height)
            {
                cachedPixels = new Color[width * height];
            }

            for (int i = 0; i < cachedPixels.Length; i++)
            {
                cachedPixels[i] = col;
            }

            // Create and set texture
            if (texture == null)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }

            texture.SetPixels(cachedPixels);
            texture.Apply();

            return texture;
        }

        public void Dispose()
        {
            // Destroy cached textures if they exist
            if (cachedTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(cachedTexture);
                cachedTexture = null;
            }

            if (cachedBackgroundTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(cachedBackgroundTexture);
                cachedBackgroundTexture = null;
            }

            if (cachedBorderTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(cachedBorderTexture);
                cachedBorderTexture = null;
            }

            // Reset styles
            foldoutStyle = null;
            backgroundStyle = null;
            borderStyle = null;
            statusLabelStyle = null;
            cachedPixels = null;
        }
    }
}