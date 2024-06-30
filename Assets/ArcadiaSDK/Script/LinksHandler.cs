using UnityEngine;

public class LinksHanler
{
    private static string iosMoreGamesURL = "https://apps.apple.com/us/developer/muhammad-umar-shafaqat/id1671095846";
    private static string androidMoreGamesURL = "https://play.google.com/store/apps/dev?id=5230473161163797991";
    private static string universalMoreGamesURL = "https://YOUR_MORE_GAMES_URL";
    private static string privacyPolicyURL = "https://sites.google.com/view/arcadia-adventures/home";

    public static void RateUs()
    {
        string rateUsURL = GetRateUsURL();
        if (!string.IsNullOrEmpty(rateUsURL))
        {
            OpenURL(rateUsURL);
        }
        else
        {
            Debug.Log("Platform not supported for Rate Us");
        }
    }

    public static void MoreGames()
    {
        #if UNITY_IOS
            OpenURL(iosMoreGamesURL);
        #elif UNITY_ANDROID
            OpenURL(androidMoreGamesURL);
        #else
            OpenURL(universalMoreGamesURL);
        #endif
    }

    private static string GetRateUsURL()
    {
        #if UNITY_IOS
            string appId = Application.identifier; // iOS bundle identifier
            return $"itms-apps://itunes.apple.com/app/id{appId}";
        #elif UNITY_ANDROID
            string packageName = Application.identifier; // Android package name
            return $"market://details?id={packageName}";
        #else
            return null; // Platform not supported
        #endif
    }

    private static void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
    public static void PrivacyPolicy()
    {
        OpenURL(privacyPolicyURL);
    }
}
