using System.Runtime.CompilerServices;
using BepInEx.Logging;
using ModLib.Options;

namespace ModLib.Logging;

/// <summary>
///     A wrapper around an <see cref="IMyLogger"/> instance, where logs are only processed
///     if they meet a minimal threshold logging level.
/// </summary>
/// <param name="logger">The logger instance to be wrapped.</param>
/// <param name="maxLogLevel">The max log level allowed for logging.</param>
public class LogWrapper(IMyLogger logger, LogLevel maxLogLevel) : IMyLogger
{
    /// <summary>
    ///     Creates a log wrapper with ModLib's default logging verbosity settings;
    ///     All logs below <c>Message</c> level (i.e. whose number is greater than <c>8</c>) are ignored, unless the Dev Tools mod is enabled.
    /// </summary>
    /// <param name="logger">The logger instance to be wrapped.</param>
    public LogWrapper(IMyLogger logger)
        : this(logger, OptionUtils.IsOptionEnabled("modlib.debug") ? LogLevel.All : LogLevel.Message)
    {
    }

    /// <inheritdoc/>
    public object GetLogSource() => logger.GetLogSource();

    /// <inheritdoc cref="Log(LogLevel, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(object message) => Log(LogLevel.Debug, message);

    /// <inheritdoc/>
    /// <remarks>
    ///     This method will only process log requests if <paramref name="level"/> is of equal importance or higher than the max <see cref="LogLevel"/> allowed.
    /// </remarks>
    public void Log(LogLevel level, object data)
    {
        if (level is LogLevel.None || maxLogLevel is LogLevel.None || maxLogLevel < level)
        {
            return;
        }

        logger.Log(level, data);
    }

    /// <inheritdoc/>
    public void LogDebug(object data) => logger.LogDebug(data);
    /// <inheritdoc/>
    public void LogError(object data) => logger.LogError(data);
    /// <inheritdoc/>
    public void LogFatal(object data) => logger.LogFatal(data);
    /// <inheritdoc/>
    public void LogInfo(object data) => logger.LogInfo(data);
    /// <inheritdoc/>
    public void LogMessage(object data) => logger.LogMessage(data);
    /// <inheritdoc/>
    public void LogWarning(object data) => logger.LogWarning(data);
}