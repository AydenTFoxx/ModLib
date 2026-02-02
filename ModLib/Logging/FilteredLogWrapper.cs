using System.Collections.Generic;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     A wrapper around an <see cref="ModLogger"/> instance, where logs are only processed
///     if they meet a minimal threshold logging level.
/// </summary>
/// <param name="logger">The logger instance to be wrapped.</param>
/// <param name="maxLogLevel">The max log level allowed for logging.</param>
public class FilteredLogWrapper(ModLogger logger, LogLevel maxLogLevel) : ModLogger
{
    internal static List<FilteredLogWrapper> DynamicInstances { get; } = [];

    /// <summary>
    ///     The max "priority" level allowed for logging; Any message whose level is lower than this will be ignored.
    /// </summary>
    public LogLevel MaxLogLevel { get; set; } = maxLogLevel;

    /// <summary>
    ///     Creates a log wrapper with ModLib's default logging verbosity settings; All logs of type <c>Debug</c> or lower are ignored, unless in Debug Mode.
    /// </summary>
    /// <param name="logger">The logger instance to be wrapped.</param>
    public FilteredLogWrapper(ModLogger logger)
        : this(logger, Extras.DebugMode ? LogLevel.All : LogLevel.Info)
    {
        DynamicInstances.Add(this);
    }

    /// <inheritdoc/>
    public override object GetLogSource() => logger.GetLogSource();

    /// <inheritdoc/>
    /// <remarks>
    ///     This method will only process log requests if <paramref name="level"/> is of equal importance or higher than the max <see cref="LogLevel"/> allowed.
    /// </remarks>
    public override void Log(LogLevel level, object data)
    {
        if (level is LogLevel.None || MaxLogLevel is LogLevel.None || MaxLogLevel < level)
        {
            return;
        }

        logger.Log(level, data);
    }

    /// <summary>
    ///     Returns a string representing the current object and its wrapped <see cref="ModLogger"/> instance.
    /// </summary>
    /// <returns>A string representing the current object and its wrapped <see cref="ModLogger"/> instance.</returns>
    public override string ToString() => $"{base.ToString()} ({logger})";
}