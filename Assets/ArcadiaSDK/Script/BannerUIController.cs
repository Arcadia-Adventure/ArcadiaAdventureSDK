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
        //return;
        if(ArcadiaSdkManager.Agent.bannerAdPosition==AdPosition.Top)
        uiPanel.pivot=new Vector2(0.5f,0);
        else if(ArcadiaSdkManager.Agent.bannerAdPosition==AdPosition.Bottom)
        uiPanel.pivot=new Vector2(0.5f,1);
        else
        uiPanel.pivot=new Vector2(0.5f,0.5f);
    }
    private void OnEnable()
    {
        AdjustUIForBanner(BannerAdController.Agent.isShowing);
        BannerAdController.Agent.OnShow += AdjustUIForBanner;
    }

    private void OnDisable()
    {
        BannerAdController.Agent.OnShow -= AdjustUIForBanner;
    }
    public void AdjustUIForBanner(bool active)
    {
        if(active)
        {
            // uiPanel.offsetMax=Vector2.up*-ArcadiaSdkManager.Agent.bannerView.GetHeightInPixels();
            // uiPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,initialSize.y - ArcadiaSdkManager.Agent.bannerView.GetHeightInPixels());
            if(uiPanel.sizeDelta==initialSize&&BannerAdController.Agent.bannerView!=null)
            uiPanel.sizeDelta = new Vector2(initialSize.x, initialSize.y - BannerAdController.Agent.bannerView.GetHeightInPixels());
        }
        else
        uiPanel.sizeDelta = initialSize;
    }
}
