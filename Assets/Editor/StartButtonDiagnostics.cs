#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class StartButtonDiagnostics
{
    [MenuItem("Tools/Start Button Diagnostics/Check LandingPage")]
    public static void CheckLandingPage()
    {
        string scenePath = "Assets/Scenes/LandingPage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("Could not open scene: " + scenePath);
            return;
        }

        Debug.Log("=== Start Button Diagnostics (LandingPage) ===");

        // Check SceneManager
        var sceneManagerGO = GameObject.Find("SceneManager");
        if (sceneManagerGO == null)
        {
            Debug.LogWarning("No GameObject named 'SceneManager' found in scene.");
        }
        else
        {
            var nav = sceneManagerGO.GetComponent<SceneNavigator>();
            if (nav == null) Debug.LogWarning("'SceneManager' exists but has no SceneNavigator component.");
            else Debug.Log("SceneManager with SceneNavigator found.");
        }

        // Find candidate Start GameObjects
        GameObject startGO = null;
        Button unityButton = null;
        StartButtonProxy proxy = null;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var children = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == null) continue;
                if (t.gameObject.name.ToLower().Contains("start"))
                {
                    startGO = t.gameObject;
                    unityButton = startGO.GetComponent<Button>();
                    proxy = startGO.GetComponent<StartButtonProxy>();
                    goto Report;
                }
            }
        }

    Report:
        if (startGO == null)
        {
            Debug.LogWarning("No GameObject with 'start' in its name found. Searching for any UI Button...");
            // fallback: find any Button
            foreach (var root in roots)
            {
                var buttons = root.GetComponentsInChildren<Button>(true);
                if (buttons != null && buttons.Length > 0)
                {
                    unityButton = buttons[0];
                    startGO = unityButton.gameObject;
                    proxy = startGO.GetComponent<StartButtonProxy>();
                    break;
                }
            }
        }

        if (startGO == null)
        {
            Debug.LogError("Could not find Start GameObject or any UI Button in the scene.");
            Debug.Log("Ensure the Start button GameObject name contains 'Start' or add a UnityEngine.UI.Button component to it.");
            return;
        }

        Debug.Log("Found Start GameObject: " + startGO.name + " (path: " + GetGameObjectPath(startGO) + ")");

        if (unityButton != null)
        {
            Debug.Log("- Unity UI Button component found on Start GameObject.");
            // List persistent listeners count if possible
            try
            {
                var count = unityButton.onClick.GetPersistentEventCount();
                Debug.Log("- Unity Button.onClick persistent listener count: " + count);
                for (int i = 0; i < count; i++)
                {
                    var target = unityButton.onClick.GetPersistentTarget(i);
                    var methodName = unityButton.onClick.GetPersistentMethodName(i);
                    Debug.Log($"  Listener {i}: target={target}, method={methodName}");
                }
            }
            catch (System.Exception)
            {
                Debug.Log("- Could not read persistent listeners (Editor API difference). Check Inspector -> On Click() listeners.");
            }
        }
        else
        {
            Debug.Log("- No Unity UI Button component on Start GameObject.");
        }

        if (proxy != null)
        {
            Debug.Log("- StartButtonProxy component present.");
            Debug.Log("  proxy.navigator = " + (proxy.navigator != null ? proxy.navigator.name : "null"));
        }
        else
        {
            Debug.Log("- No StartButtonProxy component on Start GameObject.");
        }

        Debug.Log("=== End Diagnostics ===");
    }

    static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
#endif
