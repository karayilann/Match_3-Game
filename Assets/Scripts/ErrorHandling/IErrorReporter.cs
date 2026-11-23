using System;

namespace ErrorHandling
{
    /// <summary>
    /// Backend-agnostic error sink. Implementations may be called from any thread because Unity can raise
    /// log callbacks off the main thread.
    /// </summary>
    public interface IErrorReporter
    {
        /// <summary>
        /// True when the backend already hooks Unity's log callback itself (Crashlytics does), so
        /// <see cref="UnhandledExceptionHook"/> skips forwarding and avoids duplicate reports.
        /// </summary>
        bool CapturesUnhandledExceptions { get; }

        void RecordException(Exception exception);
        void RecordError(string message, string stackTrace);
        void Log(string message);
        void SetCustomKey(string key, string value);
        void SetUserId(string userId);
    }
}
