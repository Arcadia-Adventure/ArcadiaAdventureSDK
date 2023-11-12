using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_IOS || UNITY_IPHONE

namespace AudienceNetwork
{
    public static class AdSettings
    {
        [DllImport("__Internal")] 
        private static extern void FBAdSettingsBridgeSetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled);

        public static void SetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled)
        {
            FBAdSettingsBridgeSetAdvertiserTrackingEnabled(advertiserTrackingEnabled);
            if(ArcadiaSdkManager.Agent.enableLogs)
                Debug.Log(" :* Facebook ATT Status: "+advertiserTrackingEnabled);
        }
    }
}

#endif
