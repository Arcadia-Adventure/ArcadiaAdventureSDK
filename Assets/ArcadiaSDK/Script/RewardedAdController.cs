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
public class RewardedAdController : MonoBehaviour
{
    private static RewardedAdController _agent;
    public static RewardedAdController agent
    {
        get
        {
            if(_agent == null)
            {
                _agent = new GameObject(nameof(RewardedAdController)).AddComponent<RewardedAdController>();
                DontDestroyOnLoad(agent.gameObject);
            }

            return _agent;
        }
    }

    void Awake()
    {
        _agent = this;
    }
    private RewardedAd rewardedAd;
    private Action<int> rewardedCallBack;
    private bool preCache;
    #region REWARDED ADS

    public void RequestAndLoadRewardedAd(Action<bool> Onload = null,bool useTestIDs=false)
    {
        ArcadiaSdkManager.PrintStatus("Requesting Rewarded ad.");
#if gameanalytics_enabled
        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
        string adUnitId = ArcadiaSdkManager.myGameIds.rewardedVideoAdId;
        if (useTestIDs)
        {
#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
        adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
        adUnitId = "unexpected_platform";
#endif
        }
        // create new rewarded ad instance
        RewardedAd.Load(adUnitId, new AdRequest(),
            (RewardedAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
#if gameanalytics_enabled
                    GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, loadError.GetMessage());
                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.NoFill);
#endif
                    ArcadiaSdkManager.PrintStatus("Rewarded ad failed to load with error: " + loadError.GetMessage());
                    Onload?.Invoke(false);
                    return;
                }
                else if (ad == null)
                {
#if gameanalytics_enabled

                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.InternalError);
#endif
                    ArcadiaSdkManager.PrintStatus("Rewarded ad failed to load.");
                    Onload?.Invoke(false);
                    return;
                }
                ArcadiaSdkManager.PrintStatus("Rewarded ad loaded.");
                rewardedAd = ad;
                Onload?.Invoke(true);
#if gameanalytics_enabled
                if (GameAnalytics.Initialized)
                {
                    GameAnalytics.NewAdEvent(GAAdAction.Loaded, GAAdType.RewardedVideo, ad.GetResponseInfo().GetLoadedAdapterResponseInfo().AdSourceName, "undefine");
                    GameAnalyticsILRD.SubscribeAdMobImpressions(adUnitId, rewardedAd);
                }
#endif
                RegisterEventHandlers(rewardedAd);
                RegisterReloadHandler(rewardedAd);
            });
    }
    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Dictionary<string, object> paidData = new Dictionary<string, object>();
            paidData.Add(adValue.CurrencyCode, adValue.Value);
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Undefined, GAAdType.RewardedVideo, "undefine", "undefine", paidData, true);
#endif
            ArcadiaSdkManager.PrintStatus(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
            ArcadiaSdkManager.PrintStatus("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
            ArcadiaSdkManager.PrintStatus("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            ArcadiaSdkManager.PrintStatus("Rewarded ad full screen content opened.");
        };
    }
    private void RegisterReloadHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            // Reload the ad so that we can show another as soon as possible.
            if(preCache)RequestAndLoadRewardedAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            // Reload the ad so that we can show another as soon as possible.
            if(preCache)RequestAndLoadRewardedAd();
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.InvalidRequest);
#endif
            rewardedCallBack?.Invoke(0);
            Debug.LogError("Rewarded ad failed to open full screen content " + "with error : " + error);
        };
    }
    public void ShowRewardedAd(Action<int> rewardSuccess = null, Action noVideoAvailable = null, bool useTestIDs=false, bool _preCache=true)
    {
        preCache=_preCache;
        rewardedCallBack = rewardSuccess;
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show(OnRewardComplete);
        }
        else
        {
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.UnableToPrecache);
#endif
            noVideoAvailable?.Invoke();
            ArcadiaSdkManager.PrintStatus("Rewarded ad is not ready yet.");
            RequestAndLoadRewardedAd((bool isLoaded) =>
            {
                if (isLoaded)
                {
                    if (!preCache)
                    {
                        rewardedAd.Show(OnRewardComplete);
                    }
                }
            },useTestIDs);
        }
    }
    public void OnRewardComplete(Reward reward)
    {
#if gameanalytics_enabled
        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
        rewardedCallBack?.Invoke((int)reward.Amount);
        ArcadiaSdkManager.PrintStatus("Rewarded ad granted a reward: " + reward.Amount);
    }
    #endregion
}
