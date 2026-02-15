using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using FakeAchievements;
using ModLib.Loader;
using ModLib.Logging;
using ModLib.Meadow;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace ModLib;

/// <summary>
///     A collection of utilities and wrappers for common modding activities.
/// </summary>
public static class Extras
{
    /// <summary>
    ///     The current game session of the player, if any.
    /// </summary>
    public static GameSession? GameSession { get; internal set; }

    /// <summary>
    ///     Whether or not the Rain Meadow mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsMeadowEnabled { get; internal set; }

    /// <summary>
    ///     Whether or not the Improved Input Config: Extended mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsIICEnabled { get; internal set; }

    /// <summary>
    ///     Whether or not the Fake Achievements mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsFakeAchievementsEnabled { get; internal set; }

    /// <summary>
    ///     If the current game session is in an online lobby.
    /// </summary>
    public static bool IsOnlineSession => IsMeadowEnabled && MeadowUtils.IsOnline;

    /// <summary>
    ///     If the current game session is in a multiplayer context (online or local).
    /// </summary>
    /// <remarks>
    ///     To determine if the given session is online or not, use <see cref="IsOnlineSession"/>.
    /// </remarks>
    public static bool IsMultiplayer => (ModManager.JollyCoop && InGameSession) || IsOnlineSession;

    /// <summary>
    ///     If the player is the host of the current game session. On Singleplayer, this is always true.
    /// </summary>
    public static bool IsHostPlayer => !IsMeadowEnabled || MeadowUtils.IsHost;

    /// <summary>
    ///     If the player is currently in-game and not on the main menu.
    /// </summary>
    public static bool InGameSession => GameSession is not null;

    /// <summary>
    ///     Determines if LogUtils is currently loaded and available for usage.
    /// </summary>
    public static bool LogUtilsAvailable { get; internal set; }

    /// <summary>
    ///     Determines if ModLib is currently loaded and available for usage.
    /// </summary>
    public static bool ModLibAvailable => Entrypoint.IsInitialized;

    /// <summary>
    ///     Determines if Dev Tools is currently enabled, or was enabled last time ModLib was active.
    ///     Usable even before ModManager is available, but might be innacurate on the first time the mod is run.
    /// </summary>
    public static bool DebugMode { get; internal set; }

    internal static bool RainReloaderActive => ModManager.ActiveMods.Any(static mod => mod.id == "twofour2.rainReloader");

    static Extras()
    {
        Entrypoint.TryInitialize();
    }

    internal static void Initialize()
    {
        LogUtilsAvailable = AppDomain.CurrentDomain.GetAssemblies().Any(static a => a.FullName.Contains("LogUtils"));

        IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        IsIICEnabled = CompatibilityManager.IsIICEnabled();
        IsFakeAchievementsEnabled = CompatibilityManager.IsFakeAchievementsEnabled();
    }

    /// <summary>
    ///     Grants and displays a given custom achievement to the client.
    ///     If the Fake Achievements mod is not present, this method does nothing.
    /// </summary>
    /// <param name="achievementID">The resolvable identifier of the achievement.</param>
    public static void GrantFakeAchievement(string achievementID)
    {
        if (!IsFakeAchievementsEnabled) return;

        string pluginGuid = Registry.GetMod(Assembly.GetCallingAssembly()).Plugin.GUID;

        if (!achievementID.StartsWith($"{pluginGuid}/"))
            achievementID = $"{pluginGuid}/${achievementID}";

        Core.Logger.LogDebug($"Displaying achievement: {achievementID}");

        try
        {
            FakeAchievementsAccess.GrantAchievement(achievementID);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to grant achievement [{achievementID}]!");
            Core.Logger.LogError($"Exception: {ex}");
        }
    }

    /// <summary>
    ///     Revokes a given custom achievement for the client.
    ///     If the Fake Achievements mod is not present, this method does nothing.
    /// </summary>
    /// <param name="achievementID">The resolvable identifier of the achievement.</param>
    public static void RevokeFakeAchievement(string achievementID)
    {
        if (!IsFakeAchievementsEnabled) return;

        string pluginGuid = Registry.GetMod(Assembly.GetCallingAssembly()).Plugin.GUID;

        if (!achievementID.StartsWith($"{pluginGuid}/"))
            achievementID = $"{pluginGuid}/${achievementID}";

        Core.Logger.LogDebug($"Revoking achievement: {achievementID}");

        try
        {
            FakeAchievementsAccess.RevokeAchievement(achievementID);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to revoke achievement [{achievementID}]!");
            Core.Logger.LogError($"Exception: {ex}");
        }
    }

    /// <summary>
    ///     Safely determines if the given object is owned by the player in an online context.
    /// </summary>
    /// <param name="physicalObject">The object for testing.</param>
    /// <returns>
    ///     <c>true</c> if the given object is owned by this client, <c>false</c> otherwise.
    ///     This method returns <c>true</c> if Rain Meadow is not enabled, or if the player is not in an online lobby.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLocalObject(PhysicalObject physicalObject) => !IsMeadowEnabled || MeadowUtils.IsMine(physicalObject);

    /// <summary>
    ///     Wraps a given action in a try-catch, safely performing its code while handling potential exceptions.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    public static void WrapAction(Action action)
    {
        ModLogger? logger = Registry.TryGetMod(Assembly.GetCallingAssembly())?.Logger ?? Core.Logger;

        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            if (logger is null) return;

            logger.LogError($"Failed to run wrapped action: {action.Method.Name}");
            logger.LogError(ex);
        }
    }

    internal static void WrapAction(Action action, ModLogger? logger)
    {
        logger ??= Core.Logger;

        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            if (logger is null) return;

            logger.LogError($"Failed to run wrapped action: {action.Method.Name}");
            logger.LogError(ex);
        }
    }

    private static class FakeAchievementsAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GrantAchievement(string achievementID) => AchievementsManager.GrantAchievement(achievementID);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RevokeAchievement(string achievementID) => AchievementsManager.RevokeAchievement(achievementID);
    }
}