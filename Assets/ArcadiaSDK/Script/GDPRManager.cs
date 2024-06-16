using UnityEngine;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Api;
using System.Collections.Generic;
using System;
public class GDPRManager : MonoBehaviour
{
    public static GDPRManager Agent;
    private void Awake()
    {
        if (Agent == null)
        {
            Agent = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    ConsentForm consentForm;
    void Start()
    {
        var debugSettings = new ConsentDebugSettings
        {
            // Geography appears as in EEA for debug devices.
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>
            {
                "8C1BB48990690DFB4802C3800F9D3661"
            }
        };
        // Create a ConsentRequestParameters object.
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings,
        };
        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }
    public void ResetConsentState()
    {
        ConsentInformation.Reset();
    }
    void OnConsentInfoUpdated(FormError consentError)
    {
        if (consentError != null)
        {
            // Handle the error.
            UnityEngine.Debug.LogError("error!" + consentError);
            return;
        }
        if (ConsentInformation.IsConsentFormAvailable())
        {
            LoadConsentForm();
        }
    }
    private void LoadConsentForm()
    {
        ConsentForm.Load(OnLoadConsentForm);
    }
    private void OnLoadConsentForm(ConsentForm form, FormError error)
    {
        if (error != null)
        {
            // Consent gathering failed.
            UnityEngine.Debug.LogError("error!" + error);
            return;
        }
        consentForm = form;
        // Consent has been gathered.
        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            consentForm.Show(OnShowForm);
        }
    }

    void OnShowForm(FormError error)
    {
        if (error != null)
        {
           Debug.LogError(error);
            return;
        }

   
        LoadConsentForm();
    }
}