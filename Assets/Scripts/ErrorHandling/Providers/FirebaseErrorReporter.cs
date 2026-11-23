#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using Firebase;
using Firebase.Extensions;
using UnityEngine;
using FirebaseCrashlytics = Firebase.Crashlytics.Crashlytics;

namespace ErrorHandling.Providers
{
    /// <summary>
    /// Crashlytics adapter. Calls made before the dependency check finishes are dropped, because touching
    /// Crashlytics earlier throws on devices with an outdated Google Play services.
    /// </summary>
    public sealed class FirebaseErrorReporter : IErrorReporter
    {
        private volatile bool _isReady;

        public bool CapturesUnhandledExceptions => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            FirebaseErrorReporter reporter = new FirebaseErrorReporter();
            ErrorReporter.SetReporter(reporter);
            reporter.Initialize();
        }

        private void Initialize()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    return;
                }

                FirebaseCrashlytics.ReportUncaughtExceptionsAsFatal = true;
                _isReady = true;
            });
        }

        public void RecordException(Exception exception)
        {
            if (_isReady)
            {
                FirebaseCrashlytics.LogException(exception);
            }
        }

        public void RecordError(string message, string stackTrace)
        {
            if (!_isReady)
            {
                return;
            }

            FirebaseCrashlytics.Log(message);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                FirebaseCrashlytics.Log(stackTrace);
            }
        }

        public void Log(string message)
        {
            if (_isReady)
            {
                FirebaseCrashlytics.Log(message);
            }
        }

        public void SetCustomKey(string key, string value)
        {
            if (_isReady)
            {
                FirebaseCrashlytics.SetCustomKey(key, value);
            }
        }

        public void SetUserId(string userId)
        {
            if (_isReady)
            {
                FirebaseCrashlytics.SetUserId(userId);
            }
        }
    }
}
#endif
