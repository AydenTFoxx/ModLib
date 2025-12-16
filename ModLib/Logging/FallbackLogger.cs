using System;
using System.IO;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     A logger type which logs to a custom file with basic formatting and timestamps.
/// </summary>
public class FallbackLogger(ManualLogSource logSource) : ModLogger
{
    private readonly string PathToLogFile = Path.Combine(Registry.DefaultLogsPath, logSource.SourceName + ".log");

    /// <inheritdoc/>
    public override object GetLogSource() => logSource;

    /// <inheritdoc/>
    public override void Log(LogLevel level, object data)
    {
        logSource.Log(level, data);

        WriteToFile(PathToLogFile, FormatMessage(data, level));

        if (Core.Initialized)
            WriteToUnity(data, level);
    }

    private static string FormatMessage(object message, LogLevel category) => $"{DateTime.Now:t} [{category}]: {message}{Environment.NewLine}";

    private static void WriteToFile(string path, string contents) => File.WriteAllText(path, contents);

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