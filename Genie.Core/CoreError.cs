using System;

namespace GenieClient
{
    /// <summary>
    /// Lightweight error reporting for Genie.Core.
    /// Wire ErrorHandler to GenieError.Error in Program.cs to route errors to the full error system.
    /// </summary>
    public static class CoreError
    {
        public static Action<string, string, string> ErrorHandler { get; set; }

        public static event Action<string, string, string> EventCoreError;

        public static void Error(string section, string message, string description = null)
        {
            ErrorHandler?.Invoke(section, message, description);
            EventCoreError?.Invoke(section, message, description);
        }
    }
}
