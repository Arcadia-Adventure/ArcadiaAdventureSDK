using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdBreakTimer : MonoBehaviour
{
    public GameObject alertGameObject;
    public TMP_Text alertText; // Assign a UI Text element in the Inspector
    private float timeCounter = 0f;
    public float alertDelay = 3f;
    public float adInterval = 60f;
    public bool adBreakTriggered = false;
    public bool hasAd = false;

    private void Start()
    {
        alertGameObject.gameObject.SetActive(false); // Hide alert text initially
    }

    private void Update()
    {
        // Increment the timer
        timeCounter += Time.deltaTime;

        // Check if 60 seconds have passed and ad break is not already triggered
        if (timeCounter >= adInterval && !adBreakTriggered)
        {
            adBreakTriggered = true;
            StartCoroutine(PrepareAdBreakAlert());
        }
    }

    private IEnumerator PrepareAdBreakAlert()
    {
        // Check if an ad is ready
        if (!InterstitialAdController.agent.interstitialAd.CanShowAd())
        {
            // Request an ad if not ready
            yield return StartCoroutine(RequestAndWaitForAd());
        }
        
        // Proceed with the alert if an ad is available
        else
        {
            yield return StartCoroutine(ShowAdBreakAlert());
        }
        // else
        // {
        //     // Reset if ad was not available after waiting
        //     ResetTimer();
        // }
    }

    private IEnumerator RequestAndWaitForAd()
    {
        // Reset ad status and request ad
        hasAd = false;
        bool adRequestCompleted = false;

        InterstitialAdController.agent.RequestAndLoadInterstitialAd((bool value) =>
        {
            hasAd = value;
            adRequestCompleted = true;
        },ArcadiaSdkManager.Agent.useTestIDs);

        // Wait until ad request completes
        while (!adRequestCompleted)
        {
            yield return null;
        }
    }

    private IEnumerator ShowAdBreakAlert()
    {
        alertGameObject.gameObject.SetActive(true);
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
        alertGameObject.gameObject.SetActive(false);
        ResetTimer();
    }

    private void ShowAd()
    {
        ArcadiaSdkManager.Agent.ShowInterstitialAd();
        Debug.Log("Ad is being displayed.");
    }

    private void ResetTimer()
    {
        print("Reset timer");
        timeCounter = 0f;
        adBreakTriggered = false;
        hasAd = false;
    }
}
