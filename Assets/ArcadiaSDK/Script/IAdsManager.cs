using System;
using UnityEngine;

public interface IAdsManager
{
    // Initialization
    void Initialize(string sdkKey, bool enableLogs = false);
    bool IsInitialized { get; }
    
    // Banner Ads
    void LoadBanner(string adUnitId, BannerPosition position = BannerPosition.Bottom);
    void ShowBanner(string adUnitId);
    void HideBanner(string adUnitId);
    void DestroyBanner(string adUnitId);
    bool IsBannerLoaded(string adUnitId);
    
    // Interstitial Ads
    void LoadInterstitial(string adUnitId);
    void ShowInterstitial(string adUnitId, Action onSuccess = null, Action onFail = null);
    bool IsInterstitialLoaded(string adUnitId);
    
    // Rewarded Ads
    void LoadRewarded(string adUnitId);
    void ShowRewarded(string adUnitId, Action<int> onSuccess = null, Action onFail = null);
    bool IsRewardedLoaded(string adUnitId);
    
    // MRec Ads
    void LoadMRec(string adUnitId, BannerPosition position = BannerPosition.Center);
    void ShowMRec(string adUnitId);
    void HideMRec(string adUnitId);
    void DestroyMRec(string adUnitId);
    bool IsMRecLoaded(string adUnitId);
    
    // App Open Ads
    void LoadAppOpen(string adUnitId);
    void ShowAppOpen(string adUnitId, Action onSuccess = null, Action onFail = null);
    bool IsAppOpenLoaded(string adUnitId);
    
    // Events
    event Action<string> OnAdLoaded;
    event Action<string, string> OnAdFailedToLoad;
    event Action<string> OnAdShown;
    event Action<string> OnAdClosed;
    event Action<string> OnAdClicked;
    event Action<string, int> OnRewardedAdRewarded;
    event Action<string, double> OnAdRevenuePaid;
}

public enum BannerPosition
{
    Top,
    Bottom,
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}