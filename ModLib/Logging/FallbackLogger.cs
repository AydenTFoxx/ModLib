using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     Default wrapper for a <see cref="ManualLogSource"/>; Used when LogUtils is not avaliable at runtime.
/// </summary>
public class FallbackLogger(ManualLogSource logSource) : IMyLogger
{
    /// <inheritdoc/>
    public void Log(object message) => logSource.Log(LogLevel.Info, message);
    /// <inheritdoc/>
    public void Log(LogLevel category, object message) => logSource.Log(category, message);

    /// <inheritdoc/>
    public void LogDebug(object data) => Log(LogLevel.Debug, data);
    /// <inheritdoc/>
    public void LogError(object data) => Log(LogLevel.Error, data);
    /// <inheritdoc/>
    public void LogFatal(object data) => Log(LogLevel.Fatal, data);
    /// <inheritdoc/>
    public void LogInfo(object data) => Log(LogLevel.Info, data);
    /// <inheritdoc/>
    public void LogMessage(object data) => Log(LogLevel.Message, data);
    /// <inheritdoc/>
    public void LogWarning(object data) => Log(LogLevel.Warning, data);

    /// <inheritdoc/>
    public ILogSource GetLogSource() => logSource;
}