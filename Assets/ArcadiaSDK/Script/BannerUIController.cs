using System.Reflection;
using GoogleMobileAds.Api;
using UnityEngine;

public class BannerUIController : MonoBehaviour
{
    public RectTransform uiPanel;
    public Vector2 initialSize;
    public void Awake()
    {
        uiPanel=GetComponent<RectTransform>();
        initialSize = uiPanel.sizeDelta;
        if(ArcadiaSdkManager.Agent.bannerAdPosition==AdPosition.Top)
        uiPanel.pivot=new Vector2(0.5f,0);
        else if(ArcadiaSdkManager.Agent.bannerAdPosition==AdPosition.Bottom)
        uiPanel.pivot=new Vector2(0.5f,1);
        else
        uiPanel.pivot=new Vector2(0.5f,0.5f);
    }
    private void OnEnable()
    {
        ArcadiaSdkManager.Agent.OnBannerActive += AdjustUIForBanner;
    }

    private void OnDisable()
    {
        ArcadiaSdkManager.Agent.OnBannerActive -= AdjustUIForBanner;
    }
    public void AdjustUIForBanner(bool active)
    {
        if(active)
        {
            uiPanel.sizeDelta = new Vector2(initialSize.x, initialSize.y - ArcadiaSdkManager.Agent.bannerView.GetHeightInPixels());
        }
        else
        uiPanel.sizeDelta = initialSize;
    }
}
