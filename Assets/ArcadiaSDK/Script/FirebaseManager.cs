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
    public void LogEvent(string eventName, string parameterName, string parameterValue)
    {
        FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
    }

    // Log a non-fatal error to Firebase Crashlytics
    public void LogError(string message)
    {
        //FirebaseCrashlytics.Log(message);
    }
    public static void LogLevelStartEvent(int levelName)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart,
            new Parameter(FirebaseAnalytics.ParameterLevelName, levelName));
    }

    public static void LogLevelEndEvent(int levelName, int score=-1)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
            new Parameter(FirebaseAnalytics.ParameterLevelName, levelName),
            new Parameter(FirebaseAnalytics.ParameterScore, score));
    }
    public static void LogPostScoreEvent(int levelName, int score)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPostScore,
            new Parameter(FirebaseAnalytics.ParameterLevelName, levelName),
            new Parameter(FirebaseAnalytics.ParameterScore, score));
    }

    public static void LogSelectContentEvent(string contentType, string itemId)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSelectContent,
            new Parameter(FirebaseAnalytics.ParameterContentType, contentType),
            new Parameter(FirebaseAnalytics.ParameterItemId, itemId));
    }

    public static void LogSpendVirtualCurrencyEvent(string itemName, string virtualCurrencyName, int value)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSpendVirtualCurrency,
            new Parameter(FirebaseAnalytics.ParameterItemName, itemName),
            new Parameter(FirebaseAnalytics.ParameterVirtualCurrencyName, virtualCurrencyName),
            new Parameter(FirebaseAnalytics.ParameterValue, value));
    }

    public static void LogTutorialBeginEvent()
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialBegin);
    }

    public static void LogTutorialCompleteEvent()
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialComplete);
    }

    public static void LogUnlockAchievementEvent(string achievementId)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventUnlockAchievement,
            new Parameter(FirebaseAnalytics.ParameterAchievementId, achievementId));
    }
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
