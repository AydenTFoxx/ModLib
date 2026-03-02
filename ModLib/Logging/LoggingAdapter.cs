using System;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ModLib.Logging;

// Credits to Fluffball (@TheVileOne) for the templates at LogUtils.Templates :P

/// <summary>
///     Intermediary helper for creating loggers with the appropriate logging backend.
/// </summary>
public static class LoggingAdapter
{
    /// <summary>
    ///     Creates a logger instance employing a safe encapsulation technique.
    /// </summary>
    /// <param name="logSource">The BepInEx source to use for identification and logging.</param>
    /// <param name="allowLevels">If specified, the logging levels that the new logger instance will listen to. By default, all log levels are enabled.</param>
    public static ModLogger CreateLogger(ManualLogSource logSource, LogLevel allowLevels = LogLevel.All)
    {
        return Extras.LogUtilsAvailable
            ? LogUtilsAccess.CreateLogger(logSource, allowLevels)
            : new FallbackLogger(logSource, allowLevels);
    }

    private static class LogUtilsAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ModLogger CreateLogger(ManualLogSource logSource, LogLevel filterLevels)
        {
            try
            {
                return LogUtilsHelper.CreateLogger(logSource, filterLevels);
            }
            catch (Exception ex)
            {
                Core.LogSource.LogError($"Failed to create logger instance: {ex}");

                return new FallbackLogger(logSource);
            }
        }
    }
}