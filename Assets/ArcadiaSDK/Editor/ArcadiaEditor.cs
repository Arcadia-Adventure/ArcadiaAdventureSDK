using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using UnityEditor;

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
		Selection.activeGameObject = GameObject.FindObjectOfType<ArcadiaSdkManager>().gameObject;

		Debug.Log("Arcadia SDK Successfully Configured");
	}

	static void SDKConfig()
	{
		
		AA_AnalyticsManager.SetGAIds();
		
	
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
			if (edited) {
				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines.ToArray()));
			}
		}
	}

}
#endif