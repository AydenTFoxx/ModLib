using System.Reflection;
using BepInEx.Logging;
using LogUtils;

namespace ModLib.Logging;

/// <summary>
///     Wrapper class for a LogUtils logger instance.
/// </summary>
public class LogUtilsAdapter(ILogger logger) : IMyLogger
{
    internal readonly bool ModLibCreated = Assembly.GetCallingAssembly() == Core.MyAssembly;

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
    public void Log(object message) => logger.Log(message);
    /// <inheritdoc/>
    public void Log(LogLevel category, object message) => logger.Log(category, message);

    /// <inheritdoc/>
    public object GetLogSource() => logger;
}