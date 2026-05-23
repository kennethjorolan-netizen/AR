using CorePro.ButtonPro;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CorePro.Editor
{
    [CustomEditor(typeof(ButtonPro.ButtonPro), true)]
    [CanEditMultipleObjects]
    public class ButtonProEditor : UnityEditor.Editor
    {
        #region Variables

        private SerializedProperty buttonPressStateAt;
        private ButtonPro.ButtonPro buttonPro;

        // State colors image
        private SerializedProperty useStateColorsForImage;
        private SerializedProperty useIndividualImageColors;
        private SerializedProperty imageColors;

        // State colors Text
        private SerializedProperty useStateColorsForText;
        private SerializedProperty useIndividualTextColors;
        private SerializedProperty textColors;

        // State Sprites
        private SerializedProperty useStateSprites;
        private SerializedProperty useIndividualSprites;
        private SerializedProperty disableObjOnEmptySprite;
        private SerializedProperty stateSprites;

        // State Texts
        private SerializedProperty useStateTexts;
        private SerializedProperty useIndividualTexts;
        private SerializedProperty disableObjOnEmptyText;
        private SerializedProperty stateTexts;
        
        // State Group objects
        private SerializedProperty useStateObjectGroup;
        private SerializedProperty stateObjectsGroups;

        // Anim sprites 
        private SerializedProperty usePressAnimation;
        private SerializedProperty pressAnimDuration;
        private SerializedProperty pressAnimScale;

        // Groups
        private SerializedProperty useActivationGroups;
        private SerializedProperty enableWhenInteractable;
        private SerializedProperty enableWhenNotInteractable;

        // Events
        private SerializedProperty onClick;
        private SerializedProperty onClickDown;
        private SerializedProperty onHighlighted;
        private SerializedProperty onInteractable;
        private SerializedProperty onNoInteractable;
        private SerializedProperty onInteractableChanged;

        // Lists
        private SerializedProperty images;
        private SerializedProperty texts;
        private ReorderableList reorderableTextsListPro;
        private GUIListPro ListProImages;
        private GUIListPro ListProTexts;
        private bool imageListIsExpanded;
        private bool textListIsExpanded;

        // Foldout states
        private bool showImageStateColors = false;
        private bool showTextStateColors = false;
        private bool showSpriteState = false;
        private bool showTextsState = false;
        private bool showActivationGroups = false;
        private bool showEvents = false;
        private bool showDebug = false;
        private bool showTooltip = false;
#if DOTWEEN
        private bool showAnim = false;
#endif
        
        // Tooltip
        private SerializedProperty useTooltip;
        private SerializedProperty useWhenNotInteractable;
        private SerializedProperty tooltipObject;
        private SerializedProperty tooltipText;
        private SerializedProperty stateTooltipTexts;

        [SerializeField] private GUIFoldoutPro guiFoldoutPro;

        // Temp local variables
        private Color tempColor;
        private Sprite tempSprite;
        private string tempString;
        
        [FormerlySerializedAs("PropSetBackground")] [SerializeField]
        private GUIStyle propSetBackground;

        private SerializedProperty propSetTemp;
        private SerializedProperty normalPropTemp;
        
        #endregion

        private void OnEnable()
        {
            buttonPro = (ButtonPro.ButtonPro)target;

            if (propSetBackground == null && (EditorStyles.helpBox != null))
            {
                propSetBackground = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(0, 20, 0, 0),
                    alignment = TextAnchor.MiddleCenter, 
                    fontStyle = FontStyle.Bold, 
                    fontSize = 12 
                };
            }

            if (guiFoldoutPro == null)
            {
                guiFoldoutPro = new GUIFoldoutPro();
                guiFoldoutPro.UpdateColorEnabled(Color.green);
            }

            AssignSerializedProperties();

            ListProImages = new GUIListPro(buttonPro.images, serializedObject, serializedObject.FindProperty("images"), "Images");
            ListProTexts = new GUIListPro(buttonPro.texts, serializedObject, serializedObject.FindProperty("texts"), "Texts");
        }

        private void OnDisable()
        {
            // CustomFoldout resource cleaning
            if (guiFoldoutPro != null)
            {
                guiFoldoutPro.Dispose();
                guiFoldoutPro = null;
            }
        }

        private void AssignSerializedProperties()
        {
            // Use reflection to assign all SerializedProperties
            var fields = GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property != null)
                        field.SetValue(this, property);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBasics();
            DrawReferencesLists();
            serializedObject.ApplyModifiedProperties();

            DrawStateColorsImage();
            DrawStateColorsTexts();
            DrawStateSprites();
            DrawStateTexts();
            DrawStateObjetsGroup();
            DrawTooltip();
            DrawGroups();
            DrawAnimations();
            DrawEvents();
            DrawDebug();
            
            serializedObject.ApplyModifiedProperties();
        }

        #region DrawMains

        private void DrawBasics()
        {
            // Display ButtonState as a label
            EditorGUILayout.LabelField("Current State", buttonPro.currentState.ToString());

            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();

            bool newInteractableValue = EditorGUILayout.Toggle("Interactable", buttonPro.Interactable);
            EditorGUILayout.PropertyField(buttonPressStateAt, new GUIContent("Invoke OnClick at"));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(buttonPro, "Change Interactable");
                buttonPro.Interactable = newInteractableValue;
                EditorUtility.SetDirty(buttonPro);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawReferencesLists()
        {
            EditorGUILayout.Space(5);
            ListProImages.DoLayoutList();
            EditorGUILayout.Space(5);
            ListProTexts.DoLayoutList();
            EditorGUILayout.Space(5);
        }


        private void DrawStateColorsImage()
        {
            if (guiFoldoutPro.Draw(ref showImageStateColors, useStateColorsForImage, "Colors Image", "ButtonPro StateColorsImage"))
            {
                ((ButtonPro.ButtonPro)target).ValidateImageColorsList();

                if (!IsArrayElementValid(images, 0))
                {
                    guiFoldoutPro.EndFoldout(showImageStateColors);
                    return;
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(propSetBackground);
                EditorInspectorExtensions.DrawCustomToggle(useIndividualImageColors, "Use Individual Image Colors", null, 5f,5,5);
                EditorGUILayout.EndVertical(); 

                if (useIndividualImageColors.boolValue)
                {
                    for (int i = 0; i < imageColors.arraySize; i++)
                    {
                        // Skip invalid entries
                        if (i >= buttonPro.images.Count || buttonPro.images[i] == null)
                            continue; 

                        DrawSection(imageColors, i, "Image: " + $"{buttonPro.images[i].name}");
                        TrySyncNormalPropToImg(normalPropTemp, buttonPro.images[i]);
                    }
                }
                else if (imageColors.arraySize > 0)
                {
                    DrawSection(imageColors,0, "All images");
                    TrySyncNormalPropToImg(normalPropTemp, buttonPro.images[0]);
                }

                guiFoldoutPro.EndFoldout(showImageStateColors);
            }
            
            void TrySyncNormalPropToImg(SerializedProperty normalProp, Image image)
            {
                if (Application.isPlaying == false && EditorGUI.EndChangeCheck()) 
                {
                    // if the colors are NOT the same then update image color
                    if (normalProp.colorValue != image.color)
                    {
                        Undo.RecordObject(image, "Change ButtonPro Image Color");
                        image.color = normalProp.colorValue;
                        EditorUtility.SetDirty(image);
                    }
                }
            } 
        }


        private void DrawStateColorsTexts()
        {
            if (guiFoldoutPro.Draw(ref showTextStateColors, useStateColorsForText, "Colors Text", "ButtonPro StateColorsText"))
            {
                ((ButtonPro.ButtonPro)target).ValidateTextColorsList();

                // We check whether the first element is valid
                if (!IsArrayElementValid(texts, 0))
                {
                    guiFoldoutPro.EndFoldout(showTextStateColors);
                    return;
                }
                
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(propSetBackground);
                EditorInspectorExtensions.DrawCustomToggle(useIndividualTextColors, "Use Individual Text Colors", null, 5f);
                EditorGUILayout.EndVertical();

                if (useIndividualTextColors.boolValue)
                {
                    for (int i = 0; i < textColors.arraySize; i++)
                    {
                        // Skip invalid entries
                        if (i >= buttonPro.texts.Count || buttonPro.texts[i] == null)
                            continue; 

                        DrawSection(textColors, i, $"Text: {buttonPro.texts[i].name}");
                        TrySyncNormalPropToText(normalPropTemp, buttonPro.texts[i]);
                    }
                }
                else if (textColors.arraySize > 0)
                {
                    DrawSection(textColors,0,"All texts");
                    TrySyncNormalPropToText(normalPropTemp, buttonPro.texts[0]);
                }

                guiFoldoutPro.EndFoldout(showTextStateColors);
            }
            
            void TrySyncNormalPropToText(SerializedProperty normalProp, TextMeshProUGUI text)
            {
                if (Application.isPlaying == false && EditorGUI.EndChangeCheck())
                {
                    if (normalProp.colorValue !=text.color)
                    {
                        Undo.RecordObject(text, "Change ButtonPro Image Color");
                        text.color = normalProp.colorValue;
                        EditorUtility.SetDirty(text);
                    }
                }
            } 
        }


        private void DrawStateSprites()
        {
            if (guiFoldoutPro.Draw(ref showSpriteState, useStateSprites, "Sprites", "ButtonPro StateSprites"))
            {
                ((ButtonPro.ButtonPro)target).ValidateStateSpriteList();

                if (!IsArrayElementValid(images, 0))
                {
                    guiFoldoutPro.EndFoldout(showSpriteState);
                    return;
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(propSetBackground);
                EditorInspectorExtensions.DrawCustomToggle(useIndividualSprites, "Use Individual Sprites", null, 5f, 0);
                EditorInspectorExtensions.DrawCustomToggle(disableObjOnEmptySprite, "Disable object if no sprite is set for the used state", null, 5f,5,0);
                EditorGUILayout.EndVertical();

                if (useIndividualSprites.boolValue)
                {
                    for (int i = 0; i < stateSprites.arraySize; i++)
                    {
                        // Skip invalid entries
                        if (i >= buttonPro.stateSprites.Count || buttonPro.images[i] == null)
                            continue; 

                        DrawSection(stateSprites, i, $"Image: {buttonPro.images[i].name}");
                        TrySyncSpriteToImg(normalPropTemp, i);
                    }
                }
                else if (stateSprites.arraySize > 0)
                {
                    DrawSection(stateSprites, 0, "All images");
                    TrySyncSpriteToImg(normalPropTemp, 0);
                }
                
                guiFoldoutPro.EndFoldout(showSpriteState);
            }
            
            void TrySyncSpriteToImg(SerializedProperty normalProp, int i)
            {
                if (Application.isPlaying == false && EditorGUI.EndChangeCheck())
                {
                    if (normalProp.objectReferenceValue != buttonPro.images[i].sprite)
                    {
                        Undo.RecordObject(buttonPro.texts[i], "Change ButtonPro Image Sprite");
                           
                        buttonPro.images[i].sprite = (Sprite)normalProp.objectReferenceValue;

                        if (disableObjOnEmptySprite.boolValue)
                        {
                            if (buttonPro.images[i].sprite == null)
                                buttonPro.images[i].gameObject.SetActive(false);
                            else
                                buttonPro.images[i].gameObject.SetActive(true);
                        }

                        // Apply changes to SerializedObject
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(buttonPro.texts[i]);
                    }
                }
            }
        }


        private void DrawStateTexts()
        {
            if (guiFoldoutPro.Draw(ref showTextsState, useStateTexts, "Texts", "ButtonPro StateTexts"))
            {
                ((ButtonPro.ButtonPro)target).ValidateStateTextList();

                if (!IsArrayElementValid(texts, 0))
                {
                    guiFoldoutPro.EndFoldout(showTextsState);
                    return;
                }
                
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(propSetBackground);
                EditorInspectorExtensions.DrawCustomToggle(useIndividualTexts, "Use Individual Texts", null, 5f, 0);
                EditorInspectorExtensions.DrawCustomToggle(disableObjOnEmptyText, "Disable object if no text is set for the used state", null, 5f);
                EditorGUILayout.EndVertical();

                if (useIndividualTexts.boolValue)
                {
                    for (int i = 0; i < stateTexts.arraySize; i++)
                    {
                        // Skip invalid entries
                        if (i >= buttonPro.stateTexts.Count || buttonPro.texts[i] == null)
                            continue; 

                        DrawSection(stateTexts, i, $"Texts: {buttonPro.texts[i].name}");
                        TrySync(normalPropTemp, i);
                    }
                }
                else if (stateTexts.arraySize > 0)
                {
                    DrawSection(stateTexts, 0, "All texts");
                    TrySync(normalPropTemp, 0);
                }

                guiFoldoutPro.EndFoldout(showTextsState);
            }

            void TrySync(SerializedProperty normalProp ,int i)
            {
                if (Application.isPlaying == false && EditorGUI.EndChangeCheck())
                {
                    if (normalProp != null && normalProp.stringValue != buttonPro.texts[i].text)
                    {
                        Undo.RecordObject(buttonPro.texts[i], "Change ButtonPro Text");
                        buttonPro.texts[i].text = normalProp.stringValue;

                        if (disableObjOnEmptyText.boolValue)
                        {
                            if (string.IsNullOrWhiteSpace(buttonPro.texts[i].text))
                                buttonPro.texts[i].gameObject.SetActive(false);
                            else
                                buttonPro.texts[i].gameObject.SetActive(true);
                        }

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(buttonPro.texts[i]);
                    }
                }
            }
        }

        private void DrawStateObjetsGroup()
        {
            if (guiFoldoutPro.Draw(ref showTextsState, useStateObjectGroup, "State Groups", "ButtonPro StateObjects"))
            {
                // Update list of object groups
                ((ButtonPro.ButtonPro)target).ValidateObjectsList();

                EditorGUILayout.Space(10);
                DrawCustomHelpBox("Each group will be switched according to the state");
                EditorGUILayout.Space(10);

                // Drawing groups for each state
                EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("normal"), stateObjectsGroups.FindPropertyRelative("useNormal"), "Normal");
                EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("highlighted"), stateObjectsGroups.FindPropertyRelative("useHighlighted"),
                    "Highlighted");
                EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("press"), stateObjectsGroups.FindPropertyRelative("usePress"), "Pressed");
                EditorInspectorExtensions.DrawPropertyWithToggle(stateObjectsGroups.FindPropertyRelative("inactive"), stateObjectsGroups.FindPropertyRelative("useInactive"),
                    "Inactive");

                EditorGUILayout.Space(10);

                guiFoldoutPro.EndFoldout(showTextsState);
            }
        }



        private void DrawAnimations()
        {
#if DOTWEEN
            if (guiFoldoutPro.Draw(ref showAnim, usePressAnimation, "Animations"))
            {
                EditorGUILayout.PropertyField(pressAnimDuration);
                EditorGUILayout.PropertyField(pressAnimScale);
                guiFoldoutPro.EndFoldout(showAnim);
            }
#endif
        }

        private void DrawGroups()
        {
            if (guiFoldoutPro.Draw(ref showActivationGroups, useActivationGroups, "Interactable Groups", "ButtonPro Interactable Groups"))
            {
                EditorGUILayout.PropertyField(enableWhenInteractable, true);
                EditorGUILayout.PropertyField(enableWhenNotInteractable, true);
                guiFoldoutPro.EndFoldout(showActivationGroups);
            }
        }

        private void DrawEvents()
        {
            if (guiFoldoutPro.Draw(ref showEvents, null, "Events", "ButtonPro Events"))
            {
                EditorGUILayout.PropertyField(onClick);
                EditorGUILayout.PropertyField(onClickDown);
                EditorGUILayout.PropertyField(onHighlighted);
                EditorGUILayout.PropertyField(onInteractable);
                EditorGUILayout.PropertyField(onNoInteractable);
                EditorGUILayout.PropertyField(onInteractableChanged);
                guiFoldoutPro.EndFoldout(showEvents);
            }
        }

        private void DrawDebug()
        {
            if (guiFoldoutPro.Draw(ref showDebug, null, "Debug", "ButtonPro Debug"))
            {
                if (GUILayout.Button("Set Normal State"))
                {
                    buttonPro.SetState(ButtonPro.ButtonPro.ButtonState.Normal);
                }

                if (GUILayout.Button("Set Highlighted State"))
                {
                    buttonPro.SetState(ButtonPro.ButtonPro.ButtonState.Highlighted);
                }

                if (GUILayout.Button("Set Pressed State"))
                {
                    buttonPro.SetState(ButtonPro.ButtonPro.ButtonState.Pressed);
                }

                if (GUILayout.Button("Set Disabled State"))
                {
                    buttonPro.SetState(ButtonPro.ButtonPro.ButtonState.Inactive);
                }

                guiFoldoutPro.EndFoldout(showDebug);
            }
        }
        
        private void DrawTooltip()
        {
            if (guiFoldoutPro.Draw(ref showTooltip, useTooltip, "Tooltip", "ButtonPro Tooltip"))
            {
                ((ButtonPro.ButtonPro)target).ValidateTooltip();

                if (!IsArrayElementValid(texts, 0))
                {
                    guiFoldoutPro.EndFoldout(showTextsState);
                    return;
                }
                
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(propSetBackground);
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(tooltipObject, new GUIContent("Tooltip Object"));
                EditorGUILayout.PropertyField(tooltipText, new GUIContent("Tooltip Text"));
                EditorGUILayout.Space(2);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(propSetBackground);
                EditorGUILayout.Space(2);
                DrawPropertySetIndividual(stateTooltipTexts);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);

                guiFoldoutPro.EndFoldout(showTooltip);
            }
        }
        
        #endregion

        #region Other

        void DrawSection (SerializedProperty arrayProperty, int index, string title)
        {   
            if (Application.isPlaying == false)
                EditorGUI.BeginChangeCheck();
                    
            propSetTemp = arrayProperty.GetArrayElementAtIndex(index);
            normalPropTemp = propSetTemp.FindPropertyRelative("normal");
                    
            DrawPropertySetIndividual(propSetTemp, title);
        } 

        private void DrawPropertySetIndividual(SerializedProperty propertySet, string title = null, bool disabled = false, string info = "")
        {
            if (propertySet == null)
                return;
            
            if (title != null)
            {
                // Define a style for the border
                EditorGUILayout.BeginVertical(propSetBackground); // Start the bordered section
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel); // Display the title
                EditorGUILayout.Space(2);
            }

            if (disabled)
            {
                if (string.IsNullOrWhiteSpace(info) == false)
                    EditorGUILayout.LabelField(info, EditorStyles.helpBox);

                GUI.enabled = false;
            }

            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("normal"), null, "Normal");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("highlighted"), propertySet.FindPropertyRelative("useHighlighted"), "Highlighted");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("press"), propertySet.FindPropertyRelative("usePress"), "Press");
            EditorInspectorExtensions.DrawPropertyWithToggle(propertySet.FindPropertyRelative("inactive"), propertySet.FindPropertyRelative("useInactive"), "Inactive");

            if (disabled)
                GUI.enabled = true;
            
            EditorGUILayout.Space(2);

            // End the bordered section if was title
            if (title != null)
                EditorGUILayout.EndVertical(); 
        }
        


        private bool IsArrayElementValid(SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null || arrayProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("The reference list is empty or not assigned.");
                return false;
            }

            if (index < 0 || index >= arrayProperty.arraySize)
            {
                EditorGUILayout.LabelField($"Invalid index: {index}. Ensure the list contains sufficient elements.");
                return false;
            }

            SerializedProperty element = arrayProperty.GetArrayElementAtIndex(index);
            if (element == null || element.objectReferenceValue == null)
            {
                EditorGUILayout.LabelField($"Element at index {index} is null. Please assign a valid reference.");
                return false;
            }

            return true;
        }
        
        private void DrawCustomHelpBox(string message)
        {
            // Define a custom style
            GUIStyle largeTextStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };

            // Draw the message
            EditorGUILayout.LabelField(message, largeTextStyle);
        }



        #endregion

    }
}