using System;
using System.Reflection;
using BepInEx.Logging;
using LogUtils;

namespace ModLib.Logging;

/// <summary>
///     Wrapper class for a LogUtils logger instance.
/// </summary>
public class LogUtilsLogger : ModLogger
{
    internal readonly bool ModLibCreated = Assembly.GetCallingAssembly() == Core.MyAssembly;

    private readonly ILogger LogSource;

    /// <summary>
    ///     Creates a new LogUtils logger wrapper with the specified source and default filter options.
    /// </summary>
    /// <param name="logger">The LogUtils-provided logger to be used by this instance.</param>
    public LogUtilsLogger(ILogger logger)
        : this(logger, DefaultLogLevels)
    {
    }

    /// <summary>
    ///     Creates a new LogUtils logger wrapper with the specified source and log level filter options.
    /// </summary>
    /// <param name="logger">The LogUtils-provided logger to be used by this instance.</param>
    /// <param name="allowLevels">The logging level(s) that will be processed for logging. Log requests whose log level is not specified here are ignored.</param>
    public LogUtilsLogger(ILogger logger, LogLevel allowLevels)
        : base(allowLevels)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger), "LogUtils logger instance cannot be null.");

        LogSource = logger;
    }

    /// <inheritdoc/>
    public override object GetLogSource() => LogSource;

    /// <inheritdoc/>
    protected override void LogImplementation(LogLevel category, object message) => LogSource.Log(category, message);
}