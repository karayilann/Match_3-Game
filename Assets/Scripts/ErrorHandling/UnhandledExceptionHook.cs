using System.Threading;
using UnityEngine;

namespace ErrorHandling
{
    /// <summary>
    /// Forwards exceptions that nothing caught to the active reporter. The per-session cap stops an exception
    /// thrown every frame from flooding the backend.
    /// </summary>
    public static class UnhandledExceptionHook
    {
        private const int MAX_REPORTS_PER_SESSION = 50;

        private static int _reportCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Application.logMessageReceivedThreaded -= HandleLogMessage;
            Application.logMessageReceivedThreaded += HandleLogMessage;
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            IErrorReporter reporter = ErrorReporter.Current;
            if (reporter.CapturesUnhandledExceptions)
            {
                return;
            }

            if (Interlocked.Increment(ref _reportCount) > MAX_REPORTS_PER_SESSION)
            {
                return;
            }

            reporter.RecordError(condition, stackTrace);
        }
    }
}
