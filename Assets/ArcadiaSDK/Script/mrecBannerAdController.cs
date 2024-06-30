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

public class MRecBannerAdController : MonoBehaviour
{
    private static MRecBannerAdController _agent;
    public static MRecBannerAdController Agent
    {
        get
        {
            if(_agent == null)
            {
                _agent = new GameObject(nameof(MRecBannerAdController)).AddComponent<MRecBannerAdController>();
                DontDestroyOnLoad(Agent.gameObject);
            }

            return _agent;
        }
    }

    void Awake()
    {
        _agent = this;
    }
    public BannerView bannerView;
    public bool isShowing;
    public Action<bool> OnShow;

    public void RequestBannerAd(BannerType bannerType, AdPosition adPosition, bool useTestIDs = false)
    {
        ArcadiaSdkManager.PrintStatus("Requesting Banner ad.");

        // These ad units are configured to always serve test ads.
        string adUnitId = ArcadiaSdkManager.myGameIds.mrecAdId;
        if (useTestIDs)
        {

#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
         adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        adUnitId = "unexpected_platform";
#endif
        }
        // Clean up banner before reusing
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
#if gameanalytics_enabled

        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.Banner, "undefine", "undefine");
#endif
        // Create a 320x50 banner at top of the screen
        switch (bannerType)
        {
            case BannerType.AdoptiveBanner:
                bannerView = new BannerView(adUnitId, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition);
                break;
            case BannerType.SmartBanner:
                bannerView = new BannerView(adUnitId, AdSize.SmartBanner, adPosition);
                break;
            case BannerType.Banner:
                bannerView = new BannerView(adUnitId, AdSize.Banner, adPosition);
                break;

            case BannerType.IABBanner:
                bannerView = new BannerView(adUnitId, AdSize.IABBanner, adPosition);
                break;

            case BannerType.Leaderboard:
                bannerView = new BannerView(adUnitId, AdSize.Leaderboard, adPosition);
                break;

            case BannerType.MediumRectangle:
                bannerView = new BannerView(adUnitId, AdSize.MediumRectangle, adPosition);
                break;
        }
        // Add Event Handlers
        bannerView.OnBannerAdLoaded += () =>
        {
            OnBannerShow(true);
#if gameanalytics_enabled
            if (GameAnalytics.Initialized)
            {
                GameAnalytics.NewAdEvent(GAAdAction.Loaded, GAAdType.Banner, bannerView.GetResponseInfo().GetLoadedAdapterResponseInfo().AdSourceName, "undefine");
                GameAnalyticsILRD.SubscribeAdMobImpressions(adUnitId, bannerView);
            }
#endif
            ArcadiaSdkManager.PrintStatus("Banner ad loaded.");
        };
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            OnBannerShow(false);
#if gameanalytics_enabled
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, error.GetMessage());
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Banner, "undefine", "undefine", GAAdError.InvalidRequest);
#endif
            ArcadiaSdkManager.PrintStatus("Banner ad failed to load with error: " + error.GetMessage());
        };
        bannerView.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner, "undefine", "undefine");
#endif
        };
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            ArcadiaSdkManager.PrintStatus("Banner ad opening.");
        };
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            ArcadiaSdkManager.PrintStatus("Banner ad closed.");
        };
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Dictionary<string, object> paidData = new Dictionary<string, object>();
            paidData.Add(adValue.CurrencyCode, adValue.Value);
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Undefined, GAAdType.Banner, "undefine", "undefine", paidData, true);
#endif
            string msg = string.Format("{0} (currency: {1}, value: {2}",
                                        "Banner ad received a paid event.",
                                        adValue.CurrencyCode,
                                        adValue.Value);
            ArcadiaSdkManager.PrintStatus(msg);
        };

        // Load a banner ad
        bannerView.LoadAd(new AdRequest());
    }
    public void ShowBanner(BannerType bannerType, AdPosition adPosition, bool useTestIDs)
    {
        if (bannerView != null)
        {
            OnBannerShow(true);
            bannerView.Show();
        }
        else
            RequestBannerAd(bannerType,adPosition,useTestIDs);
    }
    public void HideBanner()
    {
        if (bannerView != null)
        {
            OnBannerShow(false);
            bannerView.Hide();
        }
    }
    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            OnBannerShow(false);
            bannerView.Destroy();
            bannerView=null;
        }
    }
    void OnBannerShow(bool show)
    {
        isShowing=show;
        OnShow?.Invoke(show);
    }
}
