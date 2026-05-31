using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void LoadHome()
    {
        MobileAppExperience.LoadSceneWithMobileFlow("Home");
    }

    public void LoadSceneByName(string sceneName)
    {
        if (sceneName == "Scanner")
            MobileAppExperience.LoadScannerWithCameraFlow();
        else
            MobileAppExperience.LoadSceneWithMobileFlow(sceneName);
    }
}
