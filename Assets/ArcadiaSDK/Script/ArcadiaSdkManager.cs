using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public enum Audience
    {
        General,
        Mature,
        Child,
        Teen,
        None
    }

    [Header("[v25.7.13]")]
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
    [Header("MRec Banner")]
    public AdPosition mRecBannerAdPosition = AdPosition.BottomRight;
    public BannerType mRecBannerType = BannerType.MediumRectangle;
    [Header("Ads Setting")]
    public Audience audience;
    public GameObject loadingScreen;
    Text loadingText;
    private static GameIDs gameids = new GameIDs();

    [Space(20)]
    public static IDs myGameIds = new IDs();
    [Space(20)]
    [SerializeField]
    private IDs Ids = myGameIds;
    [Space(10)]
    [Header("-------- Enable/Disable Logs --------")]
    public bool enableLogs = false;
    private Action<int> rewardedCallBack;
    
    private IAdsManager adsManager;
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
    
    public void SetLog(bool value)
    {
        PlayerPrefs.SetInt(nameof(enableLogs), value ? 1 : 0);
        enableLogs = value;
    }
    public bool GetLog()
    {
        enableLogs = (PlayerPrefs.GetInt(nameof(enableLogs), enableLogs ? 1 : 0) == 1) ? true : false;
        return enableLogs;
    }
    
    //================================ Start_Region ================================
    #region Start_Region

    void Start()
    {
        removeAds = PlayerPrefs.GetInt(nameof(removeAds), 0) == 1;
        InitializeAdsManager();
        InternetCheckerInit();
        if (loadingText == null) loadingText = GetComponentInChildren<Text>(true);
        if (showAvaiableUpdateInStart) ShowAvailbleUpdate();
    }
    
    private void InitializeAdsManager()
    {
        // Initialize AppStateEventNotifier
        if (AppStateEventNotifier.Instance != null)
        {
            AppStateEventNotifier.Instance.gameObject.SetActive(true);
        }
        
        // Try to find an active ads manager
        adsManager = FindAdsManager();
        
        if (adsManager == null)
        {
            Debug.LogError("No ads manager found! Please ensure either AppLovinAdsManager or AdMobAdsManager is active in the scene.");
            return;
        }
        
        // Initialize the ads manager
        string sdkKey = GetSdkKey();
        adsManager.Initialize(sdkKey, enableLogs);
        
        // Subscribe to events
        adsManager.OnAdLoaded += OnAdLoaded;
        adsManager.OnAdFailedToLoad += OnAdFailedToLoad;
        adsManager.OnAdShown += OnAdShown;
        adsManager.OnAdClosed += OnAdClosed;
        
        // Load ads and proceed
        LoadAds();
        LoadNextScene();
        
        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork && 
            Application.internetReachability != NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            LoadNextScene();
        }
        Invoke(nameof(LoadNextScene), 3f);
    }
    
    private IAdsManager FindAdsManager()
    {
#if UNITY_APPLOVIN
        // Try AppLovin first
        if (AppLovinAdsManager.Instance != null)
        {
            return AppLovinAdsManager.Instance;
        }
        else
        {
            //Create AppLovinAdsManager if not found
            GameObject applovinManagerObj = new ("AppLovinAdsManager");
            AppLovinAdsManager applovinManager = applovinManagerObj.AddComponent<AppLovinAdsManager>();
            DontDestroyOnLoad(applovinManagerObj);
        }
#endif

#if UNITY_ADMOB
        // Try AdMob
        if (AdMobAdsManager.Instance != null)
        {
            return AdMobAdsManager.Instance;
        }
        else
        {
            //Create AdMobAdsManager if not found
            GameObject admobManagerObj = new ("AdMobAdsManager");
            AdMobAdsManager admobManager = admobManagerObj.AddComponent<AdMobAdsManager>();
            DontDestroyOnLoad(admobManagerObj);
        }
#endif

        // If no ads manager is found, return null
        Debug.LogWarning("No ads manager found. Please ensure either AppLovin or AdMob SDK is properly imported and configured.");
        return null;
    }
    
    private string GetSdkKey()
    {
        // Return appropriate SDK key based on which ads manager is being used
#if UNITY_APPLOVIN
        if (adsManager is AppLovinAdsManager)
        {
            return myGameIds.appLovinSdkKey;
        }
#elif UNITY_ADMOB
        if (adsManager is AdMobAdsManager)
        {
            return myGameIds.admobAppId;
        }
#endif
        
        return "";
    }
    
    public void LoadNextScene()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
            SceneManager.LoadScene(1);
    }
    
    public void OnRemoveAds()
    {
        removeAds = true;
        PlayerPrefs.SetInt(nameof(removeAds), 1);
        if (adsManager != null)
        {
            adsManager.DestroyBanner(myGameIds.bannerAdId);
            adsManager.DestroyMRec(myGameIds.mrecAdId);
        }
    }

    public void LoadAds()
    {
        if (adsManager == null || !adsManager.IsInitialized)
        {
            Debug.LogWarning("Ads manager not initialized. Cannot load ads.");
            return;
        }
        
        // Load rewarded ads
        if (myGameIds.rewardedVideoAdId.Length > 1)
        {
            adsManager.LoadRewarded(myGameIds.rewardedVideoAdId);
        }
        
        // Load interstitial ads
        if (!removeAds && myGameIds.interstitialAdId.Length > 1)
        {
            adsManager.LoadInterstitial(myGameIds.interstitialAdId);
        }
        
        // Load app open ads
        if (!removeAds && myGameIds.appOpenAdId.Length > 1)
        {
            adsManager.LoadAppOpen(myGameIds.appOpenAdId);
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }
        
        // Load and show banner ads
        if (!removeAds && myGameIds.bannerAdId.Length > 1)
        {
            adsManager.LoadBanner(myGameIds.bannerAdId, ConvertAdPosition(bannerAdPosition));
            if (showBannerInStart)
            {
                adsManager.ShowBanner(myGameIds.bannerAdId);
            }
        }
        
        // Load MRec ads
        if (!removeAds && myGameIds.mrecAdId.Length > 1)
        {
            adsManager.LoadMRec(myGameIds.mrecAdId, ConvertAdPosition(mRecBannerAdPosition));
        }
    }
    
    private BannerPosition ConvertAdPosition(AdPosition adPosition)
    {
        switch (adPosition)
        {
            case AdPosition.Top:
                return BannerPosition.Top;
            case AdPosition.Bottom:
                return BannerPosition.Bottom;
            case AdPosition.TopLeft:
                return BannerPosition.TopLeft;
            case AdPosition.TopRight:
                return BannerPosition.TopRight;
            case AdPosition.BottomLeft:
                return BannerPosition.BottomLeft;
            case AdPosition.BottomRight:
                return BannerPosition.BottomRight;
            case AdPosition.Center:
                return BannerPosition.Center;
            default:
                return BannerPosition.Bottom;
        }
    }
    
    public void OnAppStateChanged(AppState state)
    {
        ArcadiaSdkManager.PrintStatus("App State is " + state);
        if (removeAds || adsManager == null)
        {
            return;
        }
        
        if (state == AppState.Foreground && ArcadiaSdkManager.myGameIds.appOpenAdId.Length > 1)
        {
            adsManager.ShowAppOpen(myGameIds.appOpenAdId);
        }
    }
    
    // Banner Methods
    public void ShowBanner()
    {
        if (removeAds || adsManager == null) return;
        adsManager.ShowBanner(myGameIds.bannerAdId);
    }
    
    public void HideBanner()
    {
        if (adsManager == null) return;
        adsManager.HideBanner(myGameIds.bannerAdId);
    }
    
    public void DestroyBannerAd()
    {
        if (adsManager == null) return;
        adsManager.DestroyBanner(myGameIds.bannerAdId);
    }
    
    // MRec Methods
    public void ShowMRecBanner()
    {
        if (removeAds || adsManager == null) return;
        adsManager.ShowMRec(myGameIds.mrecAdId);
    }
    
    public void HideMRecBanner()
    {
        if (adsManager == null) return;
        adsManager.HideMRec(myGameIds.mrecAdId);
    }
    
    public void DestroyMRecBannerAd()
    {
        if (adsManager == null) return;
        adsManager.DestroyMRec(myGameIds.mrecAdId);
    }
    
    // Interstitial Methods
    public void ShowInterstitialAd(int timer, Action successCallBack = null, Action failCallBack = null)
    {
        StartCoroutine(ShowAdWithDelay(ShowInterstitialAd, successCallBack, failCallBack, timer));
    }
    
    public void ShowInterstitialAd(Action successCallBack = null, Action failCallBack = null)
    {
        if (removeAds || adsManager == null)
        {
            successCallBack?.Invoke();
            return;
        }
        
        ShowLoadingScreen(true);
        successCallBack += () => ShowLoadingScreen(false);
        failCallBack += () => ShowLoadingScreen(false);
        
        adsManager.ShowInterstitial(myGameIds.interstitialAdId, successCallBack, failCallBack);
    }
    
    // Rewarded Methods
    public void ShowRewardedAd(int timer, Action<int> successCallBack = null, Action failCallBack = null)
    {
        StartCoroutine(ShowAdWithDelay(ShowRewardedAd, successCallBack, failCallBack, timer));
    }
    
    public void ShowRewardedAd(Action<int> successCallBack = null, Action failCallBack = null)
    {
        if (adsManager == null)
        {
            failCallBack?.Invoke();
            return;
        }
        
        ShowLoadingScreen(true);
        successCallBack += (int reward) => ShowLoadingScreen(false);
        failCallBack += () => ShowLoadingScreen(false);
        
        adsManager.ShowRewarded(myGameIds.rewardedVideoAdId, successCallBack, failCallBack);
    }
    
    private IEnumerator ShowAdWithDelay(Action<Action, Action> AD, Action successCallBack = null, Action failCallBack = null, int timer = 0)
    {
        ShowLoadingScreen(true);
        while (timer > 0)
        {
            UpdateLoadingText($"Loading ad... \n{timer}s left.");
            yield return new WaitForSeconds(1);
            timer--;
        }
        AD.Invoke(successCallBack, failCallBack);
    }
    
    private IEnumerator ShowAdWithDelay(Action<Action<int>, Action> AD, Action<int> successCallBack = null, Action failCallBack = null, int timer = 0)
    {
        ShowLoadingScreen(true);
        while (timer > 0)
        {
            UpdateLoadingText($"Loading ad... \n{timer}s left.");
            yield return new WaitForSecondsRealtime(1);
            timer--;
        }
        AD.Invoke(successCallBack, failCallBack);
    }
    
    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }
    
    public void ShowLoadingScreen(bool active)
    {
        if (loadingScreen == null) return;
        
        if (active)
        {
            if (loadingCoroutine != null)
                StopCoroutine(loadingCoroutine);
            loadingCoroutine = StartCoroutine(ShowLoadingCoroutine());
        }
        else
        {
            loadingScreen.gameObject.SetActive(false);
        }
    }
    
    Coroutine loadingCoroutine;
    IEnumerator ShowLoadingCoroutine()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            yield return new WaitForSecondsRealtime(5);
            loadingScreen.SetActive(false);
        }
    }
    
    // Ad Event Handlers
    private void OnAdLoaded(string adUnitId)
    {
        PrintStatus($"Ad loaded: {adUnitId}");
    }
    
    private void OnAdFailedToLoad(string adUnitId, string error)
    {
        PrintStatus($"Ad failed to load: {adUnitId}, Error: {error}");
    }
    
    private void OnAdShown(string adUnitId)
    {
        PrintStatus($"Ad shown: {adUnitId}");
    }
    
    private void OnAdClosed(string adUnitId)
    {
        PrintStatus($"Ad closed: {adUnitId}");
        
        // Reload ads after they're closed
        if (adUnitId == myGameIds.interstitialAdId)
        {
            adsManager.LoadInterstitial(myGameIds.interstitialAdId);
        }
        else if (adUnitId == myGameIds.rewardedVideoAdId)
        {
            adsManager.LoadRewarded(myGameIds.rewardedVideoAdId);
        }
        else if (adUnitId == myGameIds.appOpenAdId)
        {
            adsManager.LoadAppOpen(myGameIds.appOpenAdId);
        }
    }
    
    public static void PrintStatus(string message)
    {
        if (ArcadiaSdkManager.Agent && ArcadiaSdkManager.Agent.enableLogs)
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
    
    public static string GetAppLovinSdkKey()
    {
        string idsfile = Resources.Load<TextAsset>("GameIdsFile").ToString();
        gameids = JsonUtility.FromJson<GameIDs>(idsfile);
        GetIdByName();
        return myGameIds.appLovinSdkKey;
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
    public static string GetDatedVersion()
    {
        DateTime currentDate = DateTime.Now;
        string formattedDate = currentDate.ToString("yy.M.d");
        return formattedDate;
    }
    
    static void SetARM64TargetArchitecture()
    {
        AndroidArchitecture targetArchitectures = PlayerSettings.Android.targetArchitectures;
        targetArchitectures |= AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetArchitectures = targetArchitectures;
        PrintStatus("Set Arm64 Architecture");
    }
#endif
    
    static void GetIdByName()
    {
        IDs[] adids = gameids.id.ToArray();
        myGameIds = Array.Find(adids, id => id.platform == GetPlatformName());
        
        // Fallback if no matching platform found
        if (myGameIds == null)
        {
            myGameIds = new IDs();
            Debug.LogWarning($"No game IDs found for platform: {GetPlatformName()}");
        }
    }

    static string GetPlatformName()
    {
#if UNITY_ANDROID
        return "Android";
#elif UNITY_IOS || UNITY_IPHONE
        return "IOS";
#else
        Debug.LogError("Platform not supported. Please build for Android or iOS.");
        return "unknown";
#endif
    }

    public void ShowRateUs()
    {
        StoreReviewManager obj = FindFirstObjectByType<StoreReviewManager>();
        if (obj == null)
        {
            var rate = new GameObject("StoreReviewManager");
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
        UpdateManager obj = FindFirstObjectByType<UpdateManager>();
        if (obj == null)
        {
            var updateManager = new GameObject("UpdateManager");
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
// #if UNITY_EDITOR
//         // Skip internet check in editor
//         return;
// #endif
        if (InternetRequired && !removeAds)
        {
            InternetManager obj = FindFirstObjectByType<InternetManager>();
            if (obj == null)
            {
                var net = new GameObject("InternetManager");
                net.AddComponent<InternetManager>();
                DontDestroyOnLoad(net);
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (adsManager != null)
        {
            adsManager.OnAdLoaded -= OnAdLoaded;
            adsManager.OnAdFailedToLoad -= OnAdFailedToLoad;
            adsManager.OnAdShown -= OnAdShown;
            adsManager.OnAdClosed -= OnAdClosed;
        }
        
        // Unsubscribe from app state events
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
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
    public string appLovinSdkKey;
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

public enum AdPosition
{
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}