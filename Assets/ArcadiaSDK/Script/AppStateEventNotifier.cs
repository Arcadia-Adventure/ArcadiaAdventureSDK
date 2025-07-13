using System;
using UnityEngine;

public enum AppState
{
    Foreground,
    Background
}

public class AppStateEventNotifier : MonoBehaviour
{
    public static event Action<AppState> AppStateChanged;
    private static AppStateEventNotifier _instance;
    
    public static AppStateEventNotifier Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AppStateEventNotifier");
                _instance = go.AddComponent<AppStateEventNotifier>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            AppStateChanged?.Invoke(AppState.Foreground);
        }
        else
        {
            AppStateChanged?.Invoke(AppState.Background);
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            AppStateChanged?.Invoke(AppState.Background);
        }
        else
        {
            AppStateChanged?.Invoke(AppState.Foreground);
        }
    }
}