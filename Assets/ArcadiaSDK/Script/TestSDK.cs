using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TestSDK : MonoBehaviour
{
    public Dropdown dropdown;
    public void CallButton()
    {
        switch (dropdown.value)
        {
            case 0:
                ArcadiaSdkManager.Agent.ShowRewardedAd();
                break;
            case 1:
                ArcadiaSdkManager.Agent.ShowInterstitialAd();
                break;
            case 2:
                ArcadiaSdkManager.Agent.ShowAppOpenAd();
                break;
            case 3:
                ArcadiaSdkManager.Agent.ShowBanner();
                break;
            case 4:
                ArcadiaSdkManager.Agent.HideBanner(true);
                break;
            case 5:
                ArcadiaSdkManager.Agent.DestroyBannerAd();
                break;
            case 6:
                AA_AnalyticsManager.Agent.GameStartAnalytics(1);
                break;
            case 7:
                AA_AnalyticsManager.Agent.GameCompleteAnalytics(1);
                break;
            case 8:
                AA_AnalyticsManager.Agent.GameFailAnalytics(1);
                break;
            case 9:
                ArcadiaSdkManager.Agent.ShowRateUs();
                break;
            case 10:
                ArcadiaSdkManager.Agent.ShowAvailbleUpdate();
                break;
                
        }
    }
}
