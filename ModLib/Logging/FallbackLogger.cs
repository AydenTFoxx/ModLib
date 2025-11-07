using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ModLib.Logging;

/// <summary>
///     Default wrapper for a <see cref="ManualLogSource"/>; Used when LogUtils is not avaliable at runtime.
/// </summary>
public class FallbackLogger(ManualLogSource logSource) : IMyLogger
{
    private readonly string PathToLogFile = Path.Combine(Registry.DefaultLogsPath, logSource.SourceName + ".log");

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(object message) => Log(LogLevel.Debug, message);

    /// <inheritdoc/>
    public void Log(LogLevel category, object message)
    {
        WriteToFile(FormatMessage(message, category));

        logSource.Log(category, message);

        if (Core.Initialized)
            WriteToUnity(message, category);
    }

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
    public object GetLogSource() => logSource;

    private string FormatMessage(object message, LogLevel category) => $"{DateTime.Now:t} [{category}]: {message}{Environment.NewLine}";

    private void WriteToFile(string contents) => File.WriteAllText(PathToLogFile, contents);

    private void WriteToUnity(object data, LogLevel category)
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
            case LogLevel.Info:
            case LogLevel.Debug:
            case LogLevel.All:
            case LogLevel.None:
            default:
                UnityEngine.Debug.Log(data);
                break;
        }
    }
}