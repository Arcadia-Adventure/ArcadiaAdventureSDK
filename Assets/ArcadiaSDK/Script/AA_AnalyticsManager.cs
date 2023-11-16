using System;
using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Setup;
using UnityEngine;
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
        if(_instance == null)
        {	
            _instance = this.gameObject.GetComponent<AA_AnalyticsManager>();
            _instance.InIt();
            DontDestroyOnLoad(this);
        }
        else
        {
            if(this != _instance)
                Destroy(this.gameObject);
        }
    }

   



    public void InIt()
    {
        GameAnalytics.Initialize();
        GameAnalyticsILRD.SubscribeMaxImpressions();
        
     
        if(ArcadiaSdkManager.Agent.enableLogs)
           Debug.Log(":* GameAnalytics Initialized!");
    }

    public void GameStartAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start,"Level_Start",levelNo.ToString(),levelNo);
    }
    public void GameFailAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail,"Level_Fail",levelNo.ToString(),levelNo);
    }
    public void GameCompleteAnalytics(int levelNo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete,"Level_Complete",levelNo.ToString(),levelNo);
    }

    public void AdTrackingAnalytics(String adType,String location)
    {
        
    }
    

    #region GAIDs

    public static void SetGAIds()
    {

        for (; 0< GameAnalytics.SettingsGA.Platforms.Count; )
        {
            GameAnalytics.SettingsGA.RemovePlatformAtIndex(0);
        }

        if (ArcadiaSdkManager.myGameIds.platform=="Android")
        {
            GameAnalytics.SettingsGA.AddPlatform(RuntimePlatform.Android);
        }
        else if (ArcadiaSdkManager.myGameIds.platform=="IOS")
        {
            GameAnalytics.SettingsGA.AddPlatform(RuntimePlatform.IPhonePlayer);
        }
        
        GameAnalytics.SettingsGA.UpdateGameKey(0,ArcadiaSdkManager.myGameIds.gameKey_GameAnaytics);
        GameAnalytics.SettingsGA.UpdateSecretKey(0,ArcadiaSdkManager.myGameIds.secretKey_GameAnaytics);
        
        GameAnalytics.SettingsGA.SubmitFpsAverage = true;
        GameAnalytics.SettingsGA.SubmitFpsCritical = true;
        GameAnalytics.SettingsGA.NativeErrorReporting = true;
        GameAnalytics.SettingsGA.SubmitErrors = true;
        GameAnalytics.SettingsGA.InfoLogBuild = true;
        GameAnalytics.SettingsGA.InfoLogEditor = true;
        GameAnalytics.SettingsGA.UsePlayerSettingsBuildNumber = true;
        GameAnalytics.SettingsGA.FpsCriticalThreshold = 30;
            
    }

    #endregion
    
    
    
}
