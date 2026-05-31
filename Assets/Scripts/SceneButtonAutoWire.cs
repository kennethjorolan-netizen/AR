using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneButtonAutoWire
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        WireScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WireScene(scene);
    }

    static void WireScene(Scene scene)
    {
        var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null || button.gameObject.scene != scene)
                continue;

            if (!TryResolveAction(button, scene.name, out var targetScene, out var opensPlantDetails))
                continue;

            var action = button.GetComponent<SceneButtonAutoAction>();
            if (action == null)
                action = button.gameObject.AddComponent<SceneButtonAutoAction>();

            action.Configure(targetScene, opensPlantDetails);
            button.onClick.RemoveListener(action.Invoke);
            button.onClick.AddListener(action.Invoke);
        }
    }

    static bool TryResolveAction(Button button, string sceneName, out string targetScene, out bool opensPlantDetails)
    {
        targetScene = null;
        opensPlantDetails = false;

        var searchText = Normalize(BuildSearchText(button));

        if (searchText.Contains("view detail"))
        {
            opensPlantDetails = string.Equals(sceneName, "Plants", StringComparison.OrdinalIgnoreCase);
            return opensPlantDetails;
        }

        if (searchText.Contains("start"))
        {
            targetScene = "Home";
            return true;
        }

        if (searchText.Contains("back") || searchText.Contains("home"))
        {
            targetScene = "Home";
            return !string.Equals(sceneName, "Home", StringComparison.OrdinalIgnoreCase);
        }

        if (searchText.Contains("scan plant") || searchText.Contains("scanner"))
        {
            targetScene = "Scanner";
            return true;
        }

        if (searchText.Contains("plant anatomy") || searchText.Contains("plants anatomy") || searchText.Contains("anatomyplants"))
        {
            targetScene = "Plants";
            return true;
        }

        if (searchText.Contains("about app") || searchText.Contains("about"))
        {
            targetScene = "About";
            return true;
        }

        return false;
    }

    static string BuildSearchText(Button button)
    {
        var builder = new StringBuilder(button.gameObject.name);

        foreach (var text in button.GetComponentsInChildren<Text>(true))
            builder.Append(' ').Append(text.text);

        foreach (var component in button.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                continue;

            var type = component.GetType();
            if (!type.FullName.StartsWith("TMPro.", StringComparison.Ordinal))
                continue;

            var textProperty = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (textProperty != null)
                builder.Append(' ').Append(textProperty.GetValue(component, null));
        }

        for (var parent = button.transform.parent; parent != null; parent = parent.parent)
            builder.Append(' ').Append(parent.name);

        return builder.ToString();
    }

    static string Normalize(string text)
    {
        return (text ?? string.Empty)
            .ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Replace("  ", " ");
    }

    public static void LoadSceneIfAvailable(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!SceneExistsInBuildSettings(sceneName))
        {
            Debug.LogWarning($"SceneButtonAutoWire could not load '{sceneName}' because it is not in Build Settings.");
            return;
        }

        if (string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(sceneName, "Scanner", StringComparison.OrdinalIgnoreCase))
            MobileAppExperience.LoadScannerWithCameraFlow();
        else
            MobileAppExperience.LoadSceneWithMobileFlow(sceneName);
    }

    static bool SceneExistsInBuildSettings(string sceneName)
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static string FindPlantName(GameObject source)
    {
        for (var current = source.transform; current != null; current = current.parent)
        {
            var resolved = ResolvePlantName(current.name);
            if (!string.IsNullOrEmpty(resolved))
                return resolved;
        }

        return "Plant";
    }

    static string ResolvePlantName(string name)
    {
        var normalized = Normalize(name);

        if (normalized.Contains("mango")) return "Mango";
        if (normalized.Contains("cactus")) return "Cactus";
        if (normalized.Contains("tomato")) return "Tomato";
        if (normalized.Contains("neem")) return "Neem Tree";
        if (normalized.Contains("aloe")) return "Aloe Vera";
        if (normalized.Contains("rice")) return "Rice Plant";
        if (normalized.Contains("rose")) return "Rose";
        if (normalized.Contains("sunflower")) return "Sunflower";
        if (normalized.Contains("wheat")) return "Wheat";
        if (normalized.Contains("banana")) return "Banana Plant";

        return null;
    }
}

[DisallowMultipleComponent]
public sealed class SceneButtonAutoAction : MonoBehaviour
{
    [SerializeField] string targetScene;
    [SerializeField] bool opensPlantDetails;

    public void Configure(string sceneName, bool showPlantDetails)
    {
        targetScene = sceneName;
        opensPlantDetails = showPlantDetails;
    }

    public void Invoke()
    {
        if (opensPlantDetails)
        {
            PlantDetailsOverlay.Show(SceneButtonAutoWire.FindPlantName(gameObject));
            return;
        }

        SceneButtonAutoWire.LoadSceneIfAvailable(targetScene);
    }
}

public static class PlantDetailsOverlay
{
    static readonly Dictionary<string, string> PlantDetails = new Dictionary<string, string>
    {
        {
            "Mango",
            "Scientific name: Mangifera indica\n" +
            "Family: Anacardiaceae\n" +
            "Overview: Tropical fruit tree with deep roots, woody stem, broad leaves, flower clusters, and sweet fruits.\n" +
            "Anatomy: Roots absorb water; leaves make food; flowers form mango fruits after pollination.\n" +
            "Growth needs: Full sunlight, warm climate, well-drained soil, and regular watering while young.\n" +
            "Uses: Eaten fresh or made into juice, dried mango, desserts, and preserves.\n" +
            "Quick fact: A mature mango tree can produce many fruits in one season."
        },
        {
            "Cactus",
            "Family: Cactaceae\n" +
            "Overview: Desert-adapted plant with thick green stems, shallow roots, and spines instead of broad leaves.\n" +
            "Anatomy: Stem stores water and makes food; spines protect the plant and reduce water loss.\n" +
            "Growth needs: Bright sunlight, sandy soil, very little water, and good drainage.\n" +
            "Uses: Ornamental plant and a strong example of plant adaptation to dry habitats.\n" +
            "Quick fact: Many cactus plants can survive long dry periods by saving water in their stems."
        },
        {
            "Tomato",
            "Scientific name: Solanum lycopersicum\n" +
            "Family: Solanaceae\n" +
            "Overview: Flowering plant with soft stems, green leaves, yellow flowers, and juicy red fruits.\n" +
            "Anatomy: Roots absorb water; leaves make food; flowers turn into tomato fruits after pollination.\n" +
            "Growth needs: Full sun, fertile soil, support stakes, and steady watering.\n" +
            "Uses: Sauces, salads, soups, ketchup, and many cooked dishes.\n" +
            "Quick fact: Tomato is botanically a fruit because it contains seeds."
        },
        {
            "Neem Tree",
            "Scientific name: Azadirachta indica\n" +
            "Family: Meliaceae\n" +
            "Overview: Hardy tree with bitter compound leaves, small white flowers, fruits, and seeds.\n" +
            "Anatomy: Roots anchor the tree; branches hold leaves; flowers develop into small seed fruits.\n" +
            "Growth needs: Strong sunlight, warm climate, and soil that does not stay waterlogged.\n" +
            "Uses: Traditional remedies, natural pest control, soaps, and plant-care products.\n" +
            "Quick fact: Neem can tolerate hot and dry conditions."
        },
        {
            "Aloe Vera",
            "Scientific name: Aloe barbadensis miller\n" +
            "Family: Asphodelaceae\n" +
            "Overview: Succulent plant with short roots and thick fleshy leaves that store water and clear gel.\n" +
            "Anatomy: Leaves store gel; roots absorb limited water; mature plants may grow flower stalks.\n" +
            "Growth needs: Bright light, dry soil between watering, and a pot with drainage.\n" +
            "Uses: Aloe gel is commonly used for skin care and cooling minor skin irritation.\n" +
            "Quick fact: Aloe vera should not be overwatered."
        },
        {
            "Rice Plant",
            "Scientific name: Oryza sativa\n" +
            "Family: Poaceae\n" +
            "Overview: Cereal grass and one of the most important staple foods in the world.\n" +
            "Anatomy: Fibrous roots hold muddy soil; stems support leaves; panicles carry rice grains.\n" +
            "Growth needs: Warm temperature, plenty of water, sunlight, and nutrient-rich soil.\n" +
            "Uses: Cooked as food and processed into flour, noodles, snacks, and rice products.\n" +
            "Quick fact: Many rice varieties grow well in flooded fields."
        },
        {
            "Rose",
            "Genus: Rosa\n" +
            "Family: Rosaceae\n" +
            "Overview: Flowering shrub with woody stems, leaves, thorns, and colorful fragrant flowers.\n" +
            "Anatomy: Roots absorb water; stems support buds; leaves make food; flowers attract pollinators.\n" +
            "Growth needs: Morning sun, fertile soil, regular watering, and pruning.\n" +
            "Uses: Gardens, bouquets, perfumes, decorations, and ornamental displays.\n" +
            "Quick fact: Pruning helps roses produce more flowers."
        },
        {
            "Sunflower",
            "Scientific name: Helianthus annuus\n" +
            "Family: Asteraceae\n" +
            "Overview: Tall plant with a strong stem, broad leaves, and a large yellow flower head.\n" +
            "Anatomy: Roots support the stem; leaves capture sunlight; the flower head produces seeds.\n" +
            "Growth needs: Full sunlight, loose soil, space for roots, and regular watering.\n" +
            "Uses: Seeds for snacks and oil; flowers for decoration and garden pollinators.\n" +
            "Quick fact: Young sunflower heads may turn toward the sun."
        },
        {
            "Wheat",
            "Scientific name: Triticum aestivum\n" +
            "Family: Poaceae\n" +
            "Overview: Cereal grass grown for edible grains, narrow leaves, stems, and golden grain heads.\n" +
            "Anatomy: Roots absorb nutrients; stems support leaves; grain heads hold kernels.\n" +
            "Growth needs: Sunlight, cool growing season, fertile soil, and moderate water.\n" +
            "Uses: Ground into flour for bread, noodles, pasta, biscuits, and other foods.\n" +
            "Quick fact: Wheat is one of the main crops used to make flour."
        },
        {
            "Banana Plant",
            "Genus: Musa\n" +
            "Family: Musaceae\n" +
            "Overview: Large herbaceous plant with a pseudostem, huge leaves, flower stalk, and fruit clusters.\n" +
            "Anatomy: Roots and corm support growth; leaves make food; flowers produce banana hands.\n" +
            "Growth needs: Warm climate, rich soil, steady moisture, and protection from strong wind.\n" +
            "Uses: Eaten fresh, cooked, or made into chips, desserts, and drinks; leaves wrap food.\n" +
            "Quick fact: After fruiting, new shoots grow from the base."
        },
        {
            "Plant",
            "Overview: Plants are living organisms that make food using sunlight, water, and carbon dioxide.\n" +
            "Main parts: Roots absorb water, stems support growth, leaves make food, flowers help reproduction, and fruits protect seeds.\n" +
            "Growth needs: Light, water, air, nutrients, and enough space.\n" +
            "Quick fact: Healthy plants help produce oxygen and support many living things."
        }
    };

    static GameObject currentOverlay;

    public static void Show(string plantName)
    {
        if (currentOverlay != null)
            UnityEngine.Object.Destroy(currentOverlay);

        var canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("PlantDetailsOverlay could not find a Canvas in this scene.");
            return;
        }

        plantName = string.IsNullOrWhiteSpace(plantName) ? "Plant" : plantName;
        var detail = PlantDetails.TryGetValue(plantName, out var text) ? text : PlantDetails["Plant"];

        currentOverlay = CreateOverlay(canvas.transform, plantName, detail);
    }

    static Canvas FindCanvas()
    {
        var activeScene = SceneManager.GetActiveScene();
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.scene == activeScene)
                return canvas;
        }

        return null;
    }

    static GameObject CreateOverlay(Transform parent, string title, string body)
    {
        var overlay = CreateImageObject("Plant Detail Overlay", parent, new Color(0f, 0f, 0f, 0.6f));
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Button>().onClick.AddListener(Dismiss);

        var panel = CreateImageObject("Plant Detail Panel", overlay.transform, new Color(0.94f, 0.98f, 0.92f, 1f));
        panel.GetComponent<Image>().raycastTarget = true;
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.04f, 0.06f);
        panelRect.anchorMax = new Vector2(0.96f, 0.94f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var titleText = CreateText("Title", panel.transform, title, 54, new Color(0.03f, 0.24f, 0.08f, 1f), TextAnchor.MiddleLeft, true);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.08f, 0.88f);
        titleRect.anchorMax = new Vector2(0.74f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var closeButton = CreateButton("Close Button", panel.transform, "X", 34);
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.84f, 0.9f);
        closeRect.anchorMax = new Vector2(0.95f, 0.98f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        closeButton.onClick.AddListener(Dismiss);

        CreateDetailSections(panel.transform, body);

        overlay.transform.SetAsLastSibling();

        return overlay;
    }

    static void CreateDetailSections(Transform parent, string body)
    {
        var lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var top = 0.82f;
        var gap = 0.006f;

        for (var i = 0; i < lines.Length && top > 0.06f; i++)
        {
            var fontSize = i < 2 ? 30 : 29;
            var height = GetDetailLineHeight(lines[i]);
            var bottom = Mathf.Max(0.06f, top - height);
            var lineText = CreateText(
                $"Detail Line {i + 1}",
                parent,
                lines[i],
                fontSize,
                new Color(0f, 0.08f, 0.02f, 1f),
                TextAnchor.UpperLeft,
                true);

            lineText.lineSpacing = 0.9f;
            lineText.supportRichText = false;
            lineText.raycastTarget = false;
            lineText.resizeTextMinSize = 21;
            lineText.resizeTextMaxSize = fontSize;
            lineText.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = lineText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, bottom);
            rect.anchorMax = new Vector2(0.92f, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            top = bottom - gap;
        }
    }

    static float GetDetailLineHeight(string line)
    {
        if (line.Length > 95) return 0.13f;
        if (line.Length > 72) return 0.11f;
        if (line.Length > 48) return 0.09f;
        return 0.058f;
    }

    static GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateScrollView(Transform parent, string body)
    {
        var scrollView = new GameObject("Details Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollView.transform.SetParent(parent, false);
        scrollView.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        var viewport = CreateImageObject("Viewport", scrollView.transform, new Color(1f, 1f, 1f, 0f));
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>());

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, -980f);
        contentRect.offsetMax = Vector2.zero;

        var detailsText = CreateText("Details", content.transform, body, 16, new Color(0.04f, 0.16f, 0.06f, 1f), TextAnchor.UpperLeft, false);
        detailsText.supportRichText = true;
        detailsText.lineSpacing = 1.12f;
        detailsText.verticalOverflow = VerticalWrapMode.Overflow;

        var detailsRect = detailsText.GetComponent<RectTransform>();
        detailsRect.anchorMin = Vector2.zero;
        detailsRect.anchorMax = Vector2.one;
        detailsRect.offsetMin = new Vector2(8f, 0f);
        detailsRect.offsetMax = new Vector2(-8f, -8f);

        var scroll = scrollView.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        return scrollView;
    }

    static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, bool bestFit)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = bestFit;
        text.resizeTextMinSize = bestFit ? 8 : 12;
        text.resizeTextMaxSize = fontSize;

        return text;
    }

    static Button CreateButton(string name, Transform parent, string label, int fontSize)
    {
        var go = CreateImageObject(name, parent, new Color(0.13f, 0.45f, 0.18f, 1f));
        var button = go.AddComponent<Button>();
        var labelText = CreateText("Label", go.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
        Stretch(labelText.GetComponent<RectTransform>());
        return button;
    }

    public static bool DismissIfVisible()
    {
        if (currentOverlay == null)
            return false;

        Dismiss();
        return true;
    }

    static void Dismiss()
    {
        if (currentOverlay == null)
            return;

        UnityEngine.Object.Destroy(currentOverlay);
        currentOverlay = null;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Font GetBuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
