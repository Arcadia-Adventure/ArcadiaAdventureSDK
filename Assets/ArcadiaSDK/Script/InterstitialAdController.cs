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
public class InterstitialAdController : MonoBehaviour
{
    private static InterstitialAdController _agent;
    public static InterstitialAdController agent
    {
        get
        {
            if(_agent == null)
            {
                _agent = new GameObject(nameof(InterstitialAdController)).AddComponent<InterstitialAdController>();
                DontDestroyOnLoad(agent.gameObject);
            }

            return _agent;
        }
    }

    void Awake()
    {
        _agent = this;
    }
    public InterstitialAd interstitialAd;
    private Action successCallBack,failCallBack;
    private bool preCache;
    #region INTERSTITIAL ADS
    
    public void RequestAndLoadInterstitialAd(Action<bool> OnLoad=null,bool useTestIDs=false)
    {
       ArcadiaSdkManager.PrintStatus("Requesting Interstitial ad.");
#if gameanalytics_enabled

        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.Interstitial, "undefine", "undefine");
#endif
        string adUnitId = ArcadiaSdkManager.myGameIds.interstitialAdId;
        if (useTestIDs)
        {
#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IPHONE
         adUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
         adUnitId = "unexpected_platform";
#endif
        }
        // Clean up interstitial before using it
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        // Load an interstitial ad
        InterstitialAd.Load(adUnitId, new AdRequest(),
            (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
#if gameanalytics_enabled
                    GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, loadError.GetMessage());
                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.NoFill);
#endif
                    ArcadiaSdkManager.PrintStatus("Interstitial ad failed to load with error: " +
                        loadError.GetMessage());
                        failCallBack?.Invoke();
                        OnLoad?.Invoke(false);
                    return;
                }
                else if (ad == null)
                {
#if gameanalytics_enabled

                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.InternalError);
#endif
                    ArcadiaSdkManager.PrintStatus("Interstitial ad failed to load.");
                    failCallBack?.Invoke();
                    OnLoad?.Invoke(false);
                    return;
                }
                ArcadiaSdkManager.PrintStatus("Interstitial ad loaded.");
                interstitialAd = ad;
                OnLoad?.Invoke(true);
#if gameanalytics_enabled

                if (GameAnalytics.Initialized)
                {
                    GameAnalytics.NewAdEvent(GAAdAction.Loaded, GAAdType.Interstitial, ad.GetResponseInfo().GetLoadedAdapterResponseInfo().AdSourceName, "undefine");
                    GameAnalyticsILRD.SubscribeAdMobImpressions(adUnitId, interstitialAd);
                }
#endif
                RegisterEventHandlers(interstitialAd);
                RegisterReloadHandler(interstitialAd);
            });
    }
    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is estimated to have earned money.
        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            Dictionary<string, object> paidData = new Dictionary<string, object>();
            paidData.Add(adValue.CurrencyCode, adValue.Value);
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Undefined, GAAdType.Interstitial, "undefine", "undefine", paidData, true);
#endif
            ArcadiaSdkManager.PrintStatus(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "undefine", "undefine");
#endif
            ArcadiaSdkManager.PrintStatus("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Interstitial, "undefine", "undefine");
#endif
            ArcadiaSdkManager.PrintStatus("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            ArcadiaSdkManager.PrintStatus("Interstitial ad full screen content opened.");
        };
    }
    private void RegisterReloadHandler(InterstitialAd interstitialAd)
    {
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            // Reload the ad so that we can show another as soon as possible.
            if(preCache)RequestAndLoadInterstitialAd();
            successCallBack?.Invoke();
            ArcadiaSdkManager.PrintStatus("Interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            // Reload the ad so that we can show another as soon as possible.
            failCallBack?.Invoke();
            if(preCache)RequestAndLoadInterstitialAd();
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.InvalidRequest);
#endif
            Debug.LogError("Interstitial ad failed to open full screen content " + "with error : " + error);
        };
    }

    public void ShowInterstitialAd(Action _interstitialCallBack = null,Action _interstitialFailCallBack=null, bool useTestIDs=false,bool _preCache=true)
    {
        preCache=_preCache;
        successCallBack = _interstitialCallBack;
        failCallBack = _interstitialFailCallBack;
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.UnableToPrecache);
#endif
            ArcadiaSdkManager.PrintStatus("Interstitial ad is not ready yet.");
            RequestAndLoadInterstitialAd((bool onload)=>
            {
                if(!preCache)
                {
                    interstitialAd.Show();
                }
            },useTestIDs);
        }
    }

    public void DestroyInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }
    }

    #endregion
}
