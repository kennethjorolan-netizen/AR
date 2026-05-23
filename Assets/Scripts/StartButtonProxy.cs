using UnityEngine;
using UnityEngine.EventSystems;

public class StartButtonProxy : MonoBehaviour, IPointerClickHandler
{
    public SceneNavigator navigator;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("StartButtonProxy: OnPointerClick fired on '" + gameObject.name + "'.");
        if (navigator != null) navigator.LoadHome();
    }

    // Allows wiring from UnityEvent as well
    public void Trigger()
    {
        Debug.Log("StartButtonProxy: Trigger called on '" + gameObject.name + "'.");
        if (navigator != null) navigator.LoadHome();
    }
}
