using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
public class ArcadiaIAP : MonoBehaviour
{
    public IAPType iAPType;
    public Text priceTxt;
    
    void OnEnable()
    {
        GetComponent<CodelessIAPButton>().onProductFetched.AddListener(UpdatePriceText);
        GetComponent<CodelessIAPButton>().onPurchaseComplete.AddListener(OnPurchase);
    }
    void OnDisable()
    {
        GetComponent<CodelessIAPButton>().onProductFetched.RemoveListener(UpdatePriceText);
        GetComponent<CodelessIAPButton>().onPurchaseComplete.RemoveListener(OnPurchase);
    }
    public void OnPurchase(Product product)
    {
        switch (iAPType)
        {
            case IAPType.removeAds:
                ArcadiaSdkManager.Agent.OnRemoveAds();
            break;
        }
    }
    public void UpdatePriceText(Product product)
    {
        priceTxt.text=product.metadata.isoCurrencyCode+product.metadata.localizedPriceString;
    }
    public enum IAPType{other, removeAds}
}
