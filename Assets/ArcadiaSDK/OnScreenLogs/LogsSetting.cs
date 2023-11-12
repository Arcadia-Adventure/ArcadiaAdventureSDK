//#if UNITY_EDITOR || DEVELOPMENT_BUILD

using IngameDebugConsole;
using UnityEngine;
using System;
using System.Collections;
using Object = System.Object;

public static class LogsSetting
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void OnAfterSceneLoadRuntimeMethod()
	{
		if (ArcadiaSdkManager.Agent.enableLogs)
		{
			FPSLabel fpsLabel = GameObject.FindObjectOfType<FPSLabel>();
			DebugLogManager InGameLogs = GameObject.FindObjectOfType<DebugLogManager>();
			if (fpsLabel == null)
			{
				GameObject fpsLabelGO = new GameObject("FPS Label");
				fpsLabelGO.AddComponent<FPSLabel>();
				Application.targetFrameRate = 60;
			}
			if (InGameLogs==null)
			{
				GameObject obj=Resources.Load<GameObject>("IngameDebugConsole");
				GameObject.Instantiate(obj);
			}
		}
	}
}
//#endif