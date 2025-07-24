using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine.Events;

public class FirebaseManager : MonoBehaviour
{
    private static FirebaseManager Agent;

    void Awake()
    {
        if (Agent == null)
        {
            Agent = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public static UnityEvent<bool> onInitialize;
    static FirebaseApp app;
    public static void InitializeFirebase()
    {
      FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(
        previousTask => 
        {
          var dependencyStatus = previousTask.Result;
          if (dependencyStatus == Firebase.DependencyStatus.Available) 
          {
            // Create and hold a reference to your FirebaseApp,
            app = Firebase.FirebaseApp.DefaultInstance;
            // Set the recommended Crashlytics uncaught exception behavior.
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            onInitialize.Invoke(true);
          } 
          else 
          {
            onInitialize.Invoke(false);
            UnityEngine.Debug.LogError(
              $"Could not resolve all Firebase dependencies: {dependencyStatus}\n" +
              "Firebase Unity SDK is not safe to use here");
          }
        });
    }

    // Log a custom event to Firebase Analytics
    public static void LogEvent(string eventName, string parameterName, string parameterValue)
    {
        FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
    }
    public static void LogLevelStartEvent(int levelName)
    {
       FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart,
           new Parameter(FirebaseAnalytics.ParameterLevelName, levelName));
    }



//

     public static void LogLevelFailEvent(int levelName, int score=-1)
    {
       FirebaseAnalytics.LogEvent("level_fail",
           new Parameter(FirebaseAnalytics.ParameterLevelName, levelName),
           new Parameter(FirebaseAnalytics.ParameterScore, score));
    }

    public static void LogLevelCompleteEvent(int levelName, int score=-1)
    {
       FirebaseAnalytics.LogEvent("levelComplete",
           new Parameter(FirebaseAnalytics.ParameterLevelName, levelName),
           new Parameter(FirebaseAnalytics.ParameterScore, score));
    }
}
