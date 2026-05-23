#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.SceneManagement;
using System.Linq;

public static class AutoWireStartButton
{
    [MenuItem("Tools/Auto Wire Start Button -> LandingPage")] 
    public static void WireStartButton()
    {
        string scenePath = "Assets/Scenes/LandingPage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("Could not open scene: " + scenePath);
            return;
        }

        // Ensure a SceneNavigator exists
        GameObject sceneManager = GameObject.Find("SceneManager");
        if (sceneManager == null)
        {
            sceneManager = new GameObject("SceneManager");
            sceneManager.AddComponent<SceneNavigator>();
            Debug.Log("Created SceneManager with SceneNavigator component.");
        }

        // Find UI Buttons in the scene
        var rootObjects = scene.GetRootGameObjects();
        Button targetButton = null;

        foreach (var root in rootObjects)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                // prefer buttons with "start" in name or child text == "Start"
                if (b.gameObject.name.ToLower().Contains("start") || HasChildText(b.gameObject, "start"))
                {
                    targetButton = b;
                    break;
                }
                if (targetButton == null) targetButton = b; // fallback
            }
            if (targetButton != null) break;
        }

        // If we found a Unity Button, wire it. Also attach StartButtonProxy for robustness.
        var nav = sceneManager.GetComponent<SceneNavigator>();
        if (targetButton != null)
        {
            UnityAction act = nav.LoadHome;
            UnityEventTools.AddPersistentListener(targetButton.onClick, act);
            // Ensure StartButtonProxy exists on the button GameObject
            var proxy = targetButton.gameObject.GetComponent<StartButtonProxy>();
            if (proxy == null) proxy = targetButton.gameObject.AddComponent<StartButtonProxy>();
            proxy.navigator = nav;
            Debug.Log("Wired UnityEngine.UI.Button '" + targetButton.gameObject.name + "' to SceneNavigator.LoadHome");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // No Unity Button found: search for any GameObject named like "start" and attach a proxy
        GameObject startGO = null;
        foreach (var root in rootObjects)
        {
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t == null) continue;
                if (t.gameObject.name.ToLower().Contains("start"))
                {
                    startGO = t.gameObject;
                    break;
                }
            }
            if (startGO != null) break;
        }

        if (startGO != null)
        {
            var proxy = startGO.GetComponent<StartButtonProxy>();
            if (proxy == null) proxy = startGO.AddComponent<StartButtonProxy>();
            proxy.navigator = nav;
            Debug.Log("Added StartButtonProxy to '" + startGO.name + "' and wired to SceneNavigator.LoadHome");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // As a last attempt, try to find ButtonPro components and attach proxy to their GameObject
        foreach (var root in rootObjects)
        {
            var comps = root.GetComponentsInChildren<Component>(true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                var typeName = c.GetType().Name;
                if (typeName == "ButtonPro")
                {
                    var go = c.gameObject;
                    var proxy = go.GetComponent<StartButtonProxy>();
                    if (proxy == null) proxy = go.AddComponent<StartButtonProxy>();
                    proxy.navigator = nav;
                    // Try to wire ButtonPro.onClick UnityEvent if it exists via reflection
                    var field = c.GetType().GetField("onClick");
                    if (field != null)
                    {
                        var unityEvent = field.GetValue(c) as UnityEvent;
                        if (unityEvent != null)
                        {
                            UnityAction action = proxy.Trigger;
                            UnityEventTools.AddPersistentListener(unityEvent, action);
                            Debug.Log("Wired ButtonPro.onClick to StartButtonProxy.Trigger on '" + go.name + "'");
                        }
                    }
                    Debug.Log("Added StartButtonProxy to ButtonPro '" + go.name + "'");
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    return;
                }
            }
        }

        Debug.LogWarning("Could not find Start button. Please ensure the Start button GameObject name contains 'Start' or is a UnityEngine.UI.Button.");
    }

    static bool HasChildText(GameObject go, string expected)
    {
        expected = expected.ToLower();
        var texts = go.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        foreach (var t in texts) if (t.text != null && t.text.ToLower().Contains(expected)) return true;
#if TMP_PRESENT
        var tmpTexts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
        foreach (var tt in tmpTexts) if (tt.text != null && tt.text.ToLower().Contains(expected)) return true;
#endif
        return false;
    }
}
#endif
