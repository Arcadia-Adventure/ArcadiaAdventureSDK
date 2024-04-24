using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

#if gameanalytics_enabled
using GameAnalyticsSDK;
#endif
public class AppOpenAdController : MonoBehaviour
{
    private static AppOpenAdController _agent;
    public static AppOpenAdController agent
    {
        get
        {
            if(_agent == null)
            {
                _agent = new GameObject(nameof(AppOpenAdController)).AddComponent<AppOpenAdController>();
                DontDestroyOnLoad(agent.gameObject);
            }
            return _agent;
        }
    }

    void Awake()
    {
        _agent = this;
    }
    private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromHours(4);
    private DateTime appOpenExpireTime;
    private AppOpenAd appOpenAd;
    public bool IsAppOpenAdAvailable
    {
        get
        {
            return (appOpenAd != null
                    && appOpenAd.CanShowAd()
                    && DateTime.Now < appOpenExpireTime);
        }
    }
    void RequestAndLoadAppOpenAd(bool useTestIDs=false)
    {
        ArcadiaSdkManager.PrintStatus("Requesting App Open ad.");
        string adUnitId = ArcadiaSdkManager.myGameIds.appOpenAdId;
        if (useTestIDs)
        {

#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/3419835294";
#elif UNITY_IPHONE
         adUnitId = "ca-app-pub-3940256099942544/5662855259";
#else
         adUnitId = "unexpected_platform";
#endif
        }
        // destroy old instance.
        if (appOpenAd != null)
        {
            DestroyAppOpenAd();
        }

        // Create a new app open ad instance.
        AppOpenAd.Load(adUnitId, new AdRequest(),
            (AppOpenAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    ArcadiaSdkManager.PrintStatus("App open ad failed to load with error: " +
                        loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    ArcadiaSdkManager.PrintStatus("App open ad failed to load.");
                    return;
                }

                ArcadiaSdkManager.PrintStatus("App Open ad loaded. Please background the app and return.");
                this.appOpenAd = ad;
                this.appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    ArcadiaSdkManager.PrintStatus("App open ad opened.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    ArcadiaSdkManager.PrintStatus("App open ad closed.");
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    ArcadiaSdkManager.PrintStatus("App open ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    ArcadiaSdkManager.PrintStatus("App open ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    ArcadiaSdkManager.PrintStatus("App open ad failed to show with error: " +
                        error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "App open ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    ArcadiaSdkManager.PrintStatus(msg);
                };
            });
    }

    public void DestroyAppOpenAd()
    {
        if (this.appOpenAd != null)
        {
            this.appOpenAd.Destroy();
            this.appOpenAd = null;
        }
    }

    public void ShowAppOpenAd()
    {

        if (!IsAppOpenAdAvailable)
        {
            RequestAndLoadAppOpenAd();
            return;
        }
        appOpenAd.Show();
        RequestAndLoadAppOpenAd();
    }
}
