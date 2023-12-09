using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#if gameanalytics_enabled
using GameAnalyticsSDK;
#endif

#if UNITY_EDITOR
using GoogleMobileAds.Editor;
using GoogleMobileAds;
#endif


public class ArcadiaSdkManager : MonoBehaviour 
{
	//============================== Variables_Region ============================== 
	#region Variables_Region
	private static ArcadiaSdkManager _instance = null;
	public enum RewardedPlacementName
	{
		DoubleCoin =0, ExtraCoin=1, Claim=2, UnlockItem=3, UnlockLevel=4, SkipLevel=5, ReviveHealth=6, ReviveTime=7, BonusLevel=8, Retry=9
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
    [Header("-------- v2023.12.10 --------")]
    public bool useTestIDs;

    public TagForChildDirectedTreatment tagForChild;
    [Header("Banner")] 
    public bool showBannerInStart = false;
    public AdPosition bannerAdPosition;
    public AdSize bannerAdSize;

    private TextAsset GameIdsJsonFile;
	private static GameIDs gameids= new GameIDs();
	[HideInInspector]
	public bool removeAd = false;
	[Space(10)] 
	[SerializeField]
	private bool InternetRequired = true;
	public bool useBannerBackgroundColor = false;
	[Space(20)]
	public static IDs myGameIds = new IDs();
	[Space(20)]
	[SerializeField]
	private IDs Ids = myGameIds;
	[Space(10)]
	[Header("-------- Enable/Disable Logs --------")]
	public bool enableLogs = false;
    private Action interstitialCallBack, rewardedCallBack;
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
		if(_instance == null)
		{	
			_instance = this.gameObject.GetComponent<ArcadiaSdkManager>();
			DontDestroyOnLoad(this);
		}
		else
		{
			if(this != _instance)
				Destroy(this.gameObject);
		}
	}
	#endregion
	
	//================================ Start_Region ================================
	#region Start_Region

	void Start()
	{
		LoadGameIds();
		InitAdmob();
		InternetCheckerInit();
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
			new RequestConfiguration.Builder()
			.SetTagForChildDirectedTreatment(tagForChild)
			.SetTestDeviceIds(deviceIds).build();
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
            SceneManager.LoadScene(1);
		});

		// Listen to application foreground / background events.
		AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
		//LoadAds();
	}
    private AdRequest CreateAdRequest()
    {
        return new AdRequest.Builder().Build();
    }
    public void OnAppStateChanged(AppState state)
    {
        // Display the app open ad when the app is foregrounded.
        UnityEngine.Debug.Log("App State is " + state);
        if (removeAd)
        {
            return;
        }
        if (myGameIds.interstitialAdId.Length < 1) return;
        // OnAppStateChanged is not guaranteed to execute on the Unity UI thread.
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (state == AppState.Foreground && myGameIds.interstitialAdId.Length > 1)
            {
                ShowAppOpenAd();
            }
        });
    }
	public void LoadAds()
	{
		RequestAndLoadRewardedAd();
		if (!removeAd||myGameIds.interstitialAdId.Length>1)
		{
			RequestAndLoadInterstitialAd();
		}
		if (!removeAd|| myGameIds.appOpenAdId.Length > 1)
		{
			RequestAndLoadAppOpenAd();

		}
		if (!removeAd|| myGameIds.bannerAdId.Length > 1)
		{
			if(showBannerInStart)
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
            // Clean up banner before reusing
            if (bannerView != null)
            {
                bannerView.Destroy();
            }

            // Create a 320x50 banner at top of the screen
            bannerView = new BannerView(adUnitId, AdSize.Banner, bannerAdPosition);

            // Add Event Handlers
            bannerView.OnBannerAdLoaded += () =>
            {
                PrintStatus("Banner ad loaded.");
            };
            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                PrintStatus("Banner ad failed to load with error: " + error.GetMessage());
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
                string msg = string.Format("{0} (currency: {1}, value: {2}",
                                            "Banner ad received a paid event.",
                                            adValue.CurrencyCode,
                                            adValue.Value);
                PrintStatus(msg);
            };

            // Load a banner ad
            bannerView.LoadAd(CreateAdRequest());
        }
    }
    public void ShowBanner()
    {
        if (removeAd)
        {
            return;
        }
        RequestBannerAd();
    }
    public void HideBanner()
    {
	    if (bannerView != null)
	    {
		    bannerView.Hide();
	    }
    }
    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
    }

    #endregion

    #region INTERSTITIAL ADS

    public void RequestAndLoadInterstitialAd()
    {
        PrintStatus("Requesting Interstitial ad.");

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
                    PrintStatus("Interstitial ad failed to load with error: " +
                        loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    PrintStatus("Interstitial ad failed to load.");
                    return;
                }

                PrintStatus("Interstitial ad loaded.");
                interstitialAd = ad;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    interstitialCallBack?.Invoke();
                    PrintStatus("Interstitial ad opening.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    PrintStatus("Interstitial ad closed.");
                    RequestAndLoadInterstitialAd();
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    PrintStatus("Interstitial ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    PrintStatus("Interstitial ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    interstitialCallBack?.Invoke();
                    PrintStatus("Interstitial ad failed to show with error: " +
                                error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "Interstitial ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    PrintStatus(msg);
                };
            });
    }

    public void ShowInterstitialAd(Action _interstitialCallBack = null)
    {

        if (removeAd)
        {
            return;
        }

#if gameanalytics_enabled
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "undefine", "undefine");
#endif
        interstitialCallBack = _interstitialCallBack;
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            interstitialCallBack?.Invoke();
            PrintStatus("Interstitial ad is not ready yet.");
            RequestAndLoadInterstitialAd();
            //System.Threading.Tasks.Task.Delay(1000);
            //interstitialAd.Show();
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

    public void RequestAndLoadRewardedAd()
    {
        PrintStatus("Requesting Rewarded ad.");
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
                    PrintStatus("Rewarded ad failed to load with error: " +
                                loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    PrintStatus("Rewarded ad failed to load.");
                    return;
                }

                PrintStatus("Rewarded ad loaded.");
                rewardedAd = ad;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    PrintStatus("Rewarded ad opening.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    PrintStatus("Rewarded ad closed.");
                    RequestAndLoadRewardedAd();
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    PrintStatus("Rewarded ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    PrintStatus("Rewarded ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    rewardedCallBack?.Invoke();
                    PrintStatus("Rewarded ad failed to show with error: " +
                               error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    rewardedCallBack?.Invoke();
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "Rewarded ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    PrintStatus(msg);
                };
            });
    }
    public void ShowRewardedAd(Action rewardSuccess = null, Action noVideoAvailable = null)
    {
        //if (isRemoveAds) return;
        rewardedCallBack = rewardSuccess;
#if gameanalytics_enabled
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, "undefine", "undefine");
#endif
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            PrintStatus("Reward not null");
            rewardedAd.Show((Reward reward) =>
            {
	            rewardedCallBack?.Invoke();
                //rewardAmount = (float)reward.Amount;
                /*                if (OnrewardDelegate != null)
                                {

                                    OnrewardDelegate((float)reward.Amount);
                                }*/
                PrintStatus("Rewarded ad granted a reward: " + reward.Amount);
            });
            //  RequestAndLoadRewardedAd();
        }
        else
        {
            noVideoAvailable?.Invoke();
            PrintStatus("Rewarded ad is not ready yet.");
            RequestAndLoadRewardedAd();
            //System.Threading.Tasks.Task.Delay(1000);
            //ShowRewardedAd();
        }
    }
    public void OnRewardComplete(Reward reward)
    {
        RequestAndLoadRewardedAd();
        PrintStatus("get reward is " + reward.Amount);
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
        AppOpenAd.Load(adUnitId, ScreenOrientation.Portrait, CreateAdRequest(),
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

    private void PrintStatus(string message)
    {
        Debug.Log(message);
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
			Debug.Log("Set IL2CPP Architecture");
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
		Debug.Log("Set Arm64 Architecture");
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
		if (obj==null)
		{
			var rate = new GameObject();
			obj=rate.AddComponent<StoreReviewManager>();
			obj.RateUs();
		}
		else
		{
			obj.RateUs();
		}
	}
	public void InternetCheckerInit()
	{
#if UNITY_EDITOR
	//	return;
#endif
		if (InternetRequired && !removeAd)
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
