#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Reflection;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using UnityEditor;
using GoogleMobileAds.Editor;
namespace GoogleMobileAds.Editor
{
	public class ArcadiaEditor : EditorWindow
	{
		[MenuItem("ArcadiaSDK/SDK Setup", false, 100)]
		public static void Creat()
		{
			ArcadiaSdkManager ads = GameObject.FindObjectOfType<ArcadiaSdkManager>();
			GameAnalytics GA = GameObject.FindObjectOfType<GameAnalytics>();
			if (ads == null)
			{
				GameObject obj = new GameObject("ArcadiaSdkManager");
				obj.AddComponent<ArcadiaSdkManager>();
				obj.GetComponent<ArcadiaSdkManager>().LoadGameIds();
			}
			else
			{
				ads.GetComponent<ArcadiaSdkManager>().LoadGameIds();
			}

			if (GA == null)
			{
				GameObject obj = new GameObject("GameAnalytics");
				obj.AddComponent<GameAnalytics>();
			}


			SDKConfig();
			AdmobAppIDConfig.SetAppID(ArcadiaSdkManager.myGameIds.admobAppId, ArcadiaSdkManager.myGameIds.platform.Contains("IOS")? AdmobAppIDConfig.Platform.IOS : AdmobAppIDConfig.Platform.Android);
			Selection.activeGameObject = GameObject.FindObjectOfType<ArcadiaSdkManager>().gameObject;

			Debug.Log("Arcadia SDK Successfully Configured");
		}

		static void SDKConfig()
		{

			GameAnalyticsManager.SetGAIds();
			//pass ids here
			UpdateDefines("gameanalytics_enabled", true, new BuildTargetGroup[] { BuildTargetGroup.iOS, BuildTargetGroup.Android });
		}



		private static void UpdateDefines(string entry, bool enabled, BuildTargetGroup[] groups)
		{
			foreach (var group in groups)
			{
				var defines = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
				var edited = false;
				if (enabled && !defines.Contains(entry))
				{
					defines.Add(entry);
					edited = true;
				}
				else if (!enabled && defines.Contains(entry))
				{
					defines.Remove(entry);
					edited = true;
				}
				if (edited)
				{
					PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines.ToArray()));
				}
			}
		}

	}
}

public class AdmobAppIDConfig
{
	public enum Platform
{
    Android,
    IOS
}
    public static void SetAppID(string ID, Platform platform)
    {
        Assembly editorAssembly = Assembly.Load("GoogleMobileAds.Editor");

        if (editorAssembly != null)
        {
            // Get the type of the GoogleMobileAdsSettings class.
            Type settingsType = editorAssembly.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");

            if (settingsType != null)
            {
                // Call the LoadInstance method via reflection.
                MethodInfo loadInstanceMethod = settingsType.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (loadInstanceMethod != null)
                {
                    // Invoke the method to get an instance of the settings class.
                    object settingsInstance = loadInstanceMethod.Invoke(null, null);

                    if (settingsInstance != null)
                    {
                        // Select the appropriate property based on the platform.
                        string propertyName = platform == Platform.Android 
                            ? "GoogleMobileAdsAndroidAppId" 
                            : "GoogleMobileAdsIOSAppId";

                        PropertyInfo appIdProperty = settingsType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                        if (appIdProperty != null)
                        {
                            // Set the App ID.
                            appIdProperty.SetValue(settingsInstance, ID);

                            // Save the asset changes.
                            EditorUtility.SetDirty(settingsInstance as UnityEngine.Object);
                            AssetDatabase.SaveAssets();

                            Debug.Log($"AdMob {platform} App ID set to: {ID}");
                        }
                        else
                        {
                            Debug.LogError($"{propertyName} property not found.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("LoadInstance method not found.");
                }
            }
            else
            {
                Debug.LogError("GoogleMobileAdsSettings class not found in the assembly.");
            }
        }
        else
        {
            Debug.LogError("GoogleMobileAds.Editor assembly not found.");
        }
    }
}

#endif