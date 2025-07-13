#if UNITY_ADMOB
using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

public class AdMobAdsManager : MonoBehaviour, IAdsManager
{
    private static AdMobAdsManager _instance;
    private bool _isInitialized = false;
    private Dictionary<string, BannerView> _bannerAds = new Dictionary<string, BannerView>();
    private Dictionary<string, InterstitialAd> _interstitialAds = new Dictionary<string, InterstitialAd>();
    private Dictionary<string, RewardedAd> _rewardedAds = new Dictionary<string, RewardedAd>();
    private Dictionary<string, AppOpenAd> _appOpenAds = new Dictionary<string, AppOpenAd>();
    private Dictionary<string, BannerView> _mrecAds = new Dictionary<string, BannerView>();
    
    private Action<int> _currentRewardedCallback;
    private Action _currentInterstitialSuccessCallback;
    private Action _currentInterstitialFailCallback;
    private Action _currentAppOpenSuccessCallback;
    private Action _currentAppOpenFailCallback;
    
    public static AdMobAdsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AdMobAdsManager");
                _instance = go.AddComponent<AdMobAdsManager>();
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
    
    public void Initialize(string appId, bool enableLogs = false)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("AdMob SDK is already initialized.");
            return;
        }
        
        // Configure AdMob settings
        RequestConfiguration requestConfiguration = new RequestConfiguration
        {
            TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
            TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.Unspecified,
            MaxAdContentRating = MaxAdContentRating.G
        };
        
        MobileAds.SetRequestConfiguration(requestConfiguration);
        
        // Initialize AdMob SDK
        MobileAds.Initialize(initStatus =>
        {
            _isInitialized = true;
            Debug.Log("AdMob SDK initialized successfully.");
        });
        
        // Set verbose logging
        MobileAds.SetApplicationMuted(false);
        MobileAds.SetApplicationVolume(1.0f);
        
        Debug.Log("AdMob SDK initialization started...");
    }
    
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }
    
    #endregion
    
    #region Banner Ads
    
    public void LoadBanner(string adUnitId, BannerPosition position = BannerPosition.Bottom)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AdMob SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing banner if it exists
        if (_bannerAds.ContainsKey(adUnitId))
        {
            _bannerAds[adUnitId].Destroy();
            _bannerAds.Remove(adUnitId);
        }
        
        GoogleMobileAds.Api.AdPosition adPosition = ConvertBannerPosition(position);
        BannerView bannerView = new BannerView(adUnitId, AdSize.Banner, adPosition);
        
        // Register for ad events
        bannerView.OnBannerAdLoaded += () => OnAdLoaded?.Invoke(adUnitId);
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) => OnAdFailedToLoad?.Invoke(adUnitId, error.GetMessage());
        bannerView.OnAdFullScreenContentOpened += () => OnAdShown?.Invoke(adUnitId);
        bannerView.OnAdFullScreenContentClosed += () => OnAdClosed?.Invoke(adUnitId);
        bannerView.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
        bannerView.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        
        _bannerAds[adUnitId] = bannerView;
        bannerView.LoadAd(CreateAdRequest());
    }
    
    public void ShowBanner(string adUnitId)
    {
        if (_bannerAds.ContainsKey(adUnitId))
        {
            _bannerAds[adUnitId].Show();
        }
        else
        {
            Debug.LogWarning($"Banner with ID {adUnitId} is not loaded.");
        }
    }
    
    public void HideBanner(string adUnitId)
    {
        if (_bannerAds.ContainsKey(adUnitId))
        {
            _bannerAds[adUnitId].Hide();
        }
    }
    
    public void DestroyBanner(string adUnitId)
    {
        if (_bannerAds.ContainsKey(adUnitId))
        {
            _bannerAds[adUnitId].Destroy();
            _bannerAds.Remove(adUnitId);
        }
    }
    
    public bool IsBannerLoaded(string adUnitId)
    {
        return _bannerAds.ContainsKey(adUnitId);
    }
    
    #endregion
    
    #region Interstitial Ads
    
    public void LoadInterstitial(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AdMob SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing interstitial if it exists
        if (_interstitialAds.ContainsKey(adUnitId))
        {
            _interstitialAds[adUnitId].Destroy();
            _interstitialAds.Remove(adUnitId);
        }
        
        InterstitialAd.Load(adUnitId, CreateAdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Interstitial ad failed to load: {error}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            _interstitialAds[adUnitId] = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register for ad events
            ad.OnAdFullScreenContentOpened += () => OnAdShown?.Invoke(adUnitId);
            ad.OnAdFullScreenContentClosed += () => 
            {
                OnAdClosed?.Invoke(adUnitId);
                _currentInterstitialSuccessCallback?.Invoke();
                _currentInterstitialSuccessCallback = null;
                _currentInterstitialFailCallback = null;
                _interstitialAds.Remove(adUnitId);
            };
            ad.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                _currentInterstitialFailCallback?.Invoke();
                _currentInterstitialSuccessCallback = null;
                _currentInterstitialFailCallback = null;
                _interstitialAds.Remove(adUnitId);
            };
            ad.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            ad.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowInterstitial(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsInterstitialLoaded(adUnitId))
        {
            _currentInterstitialSuccessCallback = onSuccess;
            _currentInterstitialFailCallback = onFail;
            _interstitialAds[adUnitId].Show();
        }
        else
        {
            Debug.LogWarning($"Interstitial with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsInterstitialLoaded(string adUnitId)
    {
        return _interstitialAds.ContainsKey(adUnitId) && _interstitialAds[adUnitId] != null && _interstitialAds[adUnitId].CanShowAd();
    }
    
    #endregion
    
    #region Rewarded Ads
    
    public void LoadRewarded(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AdMob SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing rewarded ad if it exists
        if (_rewardedAds.ContainsKey(adUnitId))
        {
            _rewardedAds[adUnitId].Destroy();
            _rewardedAds.Remove(adUnitId);
        }
        
        RewardedAd.Load(adUnitId, CreateAdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Rewarded ad failed to load: {error}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            _rewardedAds[adUnitId] = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register for ad events
            ad.OnAdFullScreenContentOpened += () => OnAdShown?.Invoke(adUnitId);
            ad.OnAdFullScreenContentClosed += () => 
            {
                OnAdClosed?.Invoke(adUnitId);
                _rewardedAds.Remove(adUnitId);
            };
            ad.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                _rewardedAds.Remove(adUnitId);
            };
            ad.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            ad.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowRewarded(string adUnitId, Action<int> onSuccess = null, Action onFail = null)
    {
        if (IsRewardedLoaded(adUnitId))
        {
            _currentRewardedCallback = onSuccess;
            _rewardedAds[adUnitId].Show((Reward reward) =>
            {
                OnRewardedAdRewarded?.Invoke(adUnitId, (int)reward.Amount);
                _currentRewardedCallback?.Invoke((int)reward.Amount);
                _currentRewardedCallback = null;
            });
        }
        else
        {
            Debug.LogWarning($"Rewarded ad with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsRewardedLoaded(string adUnitId)
    {
        return _rewardedAds.ContainsKey(adUnitId) && _rewardedAds[adUnitId] != null && _rewardedAds[adUnitId].CanShowAd();
    }
    
    #endregion
    
    #region MRec Ads
    
    public void LoadMRec(string adUnitId, BannerPosition position = BannerPosition.Center)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AdMob SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing MRec if it exists
        if (_mrecAds.ContainsKey(adUnitId))
        {
            _mrecAds[adUnitId].Destroy();
            _mrecAds.Remove(adUnitId);
        }
        
        GoogleMobileAds.Api.AdPosition adPosition = ConvertBannerPosition(position);
        BannerView mrecView = new BannerView(adUnitId, AdSize.MediumRectangle, adPosition);
        
        // Register for ad events
        mrecView.OnBannerAdLoaded += () => OnAdLoaded?.Invoke(adUnitId);
        mrecView.OnBannerAdLoadFailed += (LoadAdError error) => OnAdFailedToLoad?.Invoke(adUnitId, error.GetMessage());
        mrecView.OnAdFullScreenContentOpened += () => OnAdShown?.Invoke(adUnitId);
        mrecView.OnAdFullScreenContentClosed += () => OnAdClosed?.Invoke(adUnitId);
        mrecView.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
        mrecView.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        
        _mrecAds[adUnitId] = mrecView;
        mrecView.LoadAd(CreateAdRequest());
    }
    
    public void ShowMRec(string adUnitId)
    {
        if (_mrecAds.ContainsKey(adUnitId))
        {
            _mrecAds[adUnitId].Show();
        }
        else
        {
            Debug.LogWarning($"MRec with ID {adUnitId} is not loaded.");
        }
    }
    
    public void HideMRec(string adUnitId)
    {
        if (_mrecAds.ContainsKey(adUnitId))
        {
            _mrecAds[adUnitId].Hide();
        }
    }
    
    public void DestroyMRec(string adUnitId)
    {
        if (_mrecAds.ContainsKey(adUnitId))
        {
            _mrecAds[adUnitId].Destroy();
            _mrecAds.Remove(adUnitId);
        }
    }
    
    public bool IsMRecLoaded(string adUnitId)
    {
        return _mrecAds.ContainsKey(adUnitId);
    }
    
    #endregion
    
    #region App Open Ads
    
    public void LoadAppOpen(string adUnitId)
    {
        if (!_isInitialized)
        {
            Debug.LogError("AdMob SDK not initialized. Call Initialize() first.");
            return;
        }
        
        // Destroy existing app open ad if it exists
        if (_appOpenAds.ContainsKey(adUnitId))
        {
            _appOpenAds[adUnitId].Destroy();
            _appOpenAds.Remove(adUnitId);
        }
        
        AppOpenAd.Load(adUnitId, CreateAdRequest(), (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"App open ad failed to load: {error}");
                OnAdFailedToLoad?.Invoke(adUnitId, error?.GetMessage() ?? "Unknown error");
                return;
            }
            
            _appOpenAds[adUnitId] = ad;
            OnAdLoaded?.Invoke(adUnitId);
            
            // Register for ad events
            ad.OnAdFullScreenContentOpened += () => OnAdShown?.Invoke(adUnitId);
            ad.OnAdFullScreenContentClosed += () => 
            {
                OnAdClosed?.Invoke(adUnitId);
                _currentAppOpenSuccessCallback?.Invoke();
                _currentAppOpenSuccessCallback = null;
                _currentAppOpenFailCallback = null;
                _appOpenAds.Remove(adUnitId);
            };
            ad.OnAdFullScreenContentFailed += (AdError adError) => 
            {
                _currentAppOpenFailCallback?.Invoke();
                _currentAppOpenSuccessCallback = null;
                _currentAppOpenFailCallback = null;
                _appOpenAds.Remove(adUnitId);
            };
            ad.OnAdClicked += () => OnAdClicked?.Invoke(adUnitId);
            ad.OnAdPaid += (AdValue adValue) => OnAdRevenuePaid?.Invoke(adUnitId, (double)adValue.Value / 1000000.0);
        });
    }
    
    public void ShowAppOpen(string adUnitId, Action onSuccess = null, Action onFail = null)
    {
        if (IsAppOpenLoaded(adUnitId))
        {
            _currentAppOpenSuccessCallback = onSuccess;
            _currentAppOpenFailCallback = onFail;
            _appOpenAds[adUnitId].Show();
        }
        else
        {
            Debug.LogWarning($"App Open ad with ID {adUnitId} is not loaded.");
            onFail?.Invoke();
        }
    }
    
    public bool IsAppOpenLoaded(string adUnitId)
    {
        return _appOpenAds.ContainsKey(adUnitId) && _appOpenAds[adUnitId] != null && _appOpenAds[adUnitId].CanShowAd();
    }
    
    #endregion
    
    #region Helper Methods
    
    private GoogleMobileAds.Api.AdPosition ConvertBannerPosition(BannerPosition position)
    {
        switch (position)
        {
            case BannerPosition.Top:
                return GoogleMobileAds.Api.AdPosition.Top;
            case BannerPosition.Bottom:
                return GoogleMobileAds.Api.AdPosition.Bottom;
            case BannerPosition.TopLeft:
                return GoogleMobileAds.Api.AdPosition.TopLeft;
            case BannerPosition.TopRight:
                return GoogleMobileAds.Api.AdPosition.TopRight;
            case BannerPosition.BottomLeft:
                return GoogleMobileAds.Api.AdPosition.BottomLeft;
            case BannerPosition.BottomRight:
                return GoogleMobileAds.Api.AdPosition.BottomRight;
            case BannerPosition.Center:
                return GoogleMobileAds.Api.AdPosition.Center;
            default:
                return GoogleMobileAds.Api.AdPosition.Bottom;
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up all ads
        foreach (var banner in _bannerAds.Values)
        {
            banner?.Destroy();
        }
        _bannerAds.Clear();
        
        foreach (var interstitial in _interstitialAds.Values)
        {
            interstitial?.Destroy();
        }
        _interstitialAds.Clear();
        
        foreach (var rewarded in _rewardedAds.Values)
        {
            rewarded?.Destroy();
        }
        _rewardedAds.Clear();
        
        foreach (var appOpen in _appOpenAds.Values)
        {
            appOpen?.Destroy();
        }
        _appOpenAds.Clear();
        
        foreach (var mrec in _mrecAds.Values)
        {
            mrec?.Destroy();
        }
        _mrecAds.Clear();
    }
}
#endif