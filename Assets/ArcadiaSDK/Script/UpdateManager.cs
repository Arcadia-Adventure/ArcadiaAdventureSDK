#if UNITY_ANDROID
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif
using System.Collections;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
#if UNITY_ANDROID
    private AppUpdateManager appUpdateManager;
#endif

    public void ShowAvailbleUpdate()
    {
        if (PlayerPrefs.GetInt("ShowAvailableUpdate") == 1)
            return;
#if UNITY_ANDROID
        this.appUpdateManager = new AppUpdateManager();
        StartCoroutine(CheckForUpdate());
#endif
#if UNITY_IOS || UNITY_IPHONE
        //UnityEngine.iOS.Device.RequestStoreReview();        
#endif
        PlayerPrefs.SetInt("ShowAvailableUpdate", 1);
    }
#if UNITY_ANDROID

    IEnumerator CheckForUpdate()
    {
        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation =
          appUpdateManager.GetAppUpdateInfo();

        yield return appUpdateInfoOperation;


        if (appUpdateInfoOperation.Error == AppUpdateErrorCode.ErrorUnknown)
        {
            print("there is some errors");
        }


        if (appUpdateInfoOperation.IsSuccessful)
        {
            var appUpdateInfoResult = appUpdateInfoOperation.GetResult();

            if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.UpdateAvailable && appUpdateInfoResult.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions()))
            {
                var appUpdateOptions = AppUpdateOptions.ImmediateAppUpdateOptions();
                StartCoroutine(StartImmediateUpdate(appUpdateInfoResult, appUpdateOptions));
            }
        }
        else
        {
            print("there is no update for now");
        }
    }

    IEnumerator StartImmediateUpdate(AppUpdateInfo appUpdateInfo_i, AppUpdateOptions appUpdateOptions_i)
    {
        var startUpdateRequest = appUpdateManager.StartUpdate(appUpdateInfo_i, appUpdateOptions_i);
        yield return startUpdateRequest;
    }
#endif

}