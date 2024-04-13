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
public class ArcadiaSdkManager : MonoBehaviour
{
    //============================== Variables_Region ============================== 
    #region Variables_Region
    private static ArcadiaSdkManager _instance = null;
    public enum RewardedPlacementName
    {
        DoubleCoin = 0, ExtraCoin = 1, Claim = 2, UnlockItem = 3, UnlockLevel = 4, SkipLevel = 5, ReviveHealth = 6, ReviveTime = 7, BonusLevel = 8, Retry = 9
    }
    public enum InterstitialPlacementName
    {
        LevelComplete, LevelFail, SelectionScreen, BackButton, HomeButton, PauseButton
    }
    private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromHours(4);
    private DateTime appOpenExpireTime;
    private AppOpenAd appOpenAd;
    public BannerView bannerView;
    public InterstitialAd interstitialAd;
    public RewardedAd rewardedAd;
    [Header("[v2024.1.13]")]
    public bool removeAds = false;
    public bool useTestIDs;
    public bool preCache = true;
    public bool showAvaiableUpdateInStart = true;
    [SerializeField]
    private bool InternetRequired = true;

    [Header("Banner")]
    public bool showBannerInStart = false;
    public AdPosition bannerAdPosition = AdPosition.Top;
    public BannerType bannerType = BannerType.AdoptiveBanner;
    [Header("Ads Setting")]
    public TagForChildDirectedTreatment tagForChild;
    public GameObject loadingScreen;
    private static GameIDs gameids = new GameIDs();

    [Space(20)]
    public static IDs myGameIds = new IDs();
    [Space(20)]
    [SerializeField]
    private IDs Ids = myGameIds;
    [Space(10)]
    [Header("-------- Enable/Disable Logs --------")]
    public bool enableLogs = false;
    private Action interstitialCallBack;
    private Action<int> rewardedCallBack;
    public Action<bool> OnBannerActive;
    internal bool bannerActive;
    #endregion

    //============================== Singleton_Region ============================== 
    #region Singleton_Region
    static public ArcadiaSdkManager Agent
    {
        get
        {
            return _instance;
        }
    }
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != _instance)
                Destroy(this.gameObject);
        }
    }
    #endregion

    //================================ Start_Region ================================
    #region Start_Region

    void Start()
    {
        removeAds=PlayerPrefs.GetInt(nameof(removeAds),0)==1;
        LoadGameIds();
        InitAdmob();
        InternetCheckerInit();
        if (showAvaiableUpdateInStart)
            ShowAvailbleUpdate();
    }
    public void OnEnable()
    {
        OnBannerActive += (bool status) => bannerActive = status;
    }
    public void InitAdmob()
    {
        MobileAds.SetiOSAppPauseOnBackground(true);

        List<String> deviceIds = new List<String>() { AdRequest.TestDeviceSimulator };

        // Add some test device IDs (replace with your own device IDs).
#if UNITY_IPHONE
        //deviceIds.Add("D8E71788-08AE-4095-ACE6-F35B24D77298");
#elif UNITY_ANDROID
        //deviceIds.Add("75EF8D155528C04DACBBA6F36F433035");
#endif
        // Configure TagForChildDirectedTreatment and test device IDs.
        RequestConfiguration requestConfiguration =
            new RequestConfiguration.Builder().SetTagForChildDirectedTreatment(tagForChild).SetTestDeviceIds(deviceIds).build();
        MobileAds.SetRequestConfiguration(requestConfiguration);
        // Initialize the Google Mobile Ads SDK.
        //MobileAds.Initialize(HandleInitCompleteAction);
        MobileAds.Initialize((initStatus) =>
        {
            Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
            foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
            {
                string className = keyValuePair.Key;
                AdapterStatus status = keyValuePair.Value;
                switch (status.InitializationState)
                {
                    case AdapterState.NotReady:
                        // The adapter initialization did not complete.
                        MonoBehaviour.print("Adapter: " + className + " not ready.");
                        break;
                    case AdapterState.Ready:
                        // The adapter was successfully initialized.
                        MonoBehaviour.print("Adapter: " + className + " is initialized.");
                        break;
                }
            }
            LoadAds();
            LoadNextScene();
        });
        if (InternetRequired)
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork && Application.internetReachability != NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            LoadNextScene();
        }
        Invoke(nameof(LoadNextScene), 3f);
        // Listen to application foreground / background events.
        //LoadAds();
    }
    public void LoadNextScene()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
            SceneManager.LoadScene(1);
    }
    public void OnRemoveAds()
    {
        removeAds=true;
        PlayerPrefs.SetInt(nameof(removeAds),1);
        DestroyBannerAd();
    }
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }
    public void OnAppStateChanged(AppState state)
    {
        // Display the app open ad when the app is foregrounded.
        PrintStatus("App State is " + state);
        if (removeAds)
        {
            return;
        }
        // OnAppStateChanged is not guaranteed to execute on the Unity UI thread.
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (state == AppState.Foreground && myGameIds.appOpenAdId.Length > 1)
            {
                ShowAppOpenAd();
            }
        });
    }
    public void LoadAds()
    {
        RequestAndLoadRewardedAd();
        if (!removeAds && myGameIds.interstitialAdId.Length > 1)
        {
            RequestAndLoadInterstitialAd();
        }
        if (!removeAds && myGameIds.appOpenAdId.Length > 1)
        {
            RequestAndLoadAppOpenAd();

        }
        if (!removeAds && myGameIds.bannerAdId.Length > 1)
        {
            if (showBannerInStart)
                RequestBannerAd();
        }
    }
    #region BANNER ADS
    public void RequestBannerAd()
    {
        PrintStatus("Requesting Banner ad.");

        // These ad units are configured to always serve test ads.
        string adUnitId = myGameIds.bannerAdId;
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
                bannerView = new BannerView(adUnitId, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), bannerAdPosition);
                break;
            case BannerType.SmartBanner:
                bannerView = new BannerView(adUnitId, AdSize.SmartBanner, bannerAdPosition);
                break;
            case BannerType.Banner:
                bannerView = new BannerView(adUnitId, AdSize.Banner, bannerAdPosition);
                break;

            case BannerType.IABBanner:
                bannerView = new BannerView(adUnitId, AdSize.IABBanner, bannerAdPosition);
                break;

            case BannerType.Leaderboard:
                bannerView = new BannerView(adUnitId, AdSize.Leaderboard, bannerAdPosition);
                break;

            case BannerType.MediumRectangle:
                bannerView = new BannerView(adUnitId, AdSize.MediumRectangle, bannerAdPosition);
                break;
        }
        // Add Event Handlers
        bannerView.OnBannerAdLoaded += () =>
        {
#if gameanalytics_enabled
            if (GameAnalytics.Initialized)
            {
                GameAnalytics.NewAdEvent(GAAdAction.Loaded, GAAdType.Banner, bannerView.GetResponseInfo().GetLoadedAdapterResponseInfo().AdSourceName, "undefine");
                GameAnalyticsILRD.SubscribeAdMobImpressions(adUnitId, bannerView);
            }
#endif
            PrintStatus("Banner ad loaded.");
            OnBannerActive.Invoke(true);
        };
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
#if gameanalytics_enabled
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, error.GetMessage());
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Banner, "undefine", "undefine", GAAdError.InvalidRequest);
#endif
            PrintStatus("Banner ad failed to load with error: " + error.GetMessage());
        };
        bannerView.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner, "undefine", "undefine");
#endif
        };
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            PrintStatus("Banner ad opening.");
        };
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            PrintStatus("Banner ad closed.");
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
            PrintStatus(msg);
        };

        // Load a banner ad
        bannerView.LoadAd(CreateAdRequest());
    }
    public void ShowBanner(bool showNew=false)
    {
        if (removeAds)
        {
            return;
        }
        if(showNew)
        RequestBannerAd();
        else
        HideBanner(false);
    }
    public void HideBanner(bool hide)
    {
        OnBannerActive.Invoke(!hide);
        if (bannerView != null)
        {
            if(hide)
            {
            bannerView.Hide();
            }
            else
            {
            bannerView.Show();
            }
        }
        else if(!hide)
        {
            RequestBannerAd();
        }
    }
    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            OnBannerActive.Invoke(false);
            bannerView.Destroy();
        }
    }

    #endregion

    #region INTERSTITIAL ADS

    public void RequestAndLoadInterstitialAd(Action<bool> OnLoad=null)
    {

        PrintStatus("Requesting Interstitial ad.");
#if gameanalytics_enabled

        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.Interstitial, "undefine", "undefine");
#endif
        string adUnitId = myGameIds.interstitialAdId;
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
        InterstitialAd.Load(adUnitId, CreateAdRequest(),
            (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
#if gameanalytics_enabled
                    GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, loadError.GetMessage());
                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.NoFill);
#endif
                    PrintStatus("Interstitial ad failed to load with error: " +
                        loadError.GetMessage());
                        OnLoad?.Invoke(false);
                    return;
                }
                else if (ad == null)
                {
#if gameanalytics_enabled

                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.InternalError);
#endif
                    PrintStatus("Interstitial ad failed to load.");
                    OnLoad?.Invoke(false);
                    return;
                }
                PrintStatus("Interstitial ad loaded.");
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
            PrintStatus(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "undefine", "undefine");
#endif
            PrintStatus("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Interstitial, "undefine", "undefine");
#endif
            PrintStatus("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            PrintStatus("Interstitial ad full screen content opened.");
        };
    }
    private void RegisterReloadHandler(InterstitialAd interstitialAd)
    {
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            // Reload the ad so that we can show another as soon as possible.
            if(preCache)RequestAndLoadInterstitialAd();
            interstitialCallBack?.Invoke();
            ShowLoadingScreen(false);
            PrintStatus("Interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            // Reload the ad so that we can show another as soon as possible.
            if(preCache)RequestAndLoadInterstitialAd();
#if gameanalytics_enabled
            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.InvalidRequest);
#endif
            Debug.LogError("Interstitial ad failed to open full screen content " + "with error : " + error);
        };
    }

    public void ShowInterstitialAd(Action _interstitialCallBack = null)
    {

        if (removeAds)
        {
            return;
        }
        interstitialCallBack = _interstitialCallBack;
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "undefine", "undefine", GAAdError.UnableToPrecache);
#endif
            PrintStatus("Interstitial ad is not ready yet.");
            if(!preCache)ShowLoadingScreen(true);
            RequestAndLoadInterstitialAd((bool onload)=>
            {
                if(!preCache)
                {
                    interstitialAd.Show();
                }
            });
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
    #region REWARDED ADS

    public void RequestAndLoadRewardedAd(Action<bool> Onload = null)
    {
        PrintStatus("Requesting Rewarded ad.");
#if gameanalytics_enabled
        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
        string adUnitId = myGameIds.rewardedVideoAdId;
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
        RewardedAd.Load(adUnitId, CreateAdRequest(),
            (RewardedAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
#if gameanalytics_enabled
                    GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, loadError.GetMessage());
                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.NoFill);
#endif
                    PrintStatus("Rewarded ad failed to load with error: " + loadError.GetMessage());
                    Onload?.Invoke(false);
                    return;
                }
                else if (ad == null)
                {
#if gameanalytics_enabled

                    if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "undefine", "undefine", GAAdError.InternalError);
#endif
                    PrintStatus("Rewarded ad failed to load.");
                    Onload?.Invoke(false);
                    return;
                }
                PrintStatus("Rewarded ad loaded.");
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
            PrintStatus(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
            PrintStatus("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
#if gameanalytics_enabled

            if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
            PrintStatus("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            PrintStatus("Rewarded ad full screen content opened.");
        };
    }
    private void RegisterReloadHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            ShowLoadingScreen(false);
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
    public void ShowRewardedAd(Action<int> rewardSuccess = null, Action noVideoAvailable = null)
    {
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
            PrintStatus("Rewarded ad is not ready yet.");
            if(!preCache)ShowLoadingScreen(true);
            RequestAndLoadRewardedAd((bool isLoaded) =>
            {
                if (isLoaded)
                {
                    if (!preCache)
                    {
                        rewardedAd.Show(OnRewardComplete);
                    }
                }
            });
        }
    }
    public void OnRewardComplete(Reward reward)
    {
#if gameanalytics_enabled
        if (GameAnalytics.Initialized) GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
        rewardedCallBack?.Invoke((int)reward.Amount);
        PrintStatus("Rewarded ad granted a reward: " + reward.Amount);
    }
    #endregion

    #region APPOPEN ADS

    public bool IsAppOpenAdAvailable
    {
        get
        {
            return (appOpenAd != null
                    && appOpenAd.CanShowAd()
                    && DateTime.Now < appOpenExpireTime);
        }
    }

    public void RequestAndLoadAppOpenAd()
    {
        PrintStatus("Requesting App Open ad.");
        string adUnitId = myGameIds.appOpenAdId;
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
        AppOpenAd.Load(adUnitId, CreateAdRequest(),
            (AppOpenAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    PrintStatus("App open ad failed to load with error: " +
                        loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    PrintStatus("App open ad failed to load.");
                    return;
                }

                PrintStatus("App Open ad loaded. Please background the app and return.");
                this.appOpenAd = ad;
                this.appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    PrintStatus("App open ad opened.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    PrintStatus("App open ad closed.");
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    PrintStatus("App open ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    PrintStatus("App open ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    PrintStatus("App open ad failed to show with error: " +
                        error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "App open ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    PrintStatus(msg);
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

    #endregion

    public void ShowLoadingScreen(bool active)
    {
        if(active)
        {
            if(loadingCoroutine!=null)
            StopCoroutine(loadingCoroutine);
            loadingCoroutine=StartCoroutine(ShowLoadingCoroutine());
        }
        else
        {
            loadingScreen.gameObject.SetActive(false);
        }
    }
    Coroutine loadingCoroutine;
    IEnumerator ShowLoadingCoroutine()
    {
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(5);
        loadingScreen.SetActive(false);

    }
    #region AD INSPECTOR

    public void OpenAdInspector()
    {
        PrintStatus("Opening Ad inspector.");

        MobileAds.OpenAdInspector((error) =>
        {
            if (error != null)
            {
                PrintStatus("Ad inspector failed to open with error: " + error);
            }
            else
            {
                PrintStatus("Ad inspector opened successfully.");
            }
        });
    }

    #endregion

    private static void PrintStatus(string message)
    {
        print(message);
    }
    #endregion

    //============================= SDKs_InIt_Region ============================= 

    public static string GetAdmobAppID()
    {
        string idsfile = Resources.Load<TextAsset>("GameIdsFile").ToString();
        gameids = JsonUtility.FromJson<GameIDs>(idsfile);
        GetIdByName();
        return myGameIds.admobAppId;
    }

    public void LoadGameIds()
    {
        string idsfile = Resources.Load<TextAsset>("GameIdsFile").ToString();
        gameids = JsonUtility.FromJson<GameIDs>(idsfile);
        GetIdByName();
#if UNITY_EDITOR
        PlayerSettings.productName = myGameIds.gameName;
        PlayerSettings.companyName = "Arcadia Adventure";
        PlayerSettings.bundleVersion = GetDatedVersion();
        if (GetPlatformName() == "Android")
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PrintStatus("Set IL2CPP Architecture");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, myGameIds.bundleId);
            SetARM64TargetArchitecture();
        }
        else if (GetPlatformName() == "IOS")
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, myGameIds.bundleId);

        }
        Ids = myGameIds;
#endif
    }
#if UNITY_EDITOR
    public string GetDatedVersion()
    {
        // Get the current date and time
        DateTime currentDate = DateTime.Now;
        // Format the date as "yyyy.mm.dd"
        string formattedDate = currentDate.ToString("yyyy.M.d");
        return formattedDate;
        // Print the formatted date
    }
    static void SetARM64TargetArchitecture()
    {
        // Get the current target architectures for Android
        AndroidArchitecture targetArchitectures = PlayerSettings.Android.targetArchitectures;

        // Set ARM64 as a target architecture
        targetArchitectures |= AndroidArchitecture.ARM64;

        // Apply the changes
        PlayerSettings.Android.targetArchitectures = targetArchitectures;
        PrintStatus("Set Arm64 Architecture");
    }
#endif
    static void GetIdByName()
    {
        IDs[] adids = gameids.id.ToArray();
        myGameIds = Array.Find(adids, id => id.platform == GetPlatformName());
    }

    static string GetPlatformName()
    {
#if UNITY_ANDROID
        return "Android";
#endif
#if UNITY_IOS || UNITY_IPHONE
		return "IOS";
#endif

    }

    public void ShowRateUs()
    {
        StoreReviewManager obj = FindObjectOfType<StoreReviewManager>();
        if (obj == null)
        {
            var rate = new GameObject();
            obj = rate.AddComponent<StoreReviewManager>();
            obj.RateUs();
        }
        else
        {
            obj.RateUs();
        }
    }
    public void ShowAvailbleUpdate()
    {
        UpdateManager obj = FindObjectOfType<UpdateManager>();
        if (obj == null)
        {
            var updateManager = new GameObject();
            obj = updateManager.AddComponent<UpdateManager>();
            obj.ShowAvailbleUpdate();
        }
        else
        {
            obj.ShowAvailbleUpdate();
        }
    }
    public void InternetCheckerInit()
    {
#if UNITY_EDITOR
        //	return;
#endif
        if (InternetRequired && !removeAds)
        {
            InternetManager obj = FindObjectOfType<InternetManager>();
            if (obj == null)
            {
                var net = new GameObject();
                net.name = "InternetManager";
                net.AddComponent<InternetManager>();
                DontDestroyOnLoad(net);
            }

        }
    }
}


[Serializable]
public class GameIDs
{
    public List<IDs> id = new List<IDs>();
}

[Serializable]
public class IDs
{
    public string platform;
    public string gameName;
    public string bundleId;
    public string admobAppId;
    public string appOpenAdId;
    public string bannerAdId;
    public string mrecAdId;
    public string interstitialAdId;
    public string rewardedVideoAdId;
    public string gameKey_GameAnaytics;
    public string secretKey_GameAnaytics;
}
public enum BannerType
{
    AdoptiveBanner,
    SmartBanner,
    Banner,

    MediumRectangle,

    IABBanner,

    Leaderboard,
}