using System.Reflection;
using BepInEx.Logging;
using LogUtils;

namespace ModLib.Logging;

/// <summary>
///     Wrapper class for a LogUtils logger instance.
/// </summary>
public class LogUtilsLogger(ILogger logger) : ModLogger
{
    internal readonly bool ModLibCreated = Assembly.GetCallingAssembly() == Core.MyAssembly;

    /// <inheritdoc/>
    public override void Log(LogLevel category, object message) => logger.Log(category, message);

    /// <inheritdoc/>
    public override object GetLogSource() => logger;
}