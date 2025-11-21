using System;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using ModLib.Loader;
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
    public static bool IsMeadowEnabled { get; internal set; }

    /// <summary>
    ///     Whether or not the Improved Input Config: Extended mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsIICEnabled { get; internal set; }

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
    public static bool InGameSession { get; internal set; }

    /// <summary>
    ///     Determines if LogUtils is currently loaded and available for usage.
    /// </summary>
    public static bool LogUtilsAvailable { get; internal set; }

    /// <summary>
    ///     Determines if ModLib is currently loaded and available for usage.
    /// </summary>
    public static bool ModLibAvailable => Entrypoint.IsInitialized;

    static Extras()
    {
        Entrypoint.TryInitialize();
    }

    internal static void Initialize()
    {
        LogUtilsAvailable = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains("LogUtils"));

        IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        IsIICEnabled = CompatibilityManager.IsIICEnabled();
    }

    /// <summary>
    ///     Wraps a given action in a try-catch, safely performing its code while handling potential exceptions.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <param name="autoInvoke">If true, the resulting action is invoked immediately, and <c>null</c> is returned instead.</param>
    /// <returns>The wrapped <see cref="Action"/> object, or <c>null</c> if <paramref name="autoInvoke"/> was set to true.</returns>
    public static Action? WrapAction(Action action, bool autoInvoke = true)
    {
        ModLogger? logger = Registry.TryGetMod(Assembly.GetCallingAssembly())?.Logger ?? Core.Logger;

        if (autoInvoke)
        {
            WrappedResult();
            return null;
        }
        else
        {
            return WrappedResult;
        }

        void WrappedResult()
        {
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
    }

    /// <summary>
    ///     Wraps a given IL hook in a try-catch, preventing it from breaking other code when applied.
    /// </summary>
    /// <param name="action">The hook method to be wrapped.</param>
    /// <returns>An <c>ILContext.Manipulator</c> instance to be passed in place of the method itself.</returns>
    /// <remarks>Usage of this method is akin to the original <c>WrapInit</c> method; See SlugTemplate for an example of this.</remarks>
    public static ILContext.Manipulator WrapILHook(Action<ILContext> action)
    {
        ModLogger? logger = Registry.TryGetMod(Assembly.GetCallingAssembly())?.Logger ?? Core.Logger;

        return (context) =>
        {
            try
            {
                action.Invoke(context);
            }
            catch (Exception ex)
            {
                if (logger is null) return;

                logger.LogError($"Failed to apply IL hook: {action.Method.Name}");
                logger.LogError(ex);
            }
        };
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

    internal static ILContext.Manipulator WrapILHook(Action<ILContext> action, ModLogger? logger)
    {
        logger ??= Core.Logger;

        return (context) =>
        {
            try
            {
                action.Invoke(context);
            }
            catch (Exception ex)
            {
                if (logger is null) return;

                logger.LogError($"Failed to apply IL hook: {action.Method.Name}");
                logger.LogError(ex);
            }
        };
    }
}