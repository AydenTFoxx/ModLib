using System.Linq;
using BepInEx.Logging;
using LogUtils;
using LogUtils.Enums;
using LogUtils.Properties;

namespace ModLib.Logging;

internal static class LogUtilsHelper
{
    public static object MyLogID;

    static LogUtilsHelper()
    {
        MyLogID = CreateLogID(Core.MOD_NAME, register: true);

        LogProperties properties = ((LogID)MyLogID).Properties;

        if (!properties.ReadOnly)
        {
            properties.AltFilename = new LogFilename("ynhzrfxn.modlib", ".log");

            properties.IntroMessage = "# Initialized ModLib successfully.";
            properties.OutroMessage = "# Disabled ModLib successfully.";
        }
    }

    public static ModLogger CreateLogger(ILogSource logSource)
    {
        CompositeLogTarget logTargets = logSource.SourceName == Core.MOD_NAME
            ? (LogID)MyLogID | LogID.BepInEx
            : CreateLogID(logSource.SourceName, register: false) | LogID.BepInEx | LogID.Unity;

        return new LogUtilsLogger(
            new LogUtils.Logger(logTargets)
            {
                LogSource = logSource
            }
        );
    }

    public static LogID CreateLogID(string name, bool register = false)
    {
        LogID logID = new(Registry.SanitizeModName(name), Core.LogsPath, LogAccess.FullAccess, register);

        logID.Properties.ShowCategories.IsEnabled = true;
        logID.Properties.ShowLogTimestamp.IsEnabled = true;

        logID.Properties.AddTag("ModLib");

        return logID;
    }

    internal static void InitLogID(this Registry.ModEntry self)
    {
        if (self.LogID is not null
            || self.Logger is not LogUtilsLogger adapter
            || adapter.GetLogSource() is not LogUtils.Logger logger)
        {
            return;
        }

        self.LogID = self.Plugin == Core.PluginData
            ? MyLogID
            : adapter.ModLibCreated
                ? CreateLogID(self.Plugin.Name, register: false)
                : logger.LogTargets.FirstOrDefault(static id => !id.IsGameControlled);
    }
}