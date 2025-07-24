#if UNITY_APPLOVIN
using System;
using System.Collections.Generic;
using UnityEngine;

public class AppLovinAdsManager : MonoBehaviour, IAdsManager
{
    private static AppLovinAdsManager _instance;
    private bool _isInitialized = false;
    private Dictionary<string, bool> _bannerStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> _mrecStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> _interstitialStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> _rewardedStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> _appOpenStates = new Dictionary<string, bool>();
    
    private Action<int> _currentRewardedCallback;
    private Action _currentInterstitialSuccessCallback;
    private Action _currentInterstitialFailCallback;
    private Action _currentAppOpenSuccessCallback;
    private Action _currentAppOpenFailCallback;
    
    public static AppLovinAdsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AppLovinAdsManager");
                _instance = go.AddComponent<AppLovinAdsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    public bool IsInitialized => _isInitialized;
    
    // Events
    public event Action<string> OnAdLoaded;
    public event Action<string, string> OnAdFailedToLoad;
    public event Action<string> OnAdShown;
    public event Action<string> OnAdClosed;
    public event Action<string> OnAdClicked;
    public event Action<string, int> OnRewardedAdRewarded;
    public event Action<string, double> OnAdRevenuePaid;
    
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
    
    #region Initialization
    
    public void Initialize(string sdkKey, bool enableLogs = false)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("AppLovin SDK is already initialized.");
            return;
        }
        MaxSdk.SetVerboseLogging(enableLogs);
        
        MaxSdk.InitializeSdk();
        
        RegisterCallbacks();
        _isInitialized = true;
        
        Debug.Log("AppLovin MAX SDK initialized successfully.");
    }
    
    private void RegisterCallbacks()
    {
        // Banner callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClicked;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaid;
        
        // Interstitial callbacks
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialAdLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialAdLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdShown;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialAdClosed;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAdClicked;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaid;
        
        // Rewarded callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdShown;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdClosed;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClicked;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReward;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaid;
        
        // MRec callbacks
        MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoaded;
        MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailed;
        MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClicked;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaid;
        
        // App Open callbacks
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoaded;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdShown;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdClosed;
        MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClicked;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailed;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaid;
    }
    
    #endregion
    
    #region Banner Ads
    
    public void LoadBanner(string adUnitId, BannerPosition position = BannerPosition.Bottom)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AppLovin SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Use the new AdViewConfiguration API
        MaxSdkBase.AdViewPosition bannerPosition = ConvertBannerPositionToAdView(position);
        MaxSdkBase.AdViewConfiguration adViewConfiguration = new MaxSdkBase.AdViewConfiguration(bannerPosition);
        MaxSdk.CreateBanner(adUnitId, adViewConfiguration);
        MaxSdk.LoadBanner(adUnitId);
        
        _bannerStates[adUnitId] = false; // Will be set to true when loaded
    }
    
    public void ShowBanner(string adUnitId)
    {
        if (IsBannerLoaded(adUnitId))
        {
            MaxSdk.ShowBanner(adUnitId);
        }
        else
        {
            Debug.LogWarning($"Banner with ID {adUnitId} is not loaded.");
        }
    }
    
    public void HideBanner(string adUnitId)
    {
        MaxSdk.HideBanner(adUnitId);
    }
    
    public void DestroyBanner(string adUnitId)
    {
        MaxSdk.DestroyBanner(adUnitId);
        _bannerStates.Remove(adUnitId);
    }
    
    public bool IsBannerLoaded(string adUnitId)
    {
        return _bannerStates.ContainsKey(adUnitId) && _bannerStates[adUnitId];
    }
    
    #endregion
    
    #region Interstitial Ads
    
    public void LoadInterstitial(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AppLovin SDK not initialized. Call Initialize() first.");
            return;
        }
        
        MaxSdk.LoadInterstitial(adUnitId);
        _interstitialStates[adUnitId] = false; // Will be set to true when loaded
    }
    
    public void ShowInterstitial(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsInterstitialLoaded(adUnitId))
        {
            _currentInterstitialSuccessCallback = onSuccess;
            _currentInterstitialFailCallback = onFail;
            MaxSdk.ShowInterstitial(adUnitId);
        }
        else
        {
            Debug.LogWarning($"Interstitial with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsInterstitialLoaded(string adUnitId)
    {
        return _interstitialStates.ContainsKey(adUnitId) && 
               _interstitialStates[adUnitId] && 
               MaxSdk.IsInterstitialReady(adUnitId);
    }
    
    #endregion
    
    #region Rewarded Ads
    
    public void LoadRewarded(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AppLovin SDK not initialized. Call Initialize() first.");
            return;
        }
        
        MaxSdk.LoadRewardedAd(adUnitId);
        _rewardedStates[adUnitId] = false; // Will be set to true when loaded
    }
    
    public void ShowRewarded(string adUnitId, Action<int> onSuccess = null, Action onFail = null)
    {
        if (IsRewardedLoaded(adUnitId))
        {
            _currentRewardedCallback = onSuccess;
            MaxSdk.ShowRewardedAd(adUnitId);
        }
        else
        {
            Debug.LogWarning($"Rewarded ad with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsRewardedLoaded(string adUnitId)
    {
        return _rewardedStates.ContainsKey(adUnitId) && 
               _rewardedStates[adUnitId] && 
               MaxSdk.IsRewardedAdReady(adUnitId);
    }
    
    #endregion
    
    #region MRec Ads
    
    public void LoadMRec(string adUnitId, BannerPosition position = BannerPosition.Center)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AppLovin SDK not initialized. Call Initialize() first.");
            return;
        }
        
        MaxSdkBase.AdViewPosition mrecPosition = ConvertMRecPosition(position);
        MaxSdkBase.AdViewConfiguration adViewConfiguration = new MaxSdkBase.AdViewConfiguration(mrecPosition);
        MaxSdk.CreateMRec(adUnitId, adViewConfiguration);
        MaxSdk.LoadMRec(adUnitId);
        
        _mrecStates[adUnitId] = false; // Will be set to true when loaded
    }
    
    public void ShowMRec(string adUnitId)
    {
        if (IsMRecLoaded(adUnitId))
        {
            MaxSdk.ShowMRec(adUnitId);
        }
        else
        {
            Debug.LogWarning($"MRec with ID {adUnitId} is not loaded.");
        }
    }
    
    public void HideMRec(string adUnitId)
    {
        MaxSdk.HideMRec(adUnitId);
    }
    
    public void DestroyMRec(string adUnitId)
    {
        MaxSdk.DestroyMRec(adUnitId);
        _mrecStates.Remove(adUnitId);
    }
    
    public bool IsMRecLoaded(string adUnitId)
    {
        return _mrecStates.ContainsKey(adUnitId) && _mrecStates[adUnitId];
    }
    
    #endregion
    
    #region App Open Ads
    
    public void LoadAppOpen(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AppLovin SDK not initialized. Call Initialize() first.");
            return;
        }
        
        MaxSdk.LoadAppOpenAd(adUnitId);
        _appOpenStates[adUnitId] = false; // Will be set to true when loaded
    }
    
    public void ShowAppOpen(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsAppOpenLoaded(adUnitId))
        {
            _currentAppOpenSuccessCallback = onSuccess;
            _currentAppOpenFailCallback = onFail;
            MaxSdk.ShowAppOpenAd(adUnitId);
        }
        else
        {
            Debug.LogWarning($"App Open ad with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsAppOpenLoaded(string adUnitId)
    {
        return _appOpenStates.ContainsKey(adUnitId) && 
               _appOpenStates[adUnitId] && 
               MaxSdk.IsAppOpenAdReady(adUnitId);
    }
    
    #endregion
    
    #region Helper Methods
    
    private MaxSdkBase.AdViewPosition ConvertBannerPositionToAdView(BannerPosition position)
    {
        switch (position)
        {
            case BannerPosition.Top:
                return MaxSdkBase.AdViewPosition.TopCenter;
            case BannerPosition.Bottom:
                return MaxSdkBase.AdViewPosition.BottomCenter;
            case BannerPosition.TopLeft:
                return MaxSdkBase.AdViewPosition.TopLeft;
            case BannerPosition.TopRight:
                return MaxSdkBase.AdViewPosition.TopRight;
            case BannerPosition.BottomLeft:
                return MaxSdkBase.AdViewPosition.BottomLeft;
            case BannerPosition.BottomRight:
                return MaxSdkBase.AdViewPosition.BottomRight;
            case BannerPosition.Center:
                return MaxSdkBase.AdViewPosition.Centered;
            default:
                return MaxSdkBase.AdViewPosition.BottomCenter;
        }
    }
    
    private MaxSdkBase.AdViewPosition ConvertMRecPosition(BannerPosition position)
    {
        switch (position)
        {
            case BannerPosition.Top:
                return MaxSdkBase.AdViewPosition.TopCenter;
            case BannerPosition.Bottom:
                return MaxSdkBase.AdViewPosition.BottomCenter;
            case BannerPosition.TopLeft:
                return MaxSdkBase.AdViewPosition.TopLeft;
            case BannerPosition.TopRight:
                return MaxSdkBase.AdViewPosition.TopRight;
            case BannerPosition.BottomLeft:
                return MaxSdkBase.AdViewPosition.BottomLeft;
            case BannerPosition.BottomRight:
                return MaxSdkBase.AdViewPosition.BottomRight;
            case BannerPosition.Center:
                return MaxSdkBase.AdViewPosition.Centered;
            default:
                return MaxSdkBase.AdViewPosition.Centered;
        }
    }
    
    #endregion
    
    #region Callback Handlers
    
    // Banner callbacks
    private void OnBannerAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _bannerStates[adUnitId] = true;
        OnAdLoaded?.Invoke(adUnitId);
    }
    
    private void OnBannerAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        _bannerStates[adUnitId] = false;
        OnAdFailedToLoad?.Invoke(adUnitId, errorInfo.Message);
    }
    
    private void OnBannerAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdClicked?.Invoke(adUnitId);
    }
    
    private void OnBannerAdRevenuePaid(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdRevenuePaid?.Invoke(adUnitId, adInfo.Revenue);
    }
    
    // Interstitial callbacks
    private void OnInterstitialAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _interstitialStates[adUnitId] = true;
        OnAdLoaded?.Invoke(adUnitId);
    }
    
    private void OnInterstitialAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        _interstitialStates[adUnitId] = false;
        OnAdFailedToLoad?.Invoke(adUnitId, errorInfo.Message);
    }
    
    private void OnInterstitialAdShown(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdShown?.Invoke(adUnitId);
    }
    
    private void OnInterstitialAdClosed(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _interstitialStates[adUnitId] = false;
        OnAdClosed?.Invoke(adUnitId);
        _currentInterstitialSuccessCallback?.Invoke();
        _currentInterstitialSuccessCallback = null;
        _currentInterstitialFailCallback = null;
    }
    
    private void OnInterstitialAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdClicked?.Invoke(adUnitId);
    }
    
    private void OnInterstitialAdDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        _interstitialStates[adUnitId] = false;
        _currentInterstitialFailCallback?.Invoke();
        _currentInterstitialSuccessCallback = null;
        _currentInterstitialFailCallback = null;
    }
    
    private void OnInterstitialAdRevenuePaid(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdRevenuePaid?.Invoke(adUnitId, adInfo.Revenue);
    }
    
    // Rewarded callbacks
    private void OnRewardedAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _rewardedStates[adUnitId] = true;
        OnAdLoaded?.Invoke(adUnitId);
    }
    
    private void OnRewardedAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        _rewardedStates[adUnitId] = false;
        OnAdFailedToLoad?.Invoke(adUnitId, errorInfo.Message);
    }
    
    private void OnRewardedAdShown(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdShown?.Invoke(adUnitId);
    }
    
    private void OnRewardedAdClosed(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _rewardedStates[adUnitId] = false;
        OnAdClosed?.Invoke(adUnitId);
    }
    
    private void OnRewardedAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdClicked?.Invoke(adUnitId);
    }
    
    private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        _rewardedStates[adUnitId] = false;
    }
    
    private void OnRewardedAdReward(string adUnitId, MaxSdk.Reward reward, MaxSdk.AdInfo adInfo)
    {
        OnRewardedAdRewarded?.Invoke(adUnitId, reward.Amount);
        _currentRewardedCallback?.Invoke(reward.Amount);
        _currentRewardedCallback = null;
    }
    
    private void OnRewardedAdRevenuePaid(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdRevenuePaid?.Invoke(adUnitId, adInfo.Revenue);
    }
    
    // MRec callbacks
    private void OnMRecAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _mrecStates[adUnitId] = true;
        OnAdLoaded?.Invoke(adUnitId);
    }
    
    private void OnMRecAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        _mrecStates[adUnitId] = false;
        OnAdFailedToLoad?.Invoke(adUnitId, errorInfo.Message);
    }
    
    private void OnMRecAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdClicked?.Invoke(adUnitId);
    }
    
    private void OnMRecAdRevenuePaid(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdRevenuePaid?.Invoke(adUnitId, adInfo.Revenue);
    }
    
    // App Open callbacks
    private void OnAppOpenAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _appOpenStates[adUnitId] = true;
        OnAdLoaded?.Invoke(adUnitId);
    }
    
    private void OnAppOpenAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        _appOpenStates[adUnitId] = false;
        OnAdFailedToLoad?.Invoke(adUnitId, errorInfo.Message);
    }
    
    private void OnAppOpenAdShown(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdShown?.Invoke(adUnitId);
    }
    
    private void OnAppOpenAdClosed(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        _appOpenStates[adUnitId] = false;
        OnAdClosed?.Invoke(adUnitId);
        _currentAppOpenSuccessCallback?.Invoke();
        _currentAppOpenSuccessCallback = null;
        _currentAppOpenFailCallback = null;
    }
    
    private void OnAppOpenAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdClicked?.Invoke(adUnitId);
    }
    
    private void OnAppOpenAdDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        _appOpenStates[adUnitId] = false;
        _currentAppOpenFailCallback?.Invoke();
        _currentAppOpenSuccessCallback = null;
        _currentAppOpenFailCallback = null;
    }
    
    private void OnAppOpenAdRevenuePaid(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        OnAdRevenuePaid?.Invoke(adUnitId, adInfo.Revenue);
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unregister all callbacks to prevent memory leaks
        if (_isInitialized)
        {
            // Banner callbacks
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClicked;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaid;
            
            // Interstitial callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialAdLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialAdShown;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialAdClosed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialAdClicked;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialAdDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialAdRevenuePaid;
            
            // Rewarded callbacks
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdShown;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdClosed;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClicked;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReward;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedAdRevenuePaid;
            
            // MRec callbacks
            MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMRecAdLoaded;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMRecAdLoadFailed;
            MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMRecAdClicked;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecAdRevenuePaid;
            
            // App Open callbacks
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenAdLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenAdLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenAdShown;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenAdClosed;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAppOpenAdClicked;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenAdDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenAdRevenuePaid;
        }
    }
}
#endif