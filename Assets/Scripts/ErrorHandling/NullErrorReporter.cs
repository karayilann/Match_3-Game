using System;

namespace ErrorHandling
{
    public sealed class NullErrorReporter : IErrorReporter
    {
        public static readonly NullErrorReporter INSTANCE = new NullErrorReporter();

        public bool CapturesUnhandledExceptions => false;

        private NullErrorReporter()
        {
        }

        public void RecordException(Exception exception)
        {
        }

        public void RecordError(string message, string stackTrace)
        {
        }

        public void Log(string message)
        {
        }

        public void SetCustomKey(string key, string value)
        {
        }

        public void SetUserId(string userId)
        {
        }
    }
}
