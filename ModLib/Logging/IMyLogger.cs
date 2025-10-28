using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     Base interface providing the same methods of a <see cref="ManualLogSource"/> object.
/// </summary>
/// <remarks>
///     Implementation of this interface will depend on whether LogUtils is present at runtime.
/// </remarks>
public interface IMyLogger
{
    /// <summary>
    ///     Logs a message with the default logging level.
    /// </summary>
    /// <param name="message">Message to log.</param>
    void Log(object message);

    /// <inheritdoc cref="ManualLogSource.Log"/>
    void Log(LogLevel level, object data);

    /// <inheritdoc cref="ManualLogSource.LogDebug"/>
    void LogDebug(object data);

    /// <inheritdoc cref="ManualLogSource.LogError"/>
    void LogError(object data);

    /// <inheritdoc cref="ManualLogSource.LogFatal"/>
    void LogFatal(object data);

    /// <inheritdoc cref="ManualLogSource.LogInfo"/>
    void LogInfo(object data);

    /// <inheritdoc cref="ManualLogSource.LogMessage"/>
    void LogMessage(object data);

    /// <inheritdoc cref="ManualLogSource.LogWarning"/>
    void LogWarning(object data);

    /// <summary>
    ///     Retrieves the internal log source of this logger implementation.
    /// </summary>
    /// <returns>The log source of this logger implementation.</returns>
    object GetLogSource();
}