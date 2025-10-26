using System.IO;
using System.Linq;
using System.Text;
using LogUtils;
using LogUtils.Enums;
using LogUtils.Properties;

namespace ModLib.Logging;

internal static class LogUtilsHelper
{
    public static object MyLogID = CreateLogID(Core.MOD_NAME, register: true);

    public static bool IsAvailable => UtilityCore.IsInitialized;

    static LogUtilsHelper()
    {
        UtilityCore.EnsureInitializedState();

        if (!IsAvailable) return;

        if (!Directory.Exists(Core.LogsPath))
        {
            Directory.CreateDirectory(Core.LogsPath);
        }

        LogProperties? properties = (MyLogID as LogID)?.Properties;

        if (properties is not null && !properties.ReadOnly)
        {
            properties.AltFilename = new LogFilename("ynhzrfxn.modlib", ".log");

            properties.IntroMessage = "# Initialized ModLib successfully.";
            properties.OutroMessage = "# Disabled ModLib successfully.";
        }
    }

    public static LogID CreateLogID(string name, bool register = false)
    {
        LogID logID = new(SanitizeName(name), Core.LogsPath, LogAccess.FullAccess, register);

        logID.Properties.ShowCategories.IsEnabled = true;
        logID.Properties.ShowLogTimestamp.IsEnabled = true;

        logID.Properties.AddTag("ModLib");

        return logID;
    }

    public static string SanitizeName(string modName)
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

    internal static void InitLogID(this Registry.ModEntry self, bool createLogID)
    {
        if (self.LogID is not null
            || self.Logger is not LogUtilsAdapter adapter
            || adapter.GetLogSource() is not Logger logger)
        {
            return;
        }

        self.LogID = createLogID
            ? CreateLogID(self.Plugin.Name, register: false)
            : logger.LogTargets.FirstOrDefault(id => !id.IsGameControlled);
    }
}