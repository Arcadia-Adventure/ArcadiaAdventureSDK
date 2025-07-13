using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdBreakController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject alertGameObject;
    public TMP_Text alertText;
    
    [Header("Timer Settings")]
    public float alertDelay = 3f;
    public float adInterval = 60f;
    
    [Header("Ad Settings")]
    public string interstitialAdUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";
    public bool useTestAds = false;
    
    private float timeCounter = 0f;
    private bool adBreakTriggered = false;
    public bool hasAd = false;
    private IAdsManager adsManager;
    
    private void Start()
    {
        InitializeAdsManager();
        alertGameObject.SetActive(false);
    }
    
    private void InitializeAdsManager()
    {
        // Try to find an active ads manager
        adsManager = FindAdsManager();
        
        if (adsManager == null)
        {
            Debug.LogError("No ads manager found! Please ensure either AppLovinAdsManager or AdMobAdsManager is active.");
            return;
        }
        
        // Subscribe to ad events
        adsManager.OnAdLoaded += OnAdLoaded;
        adsManager.OnAdFailedToLoad += OnAdFailedToLoad;
        adsManager.OnAdShown += OnAdShown;
        adsManager.OnAdClosed += OnAdClosed;
        
        // Load the initial ad
        LoadInterstitialAd();
    }
    
    private IAdsManager FindAdsManager()
    {
    #if UNITY_APPLOVIN
        // Try AppLovin first
        if (AppLovinAdsManager.Instance != null)
        {
            return AppLovinAdsManager.Instance;
        }
    #endif
    
    #if UNITY_ADMOB
        // Try AdMob
        if (AdMobAdsManager.Instance != null)
        {
            return AdMobAdsManager.Instance;
        }
    #endif
    
        Debug.LogError("No ad SDK is available. Please import AdMob or AppLovin SDK.");
        return null;
    }
    
    private void Update()
    {
        if (adsManager == null || !adsManager.IsInitialized)
            return;
            
        // Increment the timer
        timeCounter += Time.deltaTime;
        
        // Check if interval has passed and ad break is not already triggered
        if (timeCounter >= adInterval && !adBreakTriggered)
        {
            adBreakTriggered = true;
            StartCoroutine(PrepareAdBreakAlert());
        }
    }
    
    private IEnumerator PrepareAdBreakAlert()
    {
        // Check if an ad is ready
        if (!adsManager.IsInterstitialLoaded(interstitialAdUnitId))
        {
            // Request an ad if not ready
            yield return StartCoroutine(RequestAndWaitForAd());
        }
        
        // Proceed with the alert if an ad is available
        if (adsManager.IsInterstitialLoaded(interstitialAdUnitId))
        {
            yield return StartCoroutine(ShowAdBreakAlert());
        }
        else
        {
            // Reset if ad was not available after waiting
            Debug.Log("Ad not available after waiting, resetting timer");
            ResetTimer();
        }
    }
    
    private IEnumerator RequestAndWaitForAd()
    {
        hasAd = false;
        bool adRequestCompleted = false;
        
        // Load the ad
        LoadInterstitialAd();
        
        // Wait for ad to load (with timeout)
        float timeout = 10f; // 10 seconds timeout
        float timer = 0f;
        
        while (!adRequestCompleted && timer < timeout)
        {
            if (adsManager.IsInterstitialLoaded(interstitialAdUnitId))
            {
                hasAd = true;
                adRequestCompleted = true;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!adRequestCompleted)
        {
            Debug.Log("Ad request timed out");
        }
    }
    
    private IEnumerator ShowAdBreakAlert()
    {
        alertGameObject.SetActive(true);
        float countdown = alertDelay;
        
        // Show countdown
        while (countdown > 0)
        {
            alertText.text = "Ad Break in " + Mathf.Ceil(countdown) + " seconds...";
            yield return new WaitForSeconds(1f);
            countdown--;
        }
        
        // Display "Ad Break" alert and call ad function
        alertText.text = "Ad Break";
        ShowAd();
        
        // Wait a moment, hide alert, and reset
        yield return new WaitForSeconds(2f);
        alertGameObject.SetActive(false);
        ResetTimer();
    }
    
    private void ShowAd()
    {
        if (adsManager != null && adsManager.IsInterstitialLoaded(interstitialAdUnitId))
        {
            adsManager.ShowInterstitial(interstitialAdUnitId,
                onSuccess: () => Debug.Log("Interstitial ad shown successfully"),
                onFail: () => Debug.Log("Failed to show interstitial ad"));
        }
        else
        {
            Debug.LogWarning("Cannot show ad - not loaded or ads manager not available");
            ResetTimer();
        }
    }
    
    private void LoadInterstitialAd()
    {
        if (adsManager != null)
        {
            adsManager.LoadInterstitial(interstitialAdUnitId);
        }
    }
    
    private void ResetTimer()
    {
        Debug.Log("Reset timer");
        timeCounter = 0f;
        adBreakTriggered = false;
        hasAd = false;
    }
    
    #region Ad Event Handlers
    
    private void OnAdLoaded(string adUnitId)
    {
        if (adUnitId == interstitialAdUnitId)
        {
            hasAd = true;
            Debug.Log("Interstitial ad loaded successfully");
        }
    }
    
    private void OnAdFailedToLoad(string adUnitId, string error)
    {
        if (adUnitId == interstitialAdUnitId)
        {
            hasAd = false;
            Debug.LogError($"Interstitial ad failed to load: {error}");
        }
    }
    
    private void OnAdShown(string adUnitId)
    {
        if (adUnitId == interstitialAdUnitId)
        {
            Debug.Log("Interstitial ad shown");
        }
    }
    
    private void OnAdClosed(string adUnitId)
    {
        if (adUnitId == interstitialAdUnitId)
        {
            Debug.Log("Interstitial ad closed");
            // Load next ad for future use
            LoadInterstitialAd();
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (adsManager != null)
        {
            adsManager.OnAdLoaded -= OnAdLoaded;
            adsManager.OnAdFailedToLoad -= OnAdFailedToLoad;
            adsManager.OnAdShown -= OnAdShown;
            adsManager.OnAdClosed -= OnAdClosed;
        }
    }
    
    #region Public Methods for Manual Control
    
    public void TriggerAdBreak()
    {
        if (!adBreakTriggered)
        {
            adBreakTriggered = true;
            StartCoroutine(PrepareAdBreakAlert());
        }
    }
    
    public void PauseTimer()
    {
        enabled = false;
    }
    
    public void ResumeTimer()
    {
        enabled = true;
    }
    
    public void SetAdInterval(float newInterval)
    {
        adInterval = newInterval;
    }
    
    public void SetAlertDelay(float newDelay)
    {
        alertDelay = newDelay;
    }
    
    public float GetTimeUntilNextAd()
    {
        return Mathf.Max(0, adInterval - timeCounter);
    }
    
    public bool IsAdReady()
    {
        return adsManager != null && adsManager.IsInterstitialLoaded(interstitialAdUnitId);
    }
    
    #endregion
}