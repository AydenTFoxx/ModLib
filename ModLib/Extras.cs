using System;
using System.Reflection;
using System.Security.Permissions;
using ModLib.Logging;
using ModLib.Meadow;
using MonoMod.Cil;

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
    ///     Whether or not the Rain Meadow mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsMeadowEnabled { get; }

    /// <summary>
    ///     Whether or not the Improved Input Config: Extended mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsIICEnabled { get; }

    /// <summary>
    ///     If the current game session is in an online lobby.
    /// </summary>
    public static bool IsOnlineSession => IsMeadowEnabled && MeadowUtils.IsOnline;

    /// <summary>
    ///     If the player is the host of the current game session. On Singleplayer, this is always true.
    /// </summary>
    public static bool IsHostPlayer => !IsMeadowEnabled || MeadowUtils.IsHost;

    /// <summary>
    ///     If the player is currently in-game and not on the main menu.
    /// </summary>
    public static bool InGameSession { get; internal set; }

    /// <summary>
    ///     Determines if LogUtils is currently loaded and available for usage.
    /// </summary>
    public static bool LogUtilsAvailable { get; }

    /// <summary>
    ///     Determines if ModLib is currently loaded and available for usage.
    ///     This property should always return <c>true</c>, and may be used by other mods to offer optional compatibility with ModLib itself when available.
    /// </summary>
    public static bool ModLibAvailable => Loader.Entrypoint.Initialized;

    static Extras()
    {
        IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        IsIICEnabled = CompatibilityManager.IsIICEnabled();

        try
        {
            LogUtilsAvailable = LogUtilsHelper.IsAvailable;
        }
        catch (Exception)
        {
            LogUtilsAvailable = false;
        }
    }

    /// <summary>
    ///     Wraps a given action in a try-catch, safely performing its code while handling potential exceptions.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>Use sparsely; If possible, avoid throwing an exception at all instead of using this method.</remarks>
    public static void WrapAction(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            IMyLogger? logger = Registry.GetMod(Assembly.GetCallingAssembly()).Logger;

            if (logger is null) return;

            logger.LogError($"Failed to run wrapped action: {action.Method.Name}");
            logger.LogError(ex);
        }
    }

    /// <summary>
    ///     Wraps a given IL hook in a try-catch, preventing it from breaking other code when applied.
    /// </summary>
    /// <param name="action">The hook method to be wrapped.</param>
    /// <returns>An <c>ILContext.Manipulator</c> instance to be passed in place of the method itself.</returns>
    /// <remarks>Usage of this method is akin to the original <c>WrapInit</c> method; See SlugTemplate for an example of this.</remarks>
    public static ILContext.Manipulator WrapILHook(Action<ILContext> action)
    {
        return (context) =>
        {
            try
            {
                action.Invoke(context);
            }
            catch (Exception ex)
            {
                IMyLogger? logger = Registry.GetMod(Assembly.GetCallingAssembly()).Logger;

                if (logger is null) return;

                logger.LogError($"Failed to apply IL hook: {action.Method.Name}");
                logger.LogError(ex);
            }
        };
    }

    internal static void WrapAction(Action action, IMyLogger logger)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to run wrapped action: {action.Method.Name}");
            logger.LogError(ex);
        }
    }
}