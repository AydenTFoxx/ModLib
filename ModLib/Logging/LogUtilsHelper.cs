using BepInEx.Logging;
using LogUtils;
using LogUtils.Enums;
using LogUtils.Properties;

namespace ModLib.Logging;

internal static class LogUtilsHelper
{
    internal static LogID MyLogID
    {
        get
        {
            if (field is null)
            {
                field = CreateLogID(Core.MOD_NAME, register: true);

                LogProperties properties = field.Properties;

                if (!properties.ReadOnly)
                {
                    properties.AltFilename = new LogFilename("ynhzrfxn.modlib", ".log");

                    properties.IntroMessage = "# Initialized ModLib successfully.";
                    properties.OutroMessage = "# Disabled ModLib successfully.";
                }
            }
            return field;
        }
    }

    public static LogID CreateLogID(string name, bool register = false)
    {
        LogID logID = new(Registry.SanitizeModName(name), Core.LogsPath, LogAccess.FullAccess, register);

        logID.Properties.ShowCategories.IsEnabled = true;
        logID.Properties.ShowLogTimestamp.IsEnabled = true;

        logID.Properties.AddTag("ModLib");

        return logID;
    }

    public static ModLogger CreateLogger(ILogSource logSource)
    {
        ILogTarget logTargets = logSource == Core.LogSource
            ? MyLogID | LogID.Unity
            : CreateLogID(logSource.SourceName, register: false) | LogID.BepInEx | LogID.Unity;

        return new LogUtilsLogger(
            new LogUtils.Logger(logTargets)
            {
                LogSource = logSource
            }
        );
    }
}