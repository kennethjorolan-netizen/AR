using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CorePro.Editor
{
    public class GUIListPro
    {
        public ElementHeightCallbackDelegate ElementHeightCallback;
        private int id = -1;
        private readonly List<int> SelectionList = new List<int>();
        private float dragOffset;
        private SerializedObject serializedObject;
        private SerializedProperty elements;
        private string propertyPath = string.Empty;
        private IList elementList;
        private float draggedY;
        private bool dragging;
        private List<int> nonDragTargetIndices;
        private bool scheduleRemove;
        private bool hasPropertyDrawer;
        private int cacheCount = 0;
        private bool propertyCacheValid = false;
        private PropertyCacheEntry[] propertyCache = new PropertyCacheEntry[0];
        private static List<string> outdatedProperties = new List<string>();
        private static List<WeakReference<GUIListPro>> Instances = new List<WeakReference<GUIListPro>>();
        private float elementHeight = 21f;
        private float headerHeight = 20f;
        private float footerHeight = 20f;

        private int elementsCount;
        private int smallerArraySize;
        private float lastHeight = -1f;
        private int recursionCounter = 0;
        private GUIListProSlideGroup slideGroup = new GUIListProSlideGroup(10f);
        private string title;
        private bool scheduleGUIChanged;

        private GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to the list");
        private GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from the list");
        private GUIStyle _draggingHandle;
        private GUIStyle _headerBackground;
        private GUIStyle _emptyHeaderBackground;
        private GUIStyle _footerBackground;
        private GUIStyle _boxBackground;
        private GUIStyle _preButton;
        private GUIStyle _elementBackground;
        private GUIStyle DraggingHandle => _draggingHandle ??= new GUIStyle("RL DragHandle");
        private GUIStyle HeaderBackground => _headerBackground ??= new GUIStyle("RL Header");
        private GUIStyle EmptyHeaderBackground => _emptyHeaderBackground ??= new GUIStyle("RL Empty Header");
        private GUIStyle FooterBackground => _footerBackground ??= new GUIStyle("RL Footer");
        private GUIStyle BoxBackground => _boxBackground ??= new GUIStyle("RL Background");
        private GUIStyle PreButton => _preButton ??= new GUIStyle("RL FooterButton");
        private GUIStyle ElementBackground => _elementBackground ??= new GUIStyle("RL Element");

        private static readonly GUIContent listIsEmpty = EditorGUIUtility.TrTextContent("List is Empty");
        private static readonly string undoAdd = "Add Element To Array";
        private static readonly string undoRemove = "Remove Element From Array";
        private static readonly string undoMove = "Reorder Element In Array";
        private static readonly Rect infinityRect = new Rect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);
        private bool listIsExpanded = false;

        
        private readonly Dictionary<int, Rect> rectCache = new Dictionary<int, Rect>();
        private bool rectCacheInvalidated = true; 
        private Rect draggingRect;
        private string sessionKey;
       
        private void InvalidateRectCache()
        {
            rectCache.Clear();
            rectCacheInvalidated = true;
        }
        
        private static float ElementPadding(float height) => (double)height == 0.0 ? 0.0f : 2f;


        #region Delegates

        public delegate float ElementHeightCallbackDelegate(int index);

        #endregion

        private struct PropertyCacheEntry
        {
            public SerializedProperty property;
            public float height;
            public float offset;
            public int controlCount;

            public bool Set(SerializedProperty newProperty, float newHeight, float newOffset)
            {
                bool flag = height != (double)newHeight;
                this.property = newProperty;
                this.height = newHeight;
                this.offset = newOffset;
                return flag;
            }
        }

        public void InvalidateForGUI()
        {
            SerializedObject _serializedObject = serializedObject;
            if (_serializedObject == null || !_serializedObject.isEditingMultipleObjects)
            {
                InvalidateCache();
            }
            else
            {
                InvalidateExistingListCaches();
                EditorApplication.delayCall -=
                    (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall,
                        new EditorApplication.CallbackFunction(() => { }));

                EditorApplication.delayCall +=
                    (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall,
                        new EditorApplication.CallbackFunction(() => { }));
            }
        }

        #region GUIListPro

        public static void InvalidateExistingListCaches()
        {
            Instances.ForEach((Action<WeakReference<GUIListPro>>)(list =>
            {
                GUIListPro target;
                if (!list.TryGetTarget(out target))
                    return;
                target.InvalidateCache();
            }));
        }

        public GUIListPro(IList elementsList, SerializedObject serializedObject, SerializedProperty elements, string title)
        {
            InitList(serializedObject, elements, elementsList, title);
        }

        #endregion

        private void InitList(SerializedObject serializedObject, SerializedProperty elements, IList elementList, string newTitle)
        {
            //this.id = GUIUtility.GetPermanentControlID();
            this.serializedObject = serializedObject;
            this.elements = elements;
            this.elementList = elementList;
            dragging = false;
            title = newTitle;
            
            if (this.elements != null)
            {
                propertyPath = this.elements.propertyPath;
                if (!this.elements.isArray)
                    Debug.LogError((object)"Input elements should be an Array SerializedProperty");
            }
            
            sessionKey = $"{propertyPath}_{newTitle}_IsExpanded";
            listIsExpanded = SessionState.GetBool(sessionKey, false);

            Instances.Add(new WeakReference<GUIListPro>(this));
        }

        private void SaveListExpansionState()
        {
            SessionState.SetBool(sessionKey, listIsExpanded);
        }

        private SerializedProperty serializedProperty
        {
            get => this.elements;
            set
            {
                elements = value;
                serializedObject = elements.serializedObject;
                propertyPath = elements.propertyPath;
            }
        }

        public IList list
        {
            get => this.elementList;
            set => elementList = value;
        }

        private int index
        {
            get => SelectionList.Count > 0 ? SelectionList[0] : count - 1;
            set => Select(value);
        }

        public ReadOnlyCollection<int> selectedIndices
        {
            get => new ReadOnlyCollection<int>((IList<int>)SelectionList);
        }


        private float HeaderHeight => Mathf.Max(headerHeight, 2f);

        private float listElementTopPadding => headerHeight > 5.0 ? 4f : 1f;

        private bool useCulling
        {
            get => UnityEngine.GUI.matrix.rotation == Quaternion.identity && UnityEngine.GUI.matrix.lossyScale == Vector3.one;
        }

        private void TryOverrideElementHeightWithPropertyDrawer(SerializedProperty property, ref float height)
        {
            if (!hasPropertyDrawer)
                return;
            try
            {
                height = 15;
            }
            catch
            {
                height = int.MinValue;
                --elementsCount;
            }
        }

        private void CacheIfNeeded()
        {
            if (IsOverMaxMultiEditLimit || propertyCacheValid)
                return;

            propertyCacheValid = true;
            ++cacheCount;
            Array.Resize(ref propertyCache, count);
            SerializedProperty property1 = (SerializedProperty)null;
            float offset1 = 0.0f;
            float height;
            if (elementsCount > 0)
            {
                ElementHeightCallbackDelegate elementHeightCallback = ElementHeightCallback;
                height = elementHeightCallback != null ? elementHeightCallback(0) : elementHeight;
                if (elements != null)
                {
                    property1 = elements.GetArrayElementAtIndex(0);
                    TryOverrideElementHeightWithPropertyDrawer(property1, ref height);
                }

                if (height > int.MinValue)
                    scheduleGUIChanged |= propertyCache[0].Set(property1,
                        height + ElementPadding(height), offset1);
            }

            for (int index = 1; index < elementsCount; ++index)
            {
                PropertyCacheEntry propertyCacheEntry = propertyCache[index - 1];
                SerializedProperty property2 = (SerializedProperty)null;
                ElementHeightCallbackDelegate elementHeightCallback = ElementHeightCallback;
                height = elementHeightCallback != null ? elementHeightCallback(index) : elementHeight;
                float offset2 = propertyCacheEntry.offset + propertyCacheEntry.height;
                if (elements != null)
                {
                    property2 = propertyCacheEntry.property.Copy();
                    property2.Next(false);
                    TryOverrideElementHeightWithPropertyDrawer(property2, ref height);
                }

                if (height > int.MinValue)
                    scheduleGUIChanged |= propertyCache[index].Set(property2,
                        height + ElementPadding(height), offset2);
            }
        }

        public void InvalidateCache()
        {
            cacheCount = 0;
            propertyCacheValid = false;
            slideGroup.Reset(); 
        }

        public void InvalidateCacheRecursive()
        {
            InvalidateCache();
        }

        public void Select(int index, bool append = false)
        {
            int num = SelectionList.BinarySearch(index);
            if (num >= 0 && (append || SelectionList.Count <= 1))
                return;
            if (!append)
            {
                SelectionList.Clear();
                SelectionList.Add(index);
            }
            else
                SelectionList.Insert(~num, index);
        }

        public void SelectRange(int indexFrom, int indexTo)
        {
            SelectionList.Clear();
            for (int index = Mathf.Min(indexFrom, indexTo); index <= Mathf.Max(indexFrom, indexTo); ++index)
                SelectionList.Add(index);
        }

        public bool IsSelected(int index) => SelectionList.BinarySearch(index) >= 0;

        public void Deselect(int index)
        {
            int index1 = SelectionList.BinarySearch(index);
            if (index1 < 0)
                return;
            SelectionList.RemoveAt(index1);
        }

        #region Get
        private Rect GetContentRect(Rect rect)
        {
            Rect contentRect = rect;
            contentRect.xMin += 20f;

            if (hasPropertyDrawer)
                contentRect.xMin += 8f;
            contentRect.xMax -= 6f;
            return contentRect;
        }

        private float GetElementYOffset(int index) => GetElementYOffset(index, -1);

        private float GetElementYOffset(int index, int skipIndex)
        {
            if (propertyCache.Length <= index)
                return 0.0f;
            float num = 0.0f;
            if (skipIndex >= 0 && skipIndex < index)
                num = propertyCache[skipIndex].height;
            return propertyCache[index].offset - num;
        }

        private float GetElementHeight(int index)
        {
            return propertyCache.Length <= index ? 0.0f : propertyCache[index].height;
        }

        private bool IsOverMaxMultiEditLimit
        {
            get
            {
                return elements != null &&
                       smallerArraySize > elements.serializedObject.maxArraySizeForMultiEditing &&
                       elements.serializedObject.isEditingMultipleObjects;
            }
        }

        private int count
        {
            get
            {
                if (elements == null)
                    return elementsCount = elementList != null ? elementList.Count : 0;
                smallerArraySize = elements.minArraySize;
                return IsOverMaxMultiEditLimit ? (elementsCount = 0) : (elementsCount = smallerArraySize);
            }
        }


        private float GetListElementHeight()
        {
            float num = 4f + listElementTopPadding;
            if (cacheCount == 0)
                CacheIfNeeded();
            float listElementHeight = count > 0 && !IsOverMaxMultiEditLimit
                ? GetElementYOffset(elementsCount - 1) + GetElementHeight(elementsCount - 1) + num
                : elementHeight * (IsOverMaxMultiEditLimit ? 2f : 1f) + num;
            if ((double)listElementHeight != (double)lastHeight)
            {
                lastHeight = listElementHeight;
                InvalidateCache();
                listElementHeight = GetListElementHeight();
            }

            return listElementHeight;
        }
        
        private Rect GetElementRect(int index, Rect listRect)
        {
            // If the cache is valid and the Rect exists, we return it
            if (!rectCacheInvalidated && rectCache.TryGetValue(index, out var cachedRect))
            {
                return cachedRect;
            }

            // We calculate the new Rect
            float yOffset = GetElementYOffset(index);
            float height = GetElementHeight(index);

            Rect newRect = new Rect(listRect.x, listRect.y + yOffset, listRect.width, height);

            // Add the Rect to the cache
            rectCache[index] = newRect;

            return newRect;
        }
        #endregion


        private bool CheckForChildInvalidation()
        {
            if (outdatedProperties.BinarySearch(propertyPath) < 0)
                return false;
            InvalidateCache();
            outdatedProperties = outdatedProperties.Where<string>((Func<string, bool>)(e => !e.Equals(propertyPath))).ToList<string>();
            return true;
        }

        #region DoList

        public void DoLayoutList()
        {
            // Force SerializedObject to synchronise and update the list
            if (serializedObject != null)
            {
                serializedObject.Update();

                // Update cache, if required
                if (cacheCount == 0)
                    CacheIfNeeded();

                if (!propertyCacheValid)
                {
                    InvalidateCache();
                    CacheIfNeeded();
                }
            }

            GUILayout.BeginVertical();

            // List header
            Rect headerRect = GUILayoutUtility.GetRect(0.0f, HeaderHeight, GUILayout.ExpandWidth(true));
            DoListHeader(headerRect);

            if (listIsExpanded)
            {
                // List elements
                Rect elementsRect = GUILayoutUtility.GetRect(10f, GetListElementHeight(), GUILayout.ExpandWidth(true));
                DoListElements(elementsRect, infinityRect);

                // List footer
                Rect footerRect = GUILayoutUtility.GetRect(4f, footerHeight, GUILayout.ExpandWidth(true));
                DoListFooter(footerRect);
            }

            GUILayout.EndVertical();

            // Application of changes to SerializedObject, if any
            if (serializedObject != null)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }


  
        
        #region Header
        private void DoListHeader(Rect headerRect)
        {
            if (elements != null)
                EditorGUI.BeginProperty(headerRect, GUIContent.none, elements);
            
            recursionCounter = 0;

            DrawHeaderBackground(headerRect);

            headerRect.xMin += 6f;
            headerRect.xMax -= 6f;
            headerRect.height -= 2f;
            ++headerRect.y;

            HandleDragAndDrop(headerRect, serializedProperty);
            DrawHeader(headerRect, serializedObject, elements, elementList);

            if (elements == null)
                return;

            EditorGUI.EndProperty();
        }
        public void DrawHeaderBackground(Rect headerRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if ((double)headerRect.height < 5.0)
                EmptyHeaderBackground.Draw(headerRect, false, false, false, false);
            else
                HeaderBackground.Draw(headerRect, false, false, false, false);
        }
        
        public void DrawHeader(Rect rect, SerializedObject serializedObject, SerializedProperty element, IList elementList)
        {
            if (elementList == null)
            {
                EditorGUI.LabelField(rect, element != null ? "Serialized Property" : "IList");
            }
            else
            {
                rect.x += 12f;
                
                bool newExpandedState = EditorGUI.Foldout(rect, listIsExpanded, $"{element.displayName} ({elementList.Count})", true);
                if (newExpandedState != listIsExpanded)
                {
                    listIsExpanded = newExpandedState;
                    SaveListExpansionState(); // Save the status after each change
                }
            }
        }
        
        private void HandleDragAndDrop(Rect dropArea, SerializedProperty listProperty)
        {
            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            Type elementType = GetElementType();

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        // Support when the list type is MonoBehaviour or Component
                        if (typeof(Component).IsAssignableFrom(elementType) && draggedObject is GameObject go)
                        {
                            Component component = go.GetComponent(elementType);
                            if (component != null)
                            {
                                listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
                                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                                newElement.objectReferenceValue = component;
                            }
                        }
                    }

                    evt.Use();
                    break;
            }
        }
        private Type GetElementType()
        {
            if (serializedProperty != null)
            {
                var fieldInfo = serializedProperty.serializedObject.targetObject.GetType().GetField(
                    serializedProperty.propertyPath,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    var fieldType = fieldInfo.FieldType;
                    if (fieldType.IsArray)
                    {
                        return fieldType.GetElementType();
                    }
                    else if (fieldType.IsGenericType)
                    {
                        return fieldType.GetGenericArguments()[0];
                    }
                }
            }
            else if (elementList != null)
            {
                var listType = elementList.GetType();
                if (listType.IsGenericType)
                {
                    return listType.GetGenericArguments()[0];
                }
                else if (listType.IsArray)
                {
                    return listType.GetElementType();
                }
            }

            return typeof(object);
        }
        #endregion

        #region Element
      private void DoListElements(Rect listRect, Rect visibleRect)
        {
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (Event.current.type == EventType.Repaint)
                BoxBackground.Draw(listRect, false, false, false, false);

            listRect.yMin += listElementTopPadding;
            listRect.yMax -= 4f;
            listRect.xMin++;
            listRect.xMax--;

            Rect elementRect = listRect;
            elementRect.height = elementHeight;

            // Update cache if it needs to be refreshed
            if (rectCacheInvalidated)
            {
                for (int i = 0; i < elementsCount; i++)
                {
                    GetElementRect(i, listRect);
                }

                rectCacheInvalidated = false; // Cache został odświeżony
            }
            
            if (elementsCount > 0 && !IsOverMaxMultiEditLimit)
            {
                EditorGUI.BeginChangeCheck();

                // Separate functionality into logic and visualisation
                if (dragging && Event.current.type == EventType.Repaint)
                {
                    HandleDragging(listRect, elementRect);
                }
                else
                {
                    RenderElements(listRect, visibleRect, elementRect);
                }

                // Support for dragging and selection
                DoDraggingAndSelection(listRect);

                if (EditorGUI.EndChangeCheck())
                    InvalidateCacheRecursive();
            }
            else
            {
                // Drawing if the list is empty
                Rect emptyRect = listRect;
                emptyRect.height = elementHeight;
                DrawElementBackground(elementRect, -1, false, false);
                DrawNoneElement(elementRect);
            }

            EditorGUI.indentLevel = indentLevel;

            if (CheckForChildInvalidation() && recursionCounter < 2)
            {
                recursionCounter++;
                DoListElements(listRect, visibleRect);
            }

            //cacheCount = 0;
        }

        private void RenderElements(Rect listRect, Rect visibleRect, Rect elementRect)
        {
            for (int i = 0; i < elementsCount; i++)
            {
                // Handling culling (if the element is out of view)
                if (useCulling && (visibleRect.y > GetElementYOffset(i) + GetElementHeight(i) ||
                                   visibleRect.y + visibleRect.height < GetElementYOffset(i)))
                    continue;

                bool isSelected = SelectionList.Contains(i);
                bool isFocused = isSelected;

                elementRect.height = GetElementHeight(i);
                elementRect.y = listRect.y + GetElementYOffset(i);

                // Drawing the background, the handle and the element itself
                DrawElementBackground(elementRect, i, isSelected, isFocused);
                DrawElementDraggingHandle(elementRect, i, isSelected, isFocused);

                Rect contentRect = GetContentRect(elementRect);
                if (elements != null)
                    DrawElement(contentRect, elements.GetArrayElementAtIndex(i), null);
                else
                    DrawElement(contentRect, null, elementList[i]);
            }
        }

        private void HandleDragging(Rect listRect, Rect elementRect)
        {
            int rowIndex = CalculateRowIndex(listRect);

            if (nonDragTargetIndices == null)
                nonDragTargetIndices = new List<int>();

            nonDragTargetIndices.Clear();

            for (int i = 0; i < elementsCount; i++)
            {
                if (i != index)
                    nonDragTargetIndices.Add(i);
            }

            nonDragTargetIndices.Insert(rowIndex, -1);

            bool flag = false;
            for (int i = 0; i < nonDragTargetIndices.Count; i++)
            {
                int nonDragTargetIndex = nonDragTargetIndices[i];
                if (nonDragTargetIndex != -1)
                {
                    elementRect.height = GetElementHeight(nonDragTargetIndex);
                    elementRect.y = listRect.y + GetElementYOffset(nonDragTargetIndex, index);

                    if (flag)
                        elementRect.y += GetElementHeight(index);

                    // Użycie SlideGroup do animowanego przesuwania
                    Rect adjustedRect = slideGroup.GetRect(nonDragTargetIndex, elementRect);
                    elementRect.y = adjustedRect.y;

                    DrawElementBackground(elementRect, nonDragTargetIndex, false, false);
                    DrawElementDraggingHandle(elementRect, i, false, false);

                    Rect contentRect = GetContentRect(elementRect);
                    if (elements != null)
                        DrawElement(contentRect, propertyCache[nonDragTargetIndex].property, null);
                    else
                        DrawElement(contentRect, null, elementList[nonDragTargetIndex]);
                }
                else
                {
                    flag = true;
                }
            }

            // Drawing a draggable element
            if (index >= 0)
            {
                draggingRect.x = listRect.x;
                draggingRect.y = GetClampedDragPosition(listRect) - dragOffset + listRect.y;
                draggingRect.width = listRect.width;
                draggingRect.height = GetElementHeight(index);

                // Rysowanie tła i uchwytu dla przeciąganego elementu
                DrawElementBackground(draggingRect, index, true, true);
                DrawElementDraggingHandle(draggingRect, index, true, true);

                // Drawing the content of the draggable element
                Rect contentRect = GetContentRect(draggingRect);
                if (elements != null)
                    DrawElement(contentRect, propertyCache[index].property, null);
                else
                    DrawElement(contentRect, null, elementList[index]);
            }
        }

        
        private void DrawElement(Rect rect, SerializedProperty element, object listItem)
        {
            rect.y += ElementPadding(rect.height) / 2f;
            SerializedProperty property = element ?? listItem as SerializedProperty;

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = FieldLabelSize(rect, property);
            try
            {
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            }
            catch
            {
                // ignored
            }

            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                Event.current.Use();

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void DrawElementBackground(Rect rect, int index, bool selected, bool focused)
        {
            if (!listIsExpanded) return;
            if (Event.current.type != EventType.Repaint)
                return;

            ElementBackground.Draw(rect, false, selected, selected, focused);
        }

        private void DrawElementDraggingHandle(Rect rect, int index, bool selected, bool focused)
        {
            if (!listIsExpanded) return;
            if (Event.current.type != EventType.Repaint)
                return;

            DraggingHandle.Draw(new Rect(rect.x + 5f, rect.y + 8f, 10f, 6f), false, false, false, false);
        }

        private void DrawNoneElement(Rect rect)
        {
            if (listIsExpanded)
            {
                rect.x += 23;
                EditorGUI.LabelField(rect, listIsEmpty);
            }
        }

        private float FieldLabelSize(Rect r, SerializedProperty prop)
        {
            return (float)(r.width * 0.44999998807907104 - 20.0 -
                           (Regex.Matches(prop.propertyPath, ".Array.data").Count * 22)
                           + (prop.depth < 2 ? 7.0 : 0.0));
        }

        #endregion
        #region Drag

        private void DoDraggingAndSelection(Rect listRect)
        {
            Event current = Event.current;
            int index1 = index;
            bool flag = false;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (listRect.Contains(Event.current.mousePosition) && Event.current.button <= 0)
                    {
                        int rowIndex = GetRowIndex(Event.current.mousePosition.y - listRect.y);
                        if (EditorGUI.actionKey)
                        {
                            if (IsSelected(rowIndex))
                                Deselect(rowIndex);
                            else
                                Select(rowIndex, true);
                        }
                        else if (current.shift && selectedIndices.Count > 0)
                            SelectRange(
                                rowIndex < selectedIndices[0]
                                    ? selectedIndices[selectedIndices.Count - 1]
                                    : selectedIndices[0], rowIndex);
                        else
                            Select(rowIndex);

                        if (index >= 0)
                        {
                            float localY = Event.current.mousePosition.y - listRect.y;
                            dragOffset = localY - GetElementYOffset(GetRowIndex(localY));
                            UpdateDraggedY(listRect);
                            GUIUtility.hotControl = id;
                            //m_SlideGroup.Reset();
                            nonDragTargetIndices = new List<int>();
                        }

                        //GrabKeyboardFocus();
                        current.Use();
                        flag = true;
                        break;
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        current.Use();
                        try
                        {
                            int rowIndex = CalculateRowIndex(listRect);
                            if (index != rowIndex && dragging)
                            {
                                if (serializedObject != null && elements != null)
                                {
                                    Undo.RegisterCompleteObjectUndo(serializedObject.targetObjects, undoMove);
                                    elements.MoveArrayElement(index, rowIndex);

                                    // Saving the changed order
                                    serializedObject.ApplyModifiedProperties();
                                }
                                else if (elementList != null)
                                {
                                    // Updating the list of ordinary objects
                                    object element = elementList[index];
                                    elementList.RemoveAt(index);
                                    elementList.Insert(rowIndex, element);
                                }

                                index = rowIndex;
                                UnityEngine.GUI.changed = true;
                            }
                        }
                        finally
                        {
                            GUIUtility.hotControl = 0;
                            nonDragTargetIndices = null;
                            dragging = false;
                        }
                    }

                    break;


                case EventType.MouseDrag:

                    if (GUIUtility.hotControl != id || current.modifiers != 0)
                    {
                        dragging = false;
                        break;
                    }

                    dragging = true;

                    if (SelectionList.Count > 1)
                        index = GetRowIndex(UnityEngine.Event.current.mousePosition.y - listRect.y);

                    UpdateDraggedY(listRect);

                    current.Use();
                    break;
            }

            if (!(index != index1 | flag))
                return;
        }

        private void UpdateDraggedY(Rect listRect)
        {
            draggedY = Event.current.mousePosition.y - listRect.y;
        }

        private float GetClampedDragPosition(Rect listRect)
        {
            return Mathf.Clamp(draggedY, dragOffset,
                listRect.height - GetElementHeight(index) + dragOffset);
        }
        
        private int CalculateRowIndex(Rect listRect)
        {
            int rowIndex = GetRowIndex(GetClampedDragPosition(listRect) - dragOffset, true);
            float num = draggedY - dragOffset;

            while (rowIndex > 0 && GetElementYOffset(rowIndex - 1) + GetElementHeight(rowIndex - 1) / 2f > num)
                rowIndex--;

            return Mathf.Clamp(rowIndex, 0, elementsCount - 1);
        }
        private int GetRowIndex(float localY, bool skipActiveElement = false)
        {
            for (int i = 0; i < elementsCount; ++i)
            {
                float elementYoffset = GetElementYOffset(i);
                if (skipActiveElement)
                {
                    if (i >= this.index)
                        elementYoffset += (float)(-(double)GetElementHeight(i) +
                                                  (double)GetElementHeight(i) / 2.0);
                    else if (i < this.index)
                        elementYoffset -= GetElementHeight(i) / 2f;
                }

                if ((double)elementYoffset > (double)localY)
                {
                    int num;
                    return num = i - 1;
                }
            }

            return elementsCount - 1;
        }

        #endregion

        
        #region Footer

        private void DoListFooter(Rect footerRect)
        {
            DrawFooter(footerRect, this);
        }
        private Rect cachedFooterRect;
        private Rect cachedPlusButtonRect;
        private Rect cachedMinusButtonRect;
        private bool isFooterCacheValid = false;

        private void DrawFooter(Rect rect, GUIListPro listPro)
        {
            // Check that the cache is up to date
            if (!isFooterCacheValid || rect != cachedFooterRect)
            {
                // Update cache
                cachedFooterRect = rect;
                float num = rect.xMax - 10f;
                float x = num - 58f;
                cachedFooterRect = new Rect(x, rect.y, num - x, rect.height);
                cachedPlusButtonRect = new Rect(x + 4f, rect.y, 25f, 16f);
                cachedMinusButtonRect = new Rect(num - 29f, rect.y, 25f, 16f);

                // Set the cache flag as current
                isFooterCacheValid = true;
            }

            // Draw the background
            if (Event.current.type == EventType.Repaint)
                FooterBackground.Draw(cachedFooterRect, false, false, false, false);

            // "Plus" button
            using (new EditorGUI.DisabledScope(listPro.IsOverMaxMultiEditLimit))
            {
                if (UnityEngine.GUI.Button(cachedPlusButtonRect, iconToolbarPlus, PreButton))
                {
                    DoAddButton(listPro);
                    listPro.InvalidateCacheRecursive();
                }
            }

            // "Minus" button
            using (new EditorGUI.DisabledScope(listPro.index < 0 || listPro.index >= listPro.count ||
                                               listPro.IsOverMaxMultiEditLimit))
            {
                if (UnityEngine.GUI.Button(cachedMinusButtonRect, iconToolbarMinus, PreButton) ||
                    UnityEngine.GUI.enabled && listPro.scheduleRemove)
                {
                    DoRemoveButton(listPro);
                    listPro.InvalidateCacheRecursive();
                    UnityEngine.GUI.changed = true;
                }
            }

            listPro.scheduleRemove = false;
        }

        #endregion

        #endregion

        #region Buttons

        public void DoAddButton(GUIListPro listPro, UnityEngine.Object value)
        {
            // if (GUIUtility.keyboardControl != listPro.id)
            //     listPro.GrabKeyboardFocus();

            if (listPro.serializedProperty != null)
            {
                listPro.serializedProperty = listPro.serializedProperty.serializedObject.FindProperty(listPro.propertyPath);
                listPro.serializedProperty.arraySize = listPro.count + 1;
                listPro.index = listPro.serializedProperty.arraySize - 1;
                if (value != (UnityEngine.Object)null) listPro.serializedProperty.GetArrayElementAtIndex(listPro.index).objectReferenceValue = value;
            }
            else
            {
                Type type1 = listPro.list.GetType();
                Type type2 = !type1.IsGenericType
                    ? type1.GetElementType()
                    : type1.GetTypeInfo().GenericTypeArguments[0];
                if (value != (UnityEngine.Object)null)
                    listPro.index = listPro.list.Add((object)value);
                else if (type2 == typeof(string))
                    listPro.index = listPro.list.Add((object)"");
                else if (listPro.list.GetType().GetGenericArguments()[0] != (System.Type)null)
                    listPro.index =
                        listPro.list.Add(Activator.CreateInstance(listPro.list.GetType().GetGenericArguments()[0]));
            }

            Undo.SetCurrentGroupName(undoAdd);
            listPro.InvalidateForGUI();
        }

        public void DoAddButton(GUIListPro listPro) =>
            DoAddButton(listPro, (UnityEngine.Object)null);

        public void DoRemoveButton(GUIListPro listPro)
        {
            // if (GUIUtility.keyboardControl != listPro.id)
            //     listPro.GrabKeyboardFocus();
            int[] numArray1;
            if (listPro.SelectionList.Count <= 0)
                numArray1 = new int[1] { listPro.index };
            else
                numArray1 = listPro.selectedIndices.Reverse<int>().ToArray<int>();
            int[] numArray2 = numArray1;
            int num = -1;
            foreach (int index1 in numArray2)
            {
                if (index1 < listPro.count)
                {
                    if (listPro.serializedProperty != null)
                    {
                        listPro.serializedProperty.DeleteArrayElementAtIndex(index1);
                        if (index1 < listPro.count - 1)
                        {
                            SerializedProperty serializedProperty =
                                listPro.serializedProperty.GetArrayElementAtIndex(index1);
                            for (int index2 = index1 + 1; index2 < listPro.count; ++index2)
                            {
                                SerializedProperty arrayElementAtIndex =
                                    listPro.serializedProperty.GetArrayElementAtIndex(index2);
                                serializedProperty.isExpanded = arrayElementAtIndex.isExpanded;
                                serializedProperty = arrayElementAtIndex;
                            }
                        }
                    }
                    else
                        listPro.list.RemoveAt(listPro.index);

                    num = index1;
                }
            }

            listPro.index = Mathf.Clamp(num - 1, 0, listPro.count - 1);
            Undo.SetCurrentGroupName(undoRemove);
            listPro.InvalidateForGUI();
        }

        #endregion
    }

public class GUIListProSlideGroup
{
    private readonly Dictionary<int, float> elementPositions = new Dictionary<int, float>();
    private readonly float slideSpeed;

    public GUIListProSlideGroup(float slideSpeed = 10f)
    {
        this.slideSpeed = slideSpeed;
    }

    public Rect GetRect(int index, Rect targetRect)
    {
        if (!elementPositions.ContainsKey(index))
            elementPositions[index] = targetRect.y;

        elementPositions[index] = Mathf.Lerp(elementPositions[index], targetRect.y, Time.deltaTime * slideSpeed);
        return new Rect(targetRect.x, elementPositions[index], targetRect.width, targetRect.height);
    }

    public void Reset()
    {
        elementPositions.Clear();
    }
}
}
