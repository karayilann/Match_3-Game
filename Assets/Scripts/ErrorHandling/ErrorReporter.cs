using System;

namespace ErrorHandling
{
    /// <summary>
    /// Single entry point for error reporting. Game code never references a concrete backend, so swapping
    /// Firebase for another service only means calling <see cref="SetReporter"/> with a different implementation.
    /// </summary>
    public static class ErrorReporter
    {
        private static volatile IErrorReporter _reporter = NullErrorReporter.INSTANCE;

        public static IErrorReporter Current => _reporter;

        public static void SetReporter(IErrorReporter reporter)
        {
            _reporter = reporter ?? NullErrorReporter.INSTANCE;
        }

        public static void RecordException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            _reporter.RecordException(exception);
        }

        public static void RecordError(string message, string stackTrace = null)
        {
            _reporter.RecordError(message, stackTrace);
        }

        public static void Log(string message)
        {
            _reporter.Log(message);
        }

        public static void SetCustomKey(string key, string value)
        {
            _reporter.SetCustomKey(key, value);
        }

        public static void SetUserId(string userId)
        {
            _reporter.SetUserId(userId);
        }
    }
}
