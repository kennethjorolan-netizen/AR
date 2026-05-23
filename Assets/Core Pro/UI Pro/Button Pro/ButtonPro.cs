using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

#if DOTWEEN
using DG.Tweening;
#endif

namespace CorePro.ButtonPro
{
public class ButtonPro : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    #region Variables

    [FormerlySerializedAs("_interactable")]
    [SerializeField] private bool interactable = true;

    public bool Interactable
    {
        get => interactable;
        set
        {
            if (interactable == value) return;
            interactable = value;
            SetInteractable(value);
        }
    }

    #region Basics
    public ButtonState currentState;
    public ButtonPressState buttonPressStateAt;
    
    private Vector3 initScale;
    private bool isHighlighted = false;
    #endregion

    #region StateColorImages
    public bool useStateColorsForImage;
    public bool useIndividualImageColors;
    public List<Image> images = new List<Image>();
    public List<ButtonProImageColors> imageColors = new List<ButtonProImageColors>();
    #endregion

    #region StateColorTexts
    public bool useStateColorsForText;
    public bool useIndividualTextColors;
    public List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    public List<ButtonProTextsColors> textColors = new List<ButtonProTextsColors>();
    #endregion

    #region StateSprites
    public bool useStateSprites;
    public bool useIndividualSprites;
    public bool disableObjOnEmptySprite;
    public List<ButtonProStateSprites> stateSprites = new List<ButtonProStateSprites>();
  
    #endregion

    #region StateTexts

    public bool useStateTexts;
    public bool useIndividualTexts;
    public bool disableObjOnEmptyText;
    public List<ButtonProStateTexts> stateTexts = new List<ButtonProStateTexts>();
  
    #endregion

    #region StateObjectGroups
    public bool useStateObjectGroup;
    public ButtonProStateObjects stateObjectsGroups;
    #endregion
    
    #region PressAnim
    public bool usePressAnimation = false;
    public float pressAnimDuration = 0.2f;
    public Vector3 pressAnimScale = new Vector3(0.9f,0.9f,0.9f);
    #endregion

    #region InteractableGroups
    public bool useActivationGroups;
    public List<GameObject> enableWhenInteractable = new List<GameObject>();
    public List<GameObject> enableWhenNotInteractable = new List<GameObject>();
    #endregion

    #region Events
    public UnityEvent onClick;
    public UnityEvent onClickDown;
    public UnityEvent onClickUp;
    public UnityEvent onHighlighted;
    public UnityEvent onInteractable;
    public UnityEvent onNoInteractable;
    [SerializeField] 
    public BoolEvent onInteractableChanged;
    #endregion

    #region Actions
    public Action onClickAction;
    public Action onClickDownAction;
    public Action onClickUpAction;
    public Action onHighlightedAction;
    public Action onInteractableAction;
    public Action onNoInteractableAction;
    public Action<bool> onInteractableChangedAction;
    #endregion

    #region StatesSets
    
    private ButtonProImageColors defaultImageColorsSet;
    private ButtonProImageColors currentImageColors;
    private ButtonProTextsColors defaultTextColors;
    private ButtonProTextsColors currentTextColorsSet;
    private ButtonProStateSprites defaultSprites;
    private ButtonProStateSprites currentSpritesSet;
    private ButtonProStateTexts defaultTexts;
    private ButtonProStateTexts currentTextsSet;

    #endregion

    #region Tooltip
    public bool useTooltip;
    public Transform tooltipObject;    
    public TextMeshProUGUI tooltipText;    
    public ButtonProStateTooltipTexts stateTooltipTexts = new ButtonProStateTooltipTexts();

    #endregion
    #endregion

    private List<Transform> lastUsedObjects;
    private ButtonState prevButtonState;
    
    private void Awake()
    {
        initScale = transform.localScale;
    }

    private void Start()
    {
        ValidateImageColorsList();
        UpdateButtonState();
        UpdateActivation();
    }

    private void OnDestroy()
    {
        onClick?.RemoveAllListeners();
        onClickUp?.RemoveAllListeners();
        onClickDown?.RemoveAllListeners();
        onHighlighted?.RemoveAllListeners();
        onInteractable?.RemoveAllListeners();
        onNoInteractable?.RemoveAllListeners();
        onInteractableChanged?.RemoveAllListeners();

        onClickAction = null;
        onClickUpAction = null;
        onClickDownAction = null;
        onHighlightedAction = null;
        onInteractableAction = null;
        onNoInteractableAction = null;
        onInteractableChangedAction = null;
    }

    
    #region Validates
    public void ValidateImageColorsList()
    {
        if (imageColors == null)
        {
            imageColors = new List<ButtonProImageColors>();
        }
        
        if (imageColors.Count == 0)
        {
            imageColors.Add(new ButtonProImageColors());
        }

        while (imageColors.Count < images.Count)
        {
            imageColors.Add(new ButtonProImageColors());
        }

        while (imageColors.Count > images.Count)
        {
            imageColors.RemoveAt(imageColors.Count - 1);
        }
    }

    public void ValidateTextColorsList()
    {
        if (textColors.Count == 0)
        {
            textColors.Add(new ButtonProTextsColors());
        }

        while (textColors.Count < texts.Count)
        {
            textColors.Add(new ButtonProTextsColors());
        }

        while (textColors.Count > texts.Count)
        {
            textColors.RemoveAt(textColors.Count - 1);
        }
    }

    public void ValidateStateSpriteList()
    {
        if (stateSprites.Count == 0)
        {
            stateSprites.Add(new ButtonProStateSprites());
        }

        while (stateSprites.Count < images.Count)
        {
            stateSprites.Add(new ButtonProStateSprites());
        }

        while (stateSprites.Count > images.Count)
        {
            stateSprites.RemoveAt(stateSprites.Count - 1);
        }
    }

    
    public void ValidateStateTextList()
    {
        if (stateTexts.Count == 0)
        {
            stateTexts.Add(new ButtonProStateTexts());
        }

        while (stateTexts.Count < texts.Count)
        {
            stateTexts.Add(new ButtonProStateTexts());
        }

        while (stateTexts.Count > texts.Count)
        {
            stateTexts.RemoveAt(stateTexts.Count - 1);
        }
    }

    public void ValidateObjectsList()   
    {
        if (stateObjectsGroups == null)
        {
            stateObjectsGroups = new ButtonProStateObjects();
        }
    } 
    
    public void ValidateTooltip()   
    {
        if (stateTooltipTexts == null)
        {
            stateTooltipTexts = new ButtonProStateTooltipTexts();
        }
    } 
    #endregion

    #region IPointers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;

        if (buttonPressStateAt == ButtonPressState.OnButtonDown)
            ExecuteClickActions();

        SetState(ButtonState.Pressed);

        onClickDown?.Invoke();
        onClickDownAction?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Interactable) return;

        if (buttonPressStateAt == ButtonPressState.OnButtonUp)
            ExecuteClickActions();

        SetState(ButtonState.Highlighted);

        onClickUp?.Invoke();
        onClickUpAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (useTooltip && stateTooltipTexts.useInactive && Interactable == false)
        {
            SetState(ButtonState.Inactive);
            UpdateTooltip(true);
        }

        if (!Interactable) 
            return;

        isHighlighted = true;
        SetState(ButtonState.Highlighted);
        UpdateTooltip(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateTooltip(false);
        if (!Interactable) return;

        isHighlighted = false;
        SetState(ButtonState.Normal);
    }

    #endregion

    #region Set

    public void SetState(ButtonState state)
    {
        prevButtonState = currentState;
        currentState = state;

        if (useStateColorsForImage)
            UpdateColorsImage();

        if (useStateColorsForText)
            UpdateColorsText();

        if (useStateSprites)
            UpdateSprites();

        if (useStateTexts)
            UpdateTexts();

        if (useStateObjectGroup)
            UpdateStateObjects();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        UpdateButtonState();
        UpdateActivation();

        onInteractableChanged?.Invoke(interactable);
        onInteractableChangedAction?.Invoke(interactable);

        if (interactable)
        {
            onInteractable?.Invoke();
            onInteractableAction?.Invoke();
        }
        else
        {
            onNoInteractable?.Invoke();
            onNoInteractableAction?.Invoke();
        }
    }
    
    void SetColorToImage(Color color, Image targetImage)
    {
        if (targetImage)
            targetImage.color = color;
    }
    
    void SetColorToText(Color color, TextMeshProUGUI targetText)
    {
        if (targetText)
            targetText.color = color;
    }
    void SetSpriteToImage(Sprite sprite, Image targetImage)
    {
        if (disableObjOnEmptySprite)
            targetImage.gameObject.SetActive(sprite);

        if (targetImage && sprite)
            targetImage.sprite = sprite;
    }
    
    void SetText(string text, TextMeshProUGUI textMeshPro)
    {
        if (textMeshPro == null)
            return;

        if (disableObjOnEmptyText)
            textMeshPro.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));

        if (textMeshPro)
            textMeshPro.text = text;
    }
    
    void SetTooltipText(string text, TextMeshProUGUI textMeshPro)
    {
        if (textMeshPro)
            textMeshPro.text = text;
    }

    #endregion


#if DOTWEEN
    private async Task PressAnim()
    {
        await transform.DOScale(pressAnimScale, pressAnimDuration / 2).SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        await transform.DOScale(initScale, pressAnimDuration / 2).SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
    }
#endif 

    private async void ExecuteClickActions()
    {
        if (usePressAnimation)
        {
#if DOTWEEN
            await PressAnim();
#endif 
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(pressAnimDuration / 2));
        }

        onClick?.Invoke();
        onClickAction?.Invoke();
    }

    private void ResetToHighlightedState()
    {
        if (isHighlighted)
        {
            SetState(ButtonState.Highlighted);
        }
        else
        {
            SetState(ButtonState.Normal);
        }
    }

    #region Update
    public void UpdateButtonState()
    {
        SetState(Interactable ? ButtonState.Normal : ButtonState.Inactive);
    }

    private void UpdateActivation()
    {
        if (useActivationGroups)
        {
            foreach (var obj in enableWhenInteractable)
            {
                if (obj != null)
                {
                    obj.SetActive(Interactable);
                }
            }

            foreach (var obj in enableWhenNotInteractable)
            {
                if (obj != null)
                {
                    obj.SetActive(!Interactable);
                }
            }
        }
    }


    private void UpdateColorsImage()
    {
        // Retrieve the default colours or from a list if they exist
        defaultImageColorsSet = imageColors.Count > 0 ? imageColors[0] : new ButtonProImageColors();

        foreach (var image in images)
        {
            if (image == null)
                continue;

            // We choose the colours: individual or default
            currentImageColors = useIndividualImageColors && imageColors.Count > images.IndexOf(image)
                ? imageColors[images.IndexOf(image)]
                : defaultImageColorsSet;

            // Set the colour based on the state
            switch (currentState)
            {
                case ButtonState.Normal:
                    if (currentImageColors.useNormal)
                        SetColorToImage(currentImageColors.normal, image);
                    break;
                case ButtonState.Highlighted:
                    if (currentImageColors.useHighlighted)
                        SetColorToImage(currentImageColors.highlighted, image);
                    else
                        image.gameObject.SetActive(false);
                    break;
                case ButtonState.Pressed:
                    if (currentImageColors.usePress)
                        SetColorToImage(currentImageColors.press, image);
                    break;
                case ButtonState.Inactive:
                    if (currentImageColors.useInactive)
                        SetColorToImage(currentImageColors.inactive, image);
                    break;
            }
        } 
    }
    


    private void UpdateColorsText()
    {
        // We retrieve the default colours or from a list if they exist
        defaultTextColors = textColors.Count > 0 ? textColors[0] : new ButtonProTextsColors();

        foreach (var text in texts)
        {
            if (text == null)
                continue;

            // We choose the colours: individual or default
             currentTextColorsSet = useIndividualTextColors && textColors.Count > texts.IndexOf(text)
                ? textColors[texts.IndexOf(text)]
                : defaultTextColors;

             // Set the colour based on the state
            switch (currentState)
            {
                case ButtonState.Normal:
                    if (currentTextColorsSet.useNormal)
                        SetColorToText(currentTextColorsSet.normal, text);
                    break;
                case ButtonState.Highlighted:
                    if (currentTextColorsSet.useHighlighted)
                        SetColorToText(currentTextColorsSet.highlighted, text);
                    break;
                case ButtonState.Pressed:
                    if (currentTextColorsSet.usePress)
                        SetColorToText(currentTextColorsSet.press, text);
                    break;
                case ButtonState.Inactive:
                    if (currentTextColorsSet.useInactive)
                        SetColorToText(currentTextColorsSet.inactive, text);
                    break;
            }
        }
        
        
    }
   
    private void UpdateSprites()
    {
        // Download the default sprites or from a list if they exist
        defaultSprites = stateSprites.Count > 0 ? stateSprites[0] : new ButtonProStateSprites();

        foreach (var image in images)
        {
            if (image == null)
                continue;

            // We choose the sprites: individual or default
            currentSpritesSet = useIndividualSprites && stateSprites.Count > images.IndexOf(image)
                ? stateSprites[images.IndexOf(image)]
                : defaultSprites;

            // Set the sprite based on the state
            switch (currentState)
            {
                case ButtonState.Normal:
                    if (currentSpritesSet.useNormal)
                        SetSpriteToImage(currentSpritesSet.normal, image);
                    break;
                case ButtonState.Highlighted:
                    if (currentSpritesSet.useHighlighted)
                        SetSpriteToImage(currentSpritesSet.highlighted, image);
                    break;
                case ButtonState.Pressed:
                    if (currentSpritesSet.usePress)
                        SetSpriteToImage(currentSpritesSet.press, image);
                    break;
                case ButtonState.Inactive:
                    if (currentSpritesSet.useInactive)
                        SetSpriteToImage(currentSpritesSet.inactive, image);
                    break;
            }
        }
    }


    private void UpdateTexts()
    {
        defaultTexts = stateTexts.Count > 0 ? stateTexts[0] : new ButtonProStateTexts();

        foreach (var text in texts)
        {
            if (text == null)
                continue;

            currentTextsSet = useIndividualTexts && stateTexts.Count > texts.IndexOf(text)
                ? stateTexts[texts.IndexOf(text)]
                : defaultTexts;

            switch (currentState)
            {
                case ButtonState.Normal:
                    if (currentTextsSet.useNormal)
                        SetText(currentTextsSet.normal, text);
                    break;
                case ButtonState.Highlighted:
                    if (currentTextsSet.useHighlighted)
                        SetText(currentTextsSet.highlighted, text);
                    break;
                case ButtonState.Pressed:
                    if (currentTextsSet.usePress)
                        SetText(currentTextsSet.press, text);
                    break;
                case ButtonState.Inactive:
                    if (currentTextsSet.useInactive)
                        SetText(currentTextsSet.inactive, text);
                    break;
            }
        }
    }

    private void UpdateStateObjects()
    {
        if (!useStateObjectGroup) return;
        
        DisableGroup(prevButtonState);
            
        switch (currentState)
        {
            case ButtonState.Normal:
                if (stateObjectsGroups.useNormal)
                    SetObjects(stateObjectsGroups.normal, true);
                break;
            case ButtonState.Highlighted:
                if (stateObjectsGroups.useHighlighted)
                    SetObjects(stateObjectsGroups.highlighted, true);
                break;
            case ButtonState.Pressed:
                if (stateObjectsGroups.usePress)
                    SetObjects(stateObjectsGroups.press, true);
                break;
            case ButtonState.Inactive:
                if (stateObjectsGroups.useInactive)
                    SetObjects(stateObjectsGroups.inactive, true);
                break;
        }
    }

    private void DisableGroup(ButtonState state)   
    {   
        switch (state)
        {
            case ButtonState.Normal:
                SetObjects(stateObjectsGroups.normal, false);
                break;
            case ButtonState.Highlighted:
                SetObjects(stateObjectsGroups.highlighted, false);
                break;
            case ButtonState.Pressed:
                SetObjects(stateObjectsGroups.press, false);
                break;
            case ButtonState.Inactive:
                SetObjects(stateObjectsGroups.inactive, false);
                break;
        }
    } 

    private void SetObjects(List<Transform> list, bool newState)   
    {   
        foreach (var obj in list)
        {
            if (obj != null)
                obj.gameObject. SetActive(newState);
        }
    } 
    
    #endregion

    
    private void UpdateTooltip(bool shouldDisplayTooltip) 
    {
        if (useTooltip == false || tooltipObject == null)
            return;

        if (shouldDisplayTooltip == false)
        {
            tooltipObject.gameObject.SetActive(false);
            return;
        }

        if (currentState ==  ButtonState.Inactive && stateTooltipTexts.useInactive == false)
        {
            tooltipObject.gameObject.SetActive(false);
            return;
        }
        
        switch (currentState)
        {
            case ButtonState.Highlighted:
                if (stateTooltipTexts.useHighlighted)
                    SetTooltipText(stateTooltipTexts.highlighted, tooltipText);
                break;
          
            case ButtonState.Inactive:
                if (stateTooltipTexts.useInactive)
                    SetTooltipText(stateTooltipTexts.inactive, tooltipText);
                break;
        }
        
        tooltipObject.gameObject.SetActive(true);
    } 
    
    public enum ButtonState
    {
        Normal,
        Highlighted,
        Pressed,
        Inactive,
    }

    public enum ButtonPressState
    {
        OnButtonUp,
        OnButtonDown,
    }
}

[Serializable]
public class ButtonProImageColors
{
    public Color current = Color.white;

    public bool useNormal = true;
    public Color normal = Color.white;

    public bool useHighlighted = false;
    public Color highlighted = Color.white;

    public bool usePress = false;
    public Color press = Color.white;

    public bool useInactive = false;
    public Color inactive = Color.gray;

    public bool useFocusColor = false;
    public Color focusColor = Color.white;

    public ButtonProImageColors()
    {
        useNormal = true;
        normal = new Color(1f, 1f, 1f, 1f);
        highlighted = new Color(0.9f, 0.9f, 0.9f, 1f);
        press = new Color(0.7f, 0.7f, 0.7f, 1f);
        inactive = new Color(0.2f, 0.2f, 0.2f, 1f);
        focusColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    }
}

[Serializable]
public class ButtonProTextsColors
{
    public bool useNormal = true;
    public Color normal = Color.white;

    public bool useHighlighted = false;
    public Color highlighted = Color.white;

    public bool usePress = false;
    public Color press = Color.white;

    public bool useInactive = false;
    public Color inactive = Color.gray;

    public bool useFocus = false;
    public Color focus = Color.white;

    // Constructor to set default colour values
    public ButtonProTextsColors()
    {
        useNormal = true;
        normal = new Color(1f, 1f, 1f, 1f);
        highlighted = new Color(1f, 1f, 1f, 1f);
        press = new Color(1f, 1f, 1f, 1f);
        inactive = new Color(0.5f, 0.5f, 0.5f, 1f);
        focus = new Color(0.5f, 0.5f, 0.5f, 1f);
    }
}

[Serializable]
public class ButtonProStateSprites
{
    public bool useNormal = true;
    public Sprite normal;

    public bool useHighlighted = false;
    public Sprite highlighted;

    public bool usePress = false;
    public Sprite press;

    public bool useInactive = false;
    public Sprite inactive;

    public bool useFocus = false;
    public Sprite focus;

    public ButtonProStateSprites()
    {
        useNormal = true;
    }
}

[Serializable]
public class ButtonProStateTooltipTexts
{
    public bool useHighlighted = false;
    public string highlighted;

    public bool useInactive = false;
    public string inactive;

    public bool useFocus = false;
    public string focus;

    public ButtonProStateTooltipTexts()
    {
        useHighlighted = true;
    }
}

[Serializable]
public class ButtonProStateTexts
{
    public bool useNormal = true;
    public string normal;

    public bool useHighlighted = false;
    public string highlighted;

    public bool usePress = false;
    public string press;

    public bool useInactive = false;
    public string inactive;

    public bool useFocus = false;
    public string focus;

    public ButtonProStateTexts()
    {
        useNormal = true;
    }
}

[Serializable]
public class ButtonProStateObjects
{
    public bool useNormal = true;
    public List<Transform> normal = new List<Transform>();

    public bool useHighlighted = false;
    public List<Transform> highlighted = new List<Transform>();

    public bool usePress = false;
    public List<Transform> press = new List<Transform>();

    public bool useInactive = false;
    public List<Transform> inactive = new List<Transform>();

    public bool useFocus = false;
    public List<Transform> focus = new List<Transform>();

    public ButtonProStateObjects()
    {
        useNormal = true;
    }
}


[System.Serializable]
public class BoolEvent : UnityEvent<bool> { }
}
