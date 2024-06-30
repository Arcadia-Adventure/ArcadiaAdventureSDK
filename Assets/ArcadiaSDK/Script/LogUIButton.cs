using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LogUIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float holdDuration = 10f; // Duration in seconds to hold the button to activate logs panel

    private bool pointerDown = false;
    private float pointerDownTime = 0f;

    private void Update()
    {
        if (pointerDown)
        {
            // Check if the button is still being held down
            if (Time.time - pointerDownTime >= holdDuration)
            {
                ActivateLogsPanel();
                pointerDown = false; // Reset button state after activation
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset button state and timer if released early
        pointerDown = false;
        pointerDownTime = 0f;
    }

    private void ActivateLogsPanel()
    {
        // Activate the logs panel
        if(ArcadiaSdkManager.Agent.GetLog())
        {
            ArcadiaSdkManager.Agent.SetLog(false);
            LogsSetting.OnAfterSceneLoadRuntimeMethod();
        }
        else
        {
            ArcadiaSdkManager.Agent.SetLog(true);
            LogsSetting.OnAfterSceneLoadRuntimeMethod();
        }
    }
}
