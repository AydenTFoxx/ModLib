using BepInEx.Logging;
using LogUtils;
using LogUtils.Enums;
using System;

namespace ModLib.Logging;

/// <summary>
///
/// </summary>
public static class LoggingAdapter
{
    private static class LogUtilsAccess
    {
        /// <summary>
        /// Attempt to initialize LogUtils assembly
        /// </summary>
        /// <exception cref="TypeLoadException">An assembly dependency is unavailable, or is of the wrong version</exception>
        internal static void UnsafeAccess() => UtilityCore.EnsureInitializedState();

        internal static IMyLogger CreateLogger(ManualLogSource logSource)
        {
            UnsafeAccess();

            //These represent the log files you want to target for logging
            CompositeLogTarget myLogTargets = LogID.BepInEx | LogID.Unity;

            LogUtilsAdapter adapter = new(
                new LogUtils.Logger(myLogTargets) { LogSource = logSource }
            );
            return adapter;
        }
    }

    /// <summary>
    ///     Creates a logger instance employing a safe encapsulation technique.
    /// </summary>
    public static IMyLogger CreateLogger(ManualLogSource logSource)
    {
        try
        {
            return LogUtilsAccess.CreateLogger(logSource);
        }
        catch //Caught exception will probably be a TypeLoadException
        {
            return new FallbackLogger(logSource);
        }
    }
}