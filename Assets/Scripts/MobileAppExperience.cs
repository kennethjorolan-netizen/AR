using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MobileAppExperience : MonoBehaviour
{
    const string LandingScene = "LandingPage";
    const string HomeScene = "Home";
    const string ScannerScene = "Scanner";

    static MobileAppExperience instance;

    Canvas overlayCanvas;
    GameObject loadingOverlay;
    GameObject permissionOverlay;
    Text loadingText;
    Text permissionTitle;
    Text permissionBody;
    Coroutine activeLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    static MobileAppExperience EnsureInstance()
    {
        if (instance != null)
            return instance;

        var existing = FindFirstObjectByType<MobileAppExperience>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        var go = new GameObject("Mobile App Experience");
        instance = go.AddComponent<MobileAppExperience>();
        return instance;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var app = EnsureInstance();
        app.LockPortrait();
        app.StartCoroutine(app.ApplyMobilePolishNextFrame(scene));
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LockPortrait();
    }

    void Update()
    {
        LockPortrait();

        if (WasBackPressedThisFrame())
            HandleBackButton();
    }

    static bool WasBackPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            return true;
#endif

        return false;
    }

    public static void LoadSceneWithMobileFlow(string sceneName)
    {
        EnsureInstance().BeginLoadScene(sceneName, "Loading...");
    }

    public static void LoadScannerWithCameraFlow()
    {
        EnsureInstance().BeginScannerFlow();
    }

    void BeginScannerFlow()
    {
        if (!SceneExists(ScannerScene))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            RequestCameraPermission();
            return;
        }
#endif

        BeginLoadScene(ScannerScene, "Preparing camera...");
    }

    void BeginLoadScene(string sceneName, string message)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !SceneExists(sceneName))
            return;

        if (string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        if (activeLoad != null)
            StopCoroutine(activeLoad);

        activeLoad = StartCoroutine(LoadSceneRoutine(sceneName, message));
    }

    IEnumerator LoadSceneRoutine(string sceneName, string message)
    {
        HidePermissionOverlay();
        ShowLoadingOverlay(message);

        var minEndTime = Time.realtimeSinceStartup + 0.65f;
        var operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            HideLoadingOverlay();
            activeLoad = null;
            yield break;
        }

        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f || Time.realtimeSinceStartup < minEndTime)
            yield return null;

        operation.allowSceneActivation = true;
        while (!operation.isDone)
            yield return null;

        yield return null;
        HideLoadingOverlay();
        activeLoad = null;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void RequestCameraPermission()
    {
        ShowPermissionOverlay(
            "Camera Permission",
            "Allow camera access so the app can scan plant cards.");

        var callbacks = new UnityEngine.Android.PermissionCallbacks();
        callbacks.PermissionGranted += permissionName =>
        {
            HidePermissionOverlay();
            BeginLoadScene(ScannerScene, "Preparing camera...");
        };
        callbacks.PermissionDenied += permissionName =>
        {
            ShowPermissionOverlay(
                "Camera Needed",
                "Camera access is required for scanning. Tap Scan again and allow the permission.");
        };
        callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
        {
            ShowPermissionOverlay(
                "Camera Blocked",
                "Camera access is blocked. Enable it in Android App Settings, then open Scan again.");
        };

        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);
    }
#endif

    void HandleBackButton()
    {
        if (activeLoad != null)
            return;

        if (permissionOverlay != null && permissionOverlay.activeSelf)
        {
            HidePermissionOverlay();
            return;
        }

        if (PlantDetailsOverlay.DismissIfVisible())
            return;

        var scene = SceneManager.GetActiveScene().name;
        if (string.Equals(scene, ScannerScene, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene, "Plants", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene, "About", StringComparison.OrdinalIgnoreCase))
        {
            BeginLoadScene(HomeScene, "Loading...");
            return;
        }

        if (string.Equals(scene, HomeScene, StringComparison.OrdinalIgnoreCase))
        {
            BeginLoadScene(LandingScene, "Loading...");
            return;
        }

        Application.Quit();
    }

    void LockPortrait()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    IEnumerator ApplyMobilePolishNextFrame(Scene scene)
    {
        yield return null;
        ApplyMobilePolish(scene);
        yield return null;
        ApplyMobilePolish(scene);
    }

    void ApplyMobilePolish(Scene scene)
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null || button.gameObject.scene != scene)
                continue;

            PolishButton(button);
        }
    }

    static void PolishButton(Button button)
    {
        var rect = button.GetComponent<RectTransform>();
        if (rect == null)
            return;

        EnsureInvisibleTouchTarget(button, rect);
    }

    static void EnsureInvisibleTouchTarget(Button button, RectTransform buttonRect)
    {
        const string TouchTargetName = "Mobile Touch Target";
        const float MinimumTouchSize = 96f;

        var target = button.transform.Find(TouchTargetName) as RectTransform;
        if (target == null)
        {
            var targetObject = new GameObject(TouchTargetName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            targetObject.transform.SetParent(button.transform, false);
            target = targetObject.GetComponent<RectTransform>();

            var image = targetObject.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        var scaleX = Mathf.Max(Mathf.Abs(buttonRect.localScale.x), 0.01f);
        var scaleY = Mathf.Max(Mathf.Abs(buttonRect.localScale.y), 0.01f);
        var currentWidth = Mathf.Abs(buttonRect.rect.width * scaleX);
        var currentHeight = Mathf.Abs(buttonRect.rect.height * scaleY);

        target.sizeDelta = new Vector2(
            currentWidth < MinimumTouchSize ? MinimumTouchSize / scaleX : Mathf.Abs(buttonRect.rect.width),
            currentHeight < MinimumTouchSize ? MinimumTouchSize / scaleY : Mathf.Abs(buttonRect.rect.height));
        target.SetAsFirstSibling();
    }

    void EnsureOverlayCanvas()
    {
        if (overlayCanvas != null)
            return;

        var canvasObject = new GameObject("Mobile Overlay Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void ShowLoadingOverlay(string message)
    {
        EnsureOverlayCanvas();

        if (loadingOverlay == null)
            loadingOverlay = CreateLoadingOverlay();

        loadingText.text = message;
        loadingOverlay.SetActive(true);
        loadingOverlay.transform.SetAsLastSibling();
    }

    GameObject CreateLoadingOverlay()
    {
        var overlay = CreateImageObject("Loading Overlay", overlayCanvas.transform, new Color(0.01f, 0.06f, 0.02f, 0.94f));
        Stretch(overlay.GetComponent<RectTransform>());

        var title = CreateText("Loading Title", overlay.transform, "PLANT Anatomy AR", 58, Color.white, TextAnchor.MiddleCenter);
        SetAnchors(title.GetComponent<RectTransform>(), 0.08f, 0.56f, 0.92f, 0.64f);

        loadingText = CreateText("Loading Text", overlay.transform, "Loading...", 34, new Color(0.78f, 1f, 0.78f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(loadingText.GetComponent<RectTransform>(), 0.08f, 0.47f, 0.92f, 0.53f);

        var hint = CreateText("Loading Hint", overlay.transform, "Keep your phone steady", 28, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
        SetAnchors(hint.GetComponent<RectTransform>(), 0.08f, 0.39f, 0.92f, 0.45f);

        overlay.SetActive(false);
        return overlay;
    }

    void HideLoadingOverlay()
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);
    }

    void ShowPermissionOverlay(string title, string body)
    {
        EnsureOverlayCanvas();

        if (permissionOverlay == null)
            permissionOverlay = CreatePermissionOverlay();

        permissionTitle.text = title;
        permissionBody.text = body;
        permissionOverlay.SetActive(true);
        permissionOverlay.transform.SetAsLastSibling();
    }

    GameObject CreatePermissionOverlay()
    {
        var overlay = CreateImageObject("Camera Permission Overlay", overlayCanvas.transform, new Color(0f, 0f, 0f, 0.66f));
        Stretch(overlay.GetComponent<RectTransform>());

        var panel = CreateImageObject("Panel", overlay.transform, new Color(0.94f, 0.98f, 0.92f, 1f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.34f);
        panelRect.anchorMax = new Vector2(0.92f, 0.66f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        permissionTitle = CreateText("Title", panel.transform, "Camera Permission", 42, new Color(0.02f, 0.24f, 0.07f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(permissionTitle.GetComponent<RectTransform>(), 0.08f, 0.68f, 0.92f, 0.88f);

        permissionBody = CreateText("Body", panel.transform, string.Empty, 30, new Color(0f, 0.1f, 0.03f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(permissionBody.GetComponent<RectTransform>(), 0.08f, 0.34f, 0.92f, 0.64f);

        var okButton = CreateButton("OK Button", panel.transform, "OK");
        var okRect = okButton.GetComponent<RectTransform>();
        okRect.anchorMin = new Vector2(0.28f, 0.1f);
        okRect.anchorMax = new Vector2(0.72f, 0.26f);
        okRect.offsetMin = Vector2.zero;
        okRect.offsetMax = Vector2.zero;
        okButton.onClick.AddListener(HidePermissionOverlay);

        overlay.SetActive(false);
        return overlay;
    }

    void HidePermissionOverlay()
    {
        if (permissionOverlay != null)
            permissionOverlay.SetActive(false);
    }

    static GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.Image>().color = color;
        return go;
    }

    static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 22;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(string name, Transform parent, string label)
    {
        var go = CreateImageObject(name, parent, new Color(0.13f, 0.45f, 0.18f, 1f));
        var button = go.AddComponent<Button>();
        var labelText = CreateText("Label", go.transform, label, 34, Color.white, TextAnchor.MiddleCenter);
        Stretch(labelText.GetComponent<RectTransform>());
        return button;
    }

    static bool SceneExists(string sceneName)
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings.");
        return false;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
