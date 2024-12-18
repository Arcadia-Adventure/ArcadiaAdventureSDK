using System;
using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Setup;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class AA_AnalyticsManager : MonoBehaviour
{
    private static AA_AnalyticsManager _instance = null;

    static public AA_AnalyticsManager Agent
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType(typeof(AA_AnalyticsManager)) as AA_AnalyticsManager;
                if (_instance == null)
                {
                    GameObject obj = new GameObject("AA_AnalyticsManager");
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<AA_AnalyticsManager>();
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this.gameObject.GetComponent<AA_AnalyticsManager>();
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != _instance)
                Destroy(this.gameObject);
        }
    }

    public UnityEvent<bool> onGameAnalyticsInitialize;
    public UnityEvent<bool> onFirebaseInitialize;
    public void OnEnable()
    {
        InitializeAnalytics();
    }
    public void InitializeAnalytics()
    {
        GameAnalyticsManager.OnInitialize=onGameAnalyticsInitialize;
        FirebaseManager.onInitialize=onFirebaseInitialize;
        GameAnalyticsManager.Initialize();
        FirebaseManager.InitializeFirebase();
    }
    public void GameStartAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelStartEvent(levelNo);
        GameAnalyticsManager.GameStartAnalytics(levelNo);
    }
    public void GameFailAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelFailEvent(levelNo);
        GameAnalyticsManager.GameFailAnalytics(levelNo);
    }
    public void GameCompleteAnalytics(int levelNo)
    {
        FirebaseManager.LogLevelCompleteEvent(levelNo);
        GameAnalyticsManager.GameCompleteAnalytics(levelNo);
    }
    public void CustomEvent(string param, string value)
    {
        FirebaseManager.LogEvent("custom",param,value);
        GameAnalyticsManager.CustomEvent(param,value);
    }
}
