using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     Base class providing a compatibility layer between loggers from various potential sources (e.g. BepInEx, Unity, LogUtils, etc.)
/// </summary>
public abstract class ModLogger
{
    static ModLogger()
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

    /// <summary>
    ///     The default value of the <see cref="AllowedLogLevels"/> property. This field is constant.
    /// </summary>
    public const LogLevel DefaultLogLevels = LogLevel.All;

    /// <summary>
    ///     Specifies that error and warning logs are to be included. This field is constant.
    /// </summary>
    public const LogLevel ErrorLevels = LogLevel.Fatal | LogLevel.Error | LogLevel.Warning;

    /// <summary>
    ///     Specifies that informative logs are to be included. This field is constant.
    /// </summary>
    public const LogLevel InfoLevels = LogLevel.Message | LogLevel.Info;

    /// <summary>
    ///     Specifies that all non-debug logs are to be included. This field is constant.
    /// </summary>
    public const LogLevel NonDebugLevels = ErrorLevels | InfoLevels;

    /// <summary>
    ///     The log levels which will be ignored by this logger instance.
    /// </summary>
    public LogLevel AllowedLogLevels { get; set; }

    /// <summary>
    ///     Creates a new logger instance which logs messages from all log levels.
    /// </summary>
    public ModLogger()
    {
        AllowedLogLevels = DefaultLogLevels;
    }

    /// <summary>
    ///     Creates a new logger instance which only logs messages of the specified logging level(s).
    /// </summary>
    /// <param name="allowLevels">The logging level(s) that will be processed for logging. Log requests whose log level is not specified here are ignored.</param>
    public ModLogger(LogLevel allowLevels)
    {
        AllowedLogLevels = allowLevels;
    }

    /// <inheritdoc cref="ManualLogSource.Log"/>
    public void Log(LogLevel level, object data)
    {
        if (level is LogLevel.None || !AllowedLogLevels.HasFlag(level)) return;

        try
        {
            LogImplementation(level, data);
        }
        catch (Exception ex)
        {
            Core.LogSource.LogError(ex);
        }
    }

    /// <summary>
    ///     Logs a message with the default logging level.
    /// </summary>
    /// <param name="message">Message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(object message) => Log(LogLevel.Debug, message);

    /// <inheritdoc cref="ManualLogSource.LogDebug"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogDebug(object data) => Log(LogLevel.Debug, data);

    /// <inheritdoc cref="ManualLogSource.LogInfo"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInfo(object data) => Log(LogLevel.Info, data);

    /// <inheritdoc cref="ManualLogSource.LogMessage"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogMessage(object data) => Log(LogLevel.Message, data);

    /// <inheritdoc cref="ManualLogSource.LogWarning"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogWarning(object data) => Log(LogLevel.Warning, data);

    /// <inheritdoc cref="ManualLogSource.LogError"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogError(object data) => Log(LogLevel.Error, data);

    /// <inheritdoc cref="ManualLogSource.LogFatal"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogFatal(object data) => Log(LogLevel.Fatal, data);

    /// <summary>
    ///     Retrieves the internal log source of this logger instance.
    /// </summary>
    /// <returns>The log source used by this logger instance, or <c>null</c> if a source is optional for this logger type and none was provided.</returns>
    public abstract object? GetLogSource();

    /// <summary>
    ///     Provides the logging implementation method for this logger type.
    /// </summary>
    /// <param name="logLevel">The level of the logging message.</param>
    /// <param name="data">The data to be logged.</param>
    protected abstract void LogImplementation(LogLevel logLevel, object data);
}