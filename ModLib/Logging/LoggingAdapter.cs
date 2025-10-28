using System;
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
    public static IMyLogger CreateLogger(ManualLogSource logSource)
    {
        if (!Extras.LogUtilsAvailable)
            return new FallbackLogger(logSource);

        try
        {
            return LogUtilsHelper.CreateLogger(logSource);
        }
        catch (Exception ex)
        {
            Core.LogSource?.LogError($"Failed to create logger instance: {ex}");

            return new FallbackLogger(logSource);
        }
    }
}