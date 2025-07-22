using Microsoft.Extensions.Logging;

namespace ResourceUtilityLib.Logging
{
    public static class LogHelper
    {
        private static ILogger? _logger;


        public static void SetLogger(ILogger? logger)
        {
            _logger = logger;
        }

        public static void Verbose(string message, params object[] args) =>
            _logger?.LogTrace(message, args);

        public static void Warn(string message, params object[] args) =>
            _logger?.LogWarning(message, args);

        public static void Info(string message, params object[] args) =>
            _logger?.LogInformation(message, args);

        public static void Error(string message, params object[] args) =>
            _logger?.LogError(message, args);

        public static void Error(Exception ex, string message = "An exception occurred", params object[] args) =>
            _logger?.LogError(ex, message, args);

        public static void Debug(string message, params object[] args) =>
            _logger?.LogDebug(message, args);

        public static void Fatal(Exception ex, string message = "Fatal error", params object[] args) =>
            _logger?.LogCritical(ex, message, args);
    }
}
