using System;
using System.IO;
using BepInEx.Logging;
using ModLib.Extensions;

namespace ModLib.Logging;

/// <summary>
///     A logger type which logs to a custom file with basic formatting and timestamps.
/// </summary>
public class FallbackLogger : ModLogger
{
    private readonly string PathToLogFile;

    private readonly ManualLogSource? LogSource;

    /// <summary>
    ///     Creates a new default logger with the given log source and the default logging filter.
    /// </summary>
    /// <param name="logSource">The source used for logging to BepInEx.</param>
    /// <exception cref="ArgumentException">logSource is null and the caller mod assembly is not registered to ModLib.</exception>
    public FallbackLogger(ManualLogSource? logSource)
        : this(logSource, DefaultLogLevels)
    {
    }

    /// <summary>
    ///     Creates a new default logger with the given log source and logging filters.
    /// </summary>
    /// <param name="logSource">
    ///     The source used for logging to BepInEx.
    ///     If the caller mod is registered to ModLib, this argument may be passed as <c>null</c>
    /// </param>
    /// <param name="allowLevels">The logging level(s) that will be processed for logging. Log requests whose log level is not specified here are ignored.</param>
    /// <exception cref="ArgumentException">logSource is null and the caller mod assembly is not registered to ModLib.</exception>
    public FallbackLogger(ManualLogSource? logSource, LogLevel allowLevels)
        : base(allowLevels)
    {
        LogSource = logSource;

        PathToLogFile = Path.Combine(Registry.DefaultLogsPath, Registry.SanitizeModName(logSource?.SourceName ?? Registry.GetMod(AssemblyExtensions.GetCallingAssembly() ?? throw new ArgumentException("logSource cannot be omitted unless the caller is registered to ModLib.", nameof(logSource))).Plugin.Name) + ".log");

        if (File.Exists(PathToLogFile))
        {
            Extras.WrapAction(() => File.WriteAllText(PathToLogFile, ""), Core.Logger);
        }
    }

    /// <summary>
    ///     Creates a new default logger with the specified log name and default logging filters.
    /// </summary>
    /// <param name="logName">The name of the logging file. If a rooted path is specified, it is instead used as the direct path to the log file.</param>
    public FallbackLogger(string logName)
        : this(logName, DefaultLogLevels)
    {
    }

    /// <summary>
    ///     Creates a new default logger with the given log source and logging filters.
    /// </summary>
    /// <param name="logName">The name of the logging file. If a rooted path is specified, it is instead used as the direct path to the log file.</param>
    /// <param name="allowLevels">The logging level(s) that will be processed for logging. Log requests whose log level is not specified here are ignored.</param>
    public FallbackLogger(string logName, LogLevel allowLevels)
        : base(allowLevels)
    {
        PathToLogFile = Path.IsPathRooted(logName)
            ? logName
            : Path.Combine(Registry.DefaultLogsPath, Registry.SanitizeModName(logName) + ".log");

        if (File.Exists(PathToLogFile))
        {
            Extras.WrapAction(() => File.WriteAllText(PathToLogFile, ""), Core.Logger);
        }
    }

    /// <inheritdoc/>
    public override object? GetLogSource() => LogSource;

    /// <inheritdoc/>
    protected override void LogImplementation(LogLevel level, object data)
    {
        LogSource?.Log(level, data);

        WriteToFile(PathToLogFile, FormatMessage(data, level));

        if (Core.Initialized)
            WriteToUnity(data, level);
    }

    private static string FormatMessage(object message, LogLevel category) => $"{DateTime.Now:T} [{category}]: {message}{Environment.NewLine}";

    private static void WriteToFile(string path, string contents) => File.AppendAllText(path, contents);

    private static void WriteToUnity(object data, LogLevel category)
    {
        switch (category)
        {
            case LogLevel.Fatal:
            case LogLevel.Error:
                if (data is Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
                else
                {
                    UnityEngine.Debug.LogError(data);
                }
                break;
            case LogLevel.Warning:
                UnityEngine.Debug.LogWarning(data);
                break;
            case LogLevel.Message:
                UnityEngine.Debug.LogAssertion(data);
                break;
            case LogLevel.None:
            case LogLevel.Info:
            case LogLevel.Debug:
            case LogLevel.All:
            default:
                UnityEngine.Debug.Log(data);
                break;
        }
    }
}