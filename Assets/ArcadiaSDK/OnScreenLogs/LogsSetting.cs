//#if UNITY_EDITOR || DEVELOPMENT_BUILD

using IngameDebugConsole;
using UnityEngine;
using System;
using System.Collections;
using Object = System.Object;

public static class LogsSetting
{
	static GameObject fpsLabelGO;
	static GameObject ingamedebuger;
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void OnAfterSceneLoadRuntimeMethod()
	{
		//ArcadiaSdkManager.Agent.SetupLog();
		if (ArcadiaSdkManager.Agent.GetLog())
		{
			FPSLabel fpsLabel = GameObject.FindObjectOfType<FPSLabel>();
			DebugLogManager InGameLogs = GameObject.FindObjectOfType<DebugLogManager>();
			if (fpsLabel == null)
			{
				fpsLabelGO = new GameObject("FPS Label");
				fpsLabelGO.AddComponent<FPSLabel>();
				Application.targetFrameRate = 60;
			}
			if (InGameLogs==null)
			{
				GameObject obj=Resources.Load<GameObject>("IngameDebugConsole");
				ingamedebuger=GameObject.Instantiate(obj);
			}
		}
		else
		{
			fpsLabelGO?.SetActive(false);
			ingamedebuger?.SetActive(false);
		}
	}
}
//#endif