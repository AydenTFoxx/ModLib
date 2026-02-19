using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ModLib.Logging;

// Credits to Fluffball (@TheVileOne) for the templates at LogUtils.Templates :P

/// <summary>
///     Intermediary helper for creating loggers with the appropriate logging backend.
/// </summary>
public static class LoggingAdapter
{
    static LoggingAdapter()
    {
        try
        {
            if (!Directory.Exists(Core.LogsPath))
            {
                Directory.CreateDirectory(Core.LogsPath);
            }
        }
        catch (Exception ex)
        {
            Core.LogSource.LogError($"Failed to create Logs folder! {ex}");
        }
    }

    /// <inheritdoc cref="CreateLogger(ManualLogSource, BepInEx.Logging.LogLevel)"/>
    [Obsolete("Prefer specifying the levels to be excluded (if any) with CreateLogger(ManualLogSource, LogLevel) instead. This overload will be removed in a future update.")]
    public static ModLogger CreateLogger(ManualLogSource logSource, bool wrapLogger) => CreateLogger(logSource, wrapLogger ? LogLevel.Debug : LogLevel.None);

    /// <summary>
    ///     Creates a logger instance employing a safe encapsulation technique.
    /// </summary>
    /// <param name="logSource">The BepInEx source to use for identification and logging.</param>
    /// <param name="filterLevels">If set, the generated logger will be a <see cref="FilteredLogWrapper"/> which ignores log requests with the given logging level(s).</param>
    public static ModLogger CreateLogger(ManualLogSource logSource, LogLevel filterLevels = LogLevel.None)
    {
        ModLogger result = Extras.LogUtilsAvailable
            ? LogUtilsAccess.CreateLogger(logSource)
            : new FallbackLogger(logSource);

        return filterLevels is not LogLevel.None
            ? new FilteredLogWrapper(result, filterLevels)
            : result;
    }

    private static class LogUtilsAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ModLogger CreateLogger(ManualLogSource logSource)
        {
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
}