/*#if UNITY_IPHONE || UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using System.IO;

public class PostBuildProcessor : MonoBehaviour
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        var appId = ArcadiaSdkManager.GetAdmobAppID();


        var plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Actually set (then write) AdMob app id to Info.plist if valid
        plist.root.SetString("GADApplicationIdentifier", appId);

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
*/

