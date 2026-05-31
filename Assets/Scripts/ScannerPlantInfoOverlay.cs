using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public sealed class ScannerPlantInfoOverlay : MonoBehaviour
{
    const float ModelRotationDegreesPerSecond = 18f;

    sealed class PlantInfo
    {
        public readonly string Name;
        public readonly string Summary;
        public readonly string Anatomy;
        public readonly string Uses;

        public PlantInfo(string name, string summary, string anatomy, string uses)
        {
            Name = name;
            Summary = summary;
            Anatomy = anatomy;
            Uses = uses;
        }
    }

    readonly struct AnatomyLabelSpec
    {
        public readonly string Text;
        public readonly Vector3 PointPosition;
        public readonly Vector3 LabelPosition;

        public AnatomyLabelSpec(string text, Vector3 pointPosition, Vector3 labelPosition)
        {
            Text = text;
            PointPosition = pointPosition;
            LabelPosition = labelPosition;
        }
    }

    static readonly Dictionary<string, PlantInfo> PlantInfos = new Dictionary<string, PlantInfo>
    {
        { "mango", new PlantInfo("Mango", "Tropical fruit tree with broad leaves and sweet fruits.", "Roots absorb water; leaves make food; flowers become fruits.", "Food, juice, dried mango, desserts, and preserves.") },
        { "cactus", new PlantInfo("Cactus", "Desert plant with thick water-storing stems and spines.", "Stem stores water; spines protect and reduce water loss.", "Ornamental plant and example of dry-climate adaptation.") },
        { "tomato", new PlantInfo("Tomato", "Flowering plant grown for juicy red fruits.", "Roots absorb water; yellow flowers develop into tomatoes.", "Sauces, salads, soups, ketchup, and cooked dishes.") },
        { "aloevera", new PlantInfo("Aloe Vera", "Succulent plant with fleshy leaves that hold clear gel.", "Leaves store water and gel; roots prefer dry, drained soil.", "Skin care and cooling minor skin irritation.") },
        { "neem", new PlantInfo("Neem Tree", "Hardy tree with bitter compound leaves and small flowers.", "Roots anchor the tree; flowers produce small seed fruits.", "Traditional remedies, soaps, and natural pest control.") },
        { "riceplant", new PlantInfo("Rice Plant", "Cereal grass that produces rice grains.", "Fibrous roots hold muddy soil; panicles carry the grains.", "Cooked rice, flour, noodles, snacks, and rice products.") },
        { "sunflower", new PlantInfo("Sunflower", "Tall plant with broad leaves and a large yellow flower head.", "Roots support the stem; flower head produces seeds.", "Seeds, oil, garden decoration, and pollinator support.") },
        { "rose", new PlantInfo("Rose", "Flowering shrub with woody stems, thorns, and fragrant flowers.", "Roots absorb water; flowers attract pollinators.", "Gardens, bouquets, perfumes, and decorations.") },
        { "wheat", new PlantInfo("Wheat", "Cereal grass grown for edible golden grain heads.", "Roots absorb nutrients; grain heads hold kernels.", "Flour for bread, pasta, noodles, biscuits, and foods.") },
        { "bananaplant", new PlantInfo("Banana Plant", "Large herbaceous plant with huge leaves and fruit clusters.", "Corm and roots support growth; flower stalk produces bananas.", "Fresh fruit, cooked food, chips, desserts, drinks, and leaf wrappers.") },
    };

    readonly Dictionary<ObserverBehaviour, bool> trackedObservers = new Dictionary<ObserverBehaviour, bool>();
    readonly Dictionary<ObserverBehaviour, GameObject> labelGroups = new Dictionary<ObserverBehaviour, GameObject>();
    readonly Dictionary<ObserverBehaviour, Transform> rotatingModels = new Dictionary<ObserverBehaviour, Transform>();
    readonly List<Transform> labelBillboards = new List<Transform>();
    GameObject panel;
    GameObject statusPanel;
    UnityEngine.UI.Image statusBackground;
    Text statusText;
    Text titleText;
    Text summaryText;
    Text anatomyText;
    Text usesText;
    ObserverBehaviour currentObserver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        InstallForScene(SceneManager.GetActiveScene());
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    static void InstallForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != "Scanner")
            return;

        if (FindFirstObjectByType<ScannerPlantInfoOverlay>() != null)
            return;

        new GameObject("Scanner Plant Info Overlay").AddComponent<ScannerPlantInfoOverlay>();
    }

    void Start()
    {
        BuildUi();
        RegisterObservers();
        if (currentObserver == null)
            HideInfo();
    }

    void OnDestroy()
    {
        foreach (var observer in trackedObservers.Keys)
        {
            if (observer != null)
                observer.OnTargetStatusChanged -= HandleTargetStatusChanged;
        }

        foreach (var group in labelGroups.Values)
        {
            if (group != null)
                Destroy(group);
        }

        labelBillboards.Clear();
        rotatingModels.Clear();
    }

    void LateUpdate()
    {
        RotateDetectedModels();
        UpdateLabelBillboards();
    }

    void RotateDetectedModels()
    {
        foreach (var pair in trackedObservers)
        {
            if (!pair.Value || pair.Key == null)
                continue;

            var model = GetRotatingModel(pair.Key);
            if (model == null)
                continue;

            model.Rotate(pair.Key.transform.up, ModelRotationDegreesPerSecond * Time.deltaTime, Space.World);
        }
    }

    void UpdateLabelBillboards()
    {
        if (labelBillboards.Count == 0)
            return;

        var camera = FindSceneCamera();
        if (camera == null)
            return;

        var cameraRotation = camera.transform.rotation;
        foreach (var label in labelBillboards)
        {
            if (label != null && label.gameObject.activeInHierarchy)
                label.rotation = cameraRotation;
        }
    }

    Transform GetRotatingModel(ObserverBehaviour observer)
    {
        if (rotatingModels.TryGetValue(observer, out var model) && model != null)
            return model;

        foreach (Transform child in observer.transform)
        {
            if (child == null || string.Equals(child.name, "AR Anatomy Labels", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!HasRenderableModel(child))
                continue;

            rotatingModels[observer] = child;
            return child;
        }

        rotatingModels[observer] = null;
        return null;
    }

    static bool HasRenderableModel(Transform root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && renderer.GetComponent<LineRenderer>() == null)
                return true;
        }

        return false;
    }

    void RegisterObservers()
    {
        var scene = SceneManager.GetActiveScene();
        var observers = FindObjectsByType<ObserverBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var observer in observers)
        {
            if (observer == null || observer.gameObject.scene != scene)
                continue;

            observer.OnTargetStatusChanged += HandleTargetStatusChanged;
            trackedObservers[observer] = IsDetected(observer.TargetStatus.Status);

            if (trackedObservers[observer])
                ShowInfo(observer);
        }
    }

    void HandleTargetStatusChanged(ObserverBehaviour observer, TargetStatus status)
    {
        var detected = IsDetected(status.Status);
        trackedObservers[observer] = detected;

        if (detected)
        {
            ShowInfo(observer);
            return;
        }

        if (observer == currentObserver)
            ShowAnyStillTrackedOrHide();
    }

    static bool IsDetected(Status status)
    {
        return status == Status.TRACKED || status == Status.EXTENDED_TRACKED;
    }

    void ShowAnyStillTrackedOrHide()
    {
        foreach (var pair in trackedObservers)
        {
            if (pair.Value && pair.Key != null)
            {
                ShowInfo(pair.Key);
                return;
            }
        }

        HideInfo();
    }

    void ShowInfo(ObserverBehaviour observer)
    {
        var key = NormalizeTargetName(observer.TargetName);
        if (!TryGetPlantInfo(key, out var info))
        {
            HideInfo();
            return;
        }

        currentObserver = observer;
        ShowDetectedFeedback(info.Name);
        titleText.text = info.Name;
        summaryText.text = info.Summary;
        anatomyText.text = "Anatomy: " + info.Anatomy;
        usesText.text = "Uses: " + info.Uses;
        panel.SetActive(true);
        ShowAnatomyLabels(observer, key);
    }

    void HideInfo()
    {
        currentObserver = null;
        if (panel != null)
            panel.SetActive(false);

        HideAnatomyLabels();
        ShowScanningFeedback();
    }

    void ShowScanningFeedback()
    {
        if (statusPanel == null)
            return;

        statusPanel.SetActive(true);
        statusBackground.color = new Color(0f, 0.28f, 0.06f, 0.78f);
        statusText.text = "Scanning... Point camera at a plant card";
    }

    void ShowDetectedFeedback(string plantName)
    {
        if (statusPanel == null)
            return;

        statusPanel.SetActive(true);
        statusBackground.color = new Color(0f, 0.42f, 0.08f, 0.92f);
        statusText.text = plantName + " detected";
    }

    void ShowAnatomyLabels(ObserverBehaviour observer, string key)
    {
        HideAnatomyLabels();

        if (!labelGroups.TryGetValue(observer, out var group) || group == null)
        {
            group = CreateAnatomyLabelGroup(observer, key);
            labelGroups[observer] = group;
        }

        if (group != null)
            group.SetActive(true);
    }

    void HideAnatomyLabels()
    {
        foreach (var group in labelGroups.Values)
        {
            if (group != null)
                group.SetActive(false);
        }
    }

    static bool TryGetPlantInfo(string key, out PlantInfo info)
    {
        return PlantInfos.TryGetValue(key, out info);
    }

    static string NormalizeTargetName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
    }

    void BuildUi()
    {
        var canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        statusPanel = CreateImageObject("Scanner Feedback Status", canvas.transform, new Color(0f, 0.28f, 0.06f, 0.78f));
        statusBackground = statusPanel.GetComponent<UnityEngine.UI.Image>();
        var statusRect = statusPanel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.13f, 0.84f);
        statusRect.anchorMax = new Vector2(0.87f, 0.91f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        statusText = CreateText("Scanner Feedback Text", statusPanel.transform, string.Empty, 32, Color.white, TextAnchor.MiddleCenter, false);
        statusText.fontStyle = FontStyle.Bold;
        SetAnchors(statusText.GetComponent<RectTransform>(), 0.04f, 0.06f, 0.96f, 0.94f);

        panel = CreateImageObject("Detected Plant Info Panel", canvas.transform, new Color(0.94f, 0.98f, 0.92f, 0.96f));
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.04f);
        rect.anchorMax = new Vector2(0.95f, 0.31f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        titleText = CreateText("Detected Plant Title", panel.transform, "Plant", 46, new Color(0.02f, 0.24f, 0.07f, 1f), TextAnchor.MiddleLeft, false);
        titleText.fontStyle = FontStyle.Bold;
        SetAnchors(titleText.GetComponent<RectTransform>(), 0.06f, 0.76f, 0.94f, 0.95f);

        summaryText = CreateText("Detected Plant Summary", panel.transform, string.Empty, 30, new Color(0f, 0.1f, 0.03f, 1f), TextAnchor.UpperLeft, false);
        SetAnchors(summaryText.GetComponent<RectTransform>(), 0.06f, 0.52f, 0.94f, 0.72f);

        anatomyText = CreateText("Detected Plant Anatomy", panel.transform, string.Empty, 28, new Color(0f, 0.1f, 0.03f, 1f), TextAnchor.UpperLeft, false);
        SetAnchors(anatomyText.GetComponent<RectTransform>(), 0.06f, 0.30f, 0.94f, 0.48f);

        usesText = CreateText("Detected Plant Uses", panel.transform, string.Empty, 28, new Color(0f, 0.1f, 0.03f, 1f), TextAnchor.UpperLeft, false);
        SetAnchors(usesText.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.94f, 0.26f);

        panel.transform.SetAsLastSibling();
        statusPanel.transform.SetAsLastSibling();
    }

    static Canvas FindSceneCanvas()
    {
        var scene = SceneManager.GetActiveScene();
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.scene == scene)
                return canvas;
        }

        return null;
    }

    static Camera FindSceneCamera()
    {
        var scene = SceneManager.GetActiveScene();
        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            if (camera != null && camera.gameObject.scene == scene && camera.enabled)
                return camera;
        }

        return Camera.main;
    }

    GameObject CreateAnatomyLabelGroup(ObserverBehaviour observer, string key)
    {
        var group = new GameObject("AR Anatomy Labels");
        group.transform.SetParent(observer.transform, false);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;

        var labelMaterial = CreateSolidMaterial(new Color(0.02f, 0.42f, 0.08f, 0.95f));
        var lineMaterial = CreateSolidMaterial(new Color(1f, 1f, 1f, 0.92f));
        var markerMaterial = CreateSolidMaterial(new Color(1f, 0.9f, 0.18f, 1f));

        foreach (var spec in GetLabelSpecs(key, GetObserverSize(observer)))
        {
            CreateCalloutLine(spec, group.transform, lineMaterial);
            CreatePartMarker(spec.PointPosition, group.transform, markerMaterial);
            CreateWorldLabel(spec, group.transform, labelMaterial);
        }

        group.SetActive(false);
        return group;
    }

    static AnatomyLabelSpec[] GetLabelSpecs(string key, Vector2 size)
    {
        var labels = GetLabelNames(key);
        var halfWidth = Mathf.Max(size.x, 10f) * 0.5f;
        var halfHeight = Mathf.Max(size.y, 6f) * 0.5f;

        return new[]
        {
            new AnatomyLabelSpec(labels[0], Point(0f, 0.58f), Label(-0.48f, 0.80f)),
            new AnatomyLabelSpec(labels[1], Point(0.28f, 0.28f), Label(0.70f, 0.52f)),
            new AnatomyLabelSpec(labels[2], Point(-0.22f, -0.02f), Label(-0.72f, 0.12f)),
            new AnatomyLabelSpec(labels[3], Point(0f, -0.36f), Label(0.58f, -0.34f)),
            new AnatomyLabelSpec(labels[4], Point(0f, -0.78f), Label(-0.55f, -0.78f)),
        };

        Vector3 Point(float x, float z)
        {
            return new Vector3(x * halfWidth, 0.08f, z * halfHeight);
        }

        Vector3 Label(float x, float z)
        {
            return new Vector3(x * halfWidth, 0.65f, z * halfHeight);
        }
    }

    static string[] GetLabelNames(string key)
    {
        switch (key)
        {
            case "sunflower":
                return new[] { "Flower", "Seeds", "Leaf", "Stem", "Root" };
            case "wheat":
                return new[] { "Grain", "Seed Head", "Leaf", "Stem", "Root" };
            case "riceplant":
                return new[] { "Panicle", "Grain", "Leaf", "Stem", "Root" };
            case "cactus":
                return new[] { "Flower", "Fruit", "Spines", "Stem", "Root" };
            case "aloevera":
                return new[] { "Leaf", "Gel", "Outer Leaf", "Stem", "Root" };
            case "banana":
            case "bananaplant":
                return new[] { "Flower", "Fruit", "Leaf", "Pseudostem", "Root" };
            case "neem":
                return new[] { "Flower", "Fruit", "Leaf", "Trunk", "Root" };
            default:
                return new[] { "Flower", "Fruit", "Leaf", "Stem", "Root" };
        }
    }

    static Vector2 GetObserverSize(ObserverBehaviour observer)
    {
        var method = observer.GetType().GetMethod("GetSize", Type.EmptyTypes);
        if (method != null && method.Invoke(observer, null) is Vector2 size && size.x > 0f && size.y > 0f)
            return size;

        return new Vector2(15f, 8.37f);
    }

    void CreateWorldLabel(AnatomyLabelSpec spec, Transform parent, Material backgroundMaterial)
    {
        var label = new GameObject(spec.Text + " Label", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        label.transform.SetParent(parent, false);
        label.transform.localPosition = spec.LabelPosition;
        label.transform.localScale = Vector3.one * 0.011f;
        labelBillboards.Add(label.transform);

        var rect = label.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250f, 74f);

        var canvas = label.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = FindSceneCamera();
        canvas.sortingOrder = 80;

        var background = CreateImageObject("Background", label.transform, Color.white);
        background.GetComponent<UnityEngine.UI.Image>().material = backgroundMaterial;
        SetAnchors(background.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        var text = CreateText("Text", label.transform, spec.Text, 34, Color.white, TextAnchor.MiddleCenter, false);
        text.fontStyle = FontStyle.Bold;
        SetAnchors(text.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.94f, 0.92f);
    }

    static void CreateCalloutLine(AnatomyLabelSpec spec, Transform parent, Material material)
    {
        var lineObject = new GameObject(spec.Text + " Pointer", typeof(LineRenderer));
        lineObject.transform.SetParent(parent, false);
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localRotation = Quaternion.identity;
        lineObject.transform.localScale = Vector3.one;

        var line = lineObject.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, spec.PointPosition);
        line.SetPosition(1, spec.LabelPosition);
        line.startWidth = 0.035f;
        line.endWidth = 0.035f;
        line.numCapVertices = 4;
        line.material = material;
    }

    static void CreatePartMarker(Vector3 position, Transform parent, Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Part Marker";
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = position;
        marker.transform.localScale = Vector3.one * 0.16f;
        marker.GetComponent<Renderer>().sharedMaterial = material;

        var collider = marker.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    static Material CreateSolidMaterial(Color color)
    {
        var shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        material.color = color;
        return material;
    }

    static GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.Image>().color = color;
        return go;
    }

    static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, bool bestFit)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = bestFit;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = fontSize;
        text.lineSpacing = 1.05f;
        text.raycastTarget = false;

        return text;
    }

    static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
