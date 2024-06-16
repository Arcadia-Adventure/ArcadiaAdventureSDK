using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
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
    public TagForChildDirectedTreatment tagForChildDirectedTreatment;
    public TagForUnderAgeOfConsent tagForUnderAgeOfConsent;
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
    private Action<int> rewardedCallBack;
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
            new RequestConfiguration();
            requestConfiguration.TagForChildDirectedTreatment=tagForChildDirectedTreatment;
            requestConfiguration.TagForUnderAgeOfConsent= tagForUnderAgeOfConsent;
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
        BannerAdController.Agent.DestroyBannerAd();
    }
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    public void LoadAds()
    {
        RewardedAdController.agent.RequestAndLoadRewardedAd();
        if (!removeAds && myGameIds.interstitialAdId.Length > 1)
        {
            InterstitialAdController.agent.RequestAndLoadInterstitialAd(null,useTestIDs);
        }
        if (!removeAds && myGameIds.appOpenAdId.Length > 1)
        {
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }
        if (!removeAds && myGameIds.bannerAdId.Length > 1)
        {
            if (showBannerInStart)
                BannerAdController.Agent.ShowBanner(bannerType,bannerAdPosition,useTestIDs);
        }
    }
    void OnAppStateChanged(AppState state)
    {
        // Display the app open ad when the app is foregrounded.
        ArcadiaSdkManager.PrintStatus("App State is " + state);
        if (removeAds)
        {
            return;
        }
        // OnAppStateChanged is not guaranteed to execute on the Unity UI thread.
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (state == AppState.Foreground && ArcadiaSdkManager.myGameIds.appOpenAdId.Length > 1)
            {
                AppOpenAdController.agent.ShowAppOpenAd();
            }
        });
    }
    public void ShowBanner()
    {
        if(removeAds)return;
        BannerAdController.Agent.ShowBanner(bannerType,bannerAdPosition,useTestIDs);
    }
    public void HideBanner()
    {
        BannerAdController.Agent.HideBanner();
    }
    public void DestroyBannerAd()
    {
        BannerAdController.Agent.DestroyBannerAd();
    }
    public void ShowInterstitialAd(Action successCallBack = null, Action failCallBack=null)
    {
        if (removeAds)
        {
            return;
        }
        ShowLoadingScreen(true);
        successCallBack+=()=>ShowLoadingScreen(false);
        failCallBack+=()=>ShowLoadingScreen(false);
        InterstitialAdController.agent.ShowInterstitialAd(successCallBack,failCallBack,useTestIDs);
    }
    public void ShowRewardedAd(Action<int> successCallBack = null, Action failCallBack=null)
    {
        ShowLoadingScreen(true);
        successCallBack+=(int reward)=>ShowLoadingScreen(false);
        failCallBack+=()=>ShowLoadingScreen(false);
        RewardedAdController.agent.ShowRewardedAd(successCallBack,failCallBack,useTestIDs);
    }
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
        yield return new WaitForSecondsRealtime(5);
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
    public static void PrintStatus(string message)
    {
        if(ArcadiaSdkManager.Agent.enableLogs)
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
        string formattedDate = currentDate.ToString("yy.M.d");
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
        Debug.LogError("Convert Platform IOS or Android");
        return "unknown";
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