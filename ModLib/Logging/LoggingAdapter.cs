using System;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;

namespace ModLib.Logging;

// Credits to Fluffball (@TheVileOne) for the templates at LogUtils.Templates :P

/// <summary>
///     Intermediary helper for creating loggers with the appropriate logging backend.
/// </summary>
public static class LoggingAdapter
{
    static LoggingAdapter()
    {
        Extras.WrapAction(static () =>
        {
            if (!Directory.Exists(Core.LogsPath))
            {
                Directory.CreateDirectory(Core.LogsPath);
            }
        }, Core.Logger);
    }

    /// <summary>
    ///     Creates a logger instance employing a safe encapsulation technique.
    /// </summary>
    /// <param name="logSource">The BepInEx source to use for identification and logging.</param>
    /// <param name="wrapLogger">
    ///     If true, the generated logger will be a <see cref="FilteredLogWrapper"/>,
    ///     which ignores certain logs based on their level and the current type of ModLib's build.
    /// </param>
    public static ModLogger CreateLogger(ManualLogSource logSource, bool wrapLogger = false)
    {
        ModLogger result = Extras.LogUtilsAvailable
            ? CreateLogUtilsLogger(logSource)
            : new FallbackLogger(logSource);

        if (wrapLogger)
            result = new FilteredLogWrapper(result);

        return result;

        static ModLogger CreateLogUtilsLogger(ManualLogSource logSource)
        {
            try
            {
                return LogUtilsHelper.CreateLogger(logSource);
            }
            catch (Exception ex)
            {
                Core.LogSource?.LogError($"Failed to create logger instance: {ex}");

                return new FallbackLogger(logSource);
            }
        }
    }

    internal static string SanitizeName(string modName)
    {
        StringBuilder stringBuilder = new();
        char[] forbiddenChars = [.. Path.GetInvalidPathChars(), ' '];

        foreach (char c in modName)
        {
            if (forbiddenChars.Contains(c)) continue;

            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }
}