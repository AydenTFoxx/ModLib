using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     Base class providing a compatibility layer between loggers from various potential sources (e.g. BepInEx, Unity, LogUtils, etc.)
/// </summary>
public abstract class ModLogger
{
    /// <summary>
    ///     Retrieves the internal log source of this logger implementation.
    /// </summary>
    /// <returns>The log source of this logger implementation.</returns>
    public abstract object GetLogSource();

    /// <inheritdoc cref="ManualLogSource.Log"/>
    public abstract void Log(LogLevel level, object data);

    /// <summary>
    ///     Logs a message with the default logging level.
    /// </summary>
    /// <param name="message">Message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(object message) => Log(LogLevel.Debug, message);

    /// <inheritdoc cref="ManualLogSource.LogDebug"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogDebug(object data) => Log(LogLevel.Debug, data);

    /// <inheritdoc cref="ManualLogSource.LogError"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogError(object data) => Log(LogLevel.Error, data);

    /// <inheritdoc cref="ManualLogSource.LogFatal"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogFatal(object data) => Log(LogLevel.Fatal, data);

    /// <inheritdoc cref="ManualLogSource.LogInfo"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInfo(object data) => Log(LogLevel.Info, data);

    /// <inheritdoc cref="ManualLogSource.LogMessage"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogMessage(object data) => Log(LogLevel.Message, data);

    /// <inheritdoc cref="ManualLogSource.LogWarning"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogWarning(object data) => Log(LogLevel.Warning, data);
}