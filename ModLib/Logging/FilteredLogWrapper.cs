using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     A wrapper around an <see cref="ModLogger"/> instance, where logs are only processed
///     if their logging level is not within a given blacklist.
/// </summary>
/// <param name="logger">The logger instance to be wrapped.</param>
/// <param name="filterLevels">The logging level(s) to be excluded from logging.</param>
public class FilteredLogWrapper(ModLogger logger, LogLevel filterLevels) : ModLogger
{
    internal static List<FilteredLogWrapper> DynamicInstances { get; } = [];

    /// <summary>
    ///     The log levels which will be ignored by this logger instance.
    /// </summary>
    public LogLevel FilterLogLevels { get; set; } = filterLevels;

    /// <summary>
    ///     The internally wrapped logger.
    /// </summary>
    public ModLogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger), "Wrapped logger instance cannot be null.");

    /// <summary>
    ///     Creates a log wrapper with ModLib's default logging verbosity settings; All logs of <c>Debug</c> level are ignored, unless in Debug Mode.
    /// </summary>
    /// <param name="logger">The logger instance to be wrapped.</param>
    public FilteredLogWrapper(ModLogger logger)
        : this(logger, Extras.DebugMode ? LogLevel.None : LogLevel.Debug)
    {
        DynamicInstances.Add(this);
    }

    /// <inheritdoc/>
    public override object GetLogSource() => Logger.GetLogSource();

    /// <inheritdoc/>
    /// <remarks>
    ///     This method will only process log requests if <paramref name="level"/> is not blacklisted for logging by this instance.
    /// </remarks>
    public override void Log(LogLevel level, object data)
    {
        if (level is LogLevel.None || FilterLogLevels.HasFlag(level)) return;

        Logger.Log(level, data);
    }

    /// <summary>
    ///     Returns a string representing the current object and its wrapped <see cref="ModLogger"/> instance.
    /// </summary>
    /// <returns>A string representing the current object and its wrapped <see cref="ModLogger"/> instance.</returns>
    public override string ToString() => $"{base.ToString()} ({Logger})";
}