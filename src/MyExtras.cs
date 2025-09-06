using System;
using System.Security.Permissions;
using Martyr.Possession;
using Martyr.Utils;

/*
 * This file contains fixes to some common problems when modding Rain World.
 * Unless you know what you're doing, you shouldn't modify anything here.
 */

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace Martyr;

internal static class MyExtras
{
    private static bool _initialized;

    // Ensure resources are only loaded once and that failing to load them will not break other mods
    public static On.RainWorld.hook_OnModsInit WrapInit(Action<RainWorld> loadResources)
    {
        return (orig, self) =>
        {
            orig(self);

            try
            {
                if (!_initialized)
                {
                    _initialized = true;
                    loadResources(self);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        };
    }


    public static void AddPlayerHook(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
    {
        orig.Invoke(self, player);

        bool isOnlineSession = CompatibilityManager.IsRainMeadowEnabled() && MeadowUtils.IsOnline;

        if (self.game.Players.Count <= 1 && (!isOnlineSession || MeadowUtils.IsHost))
        {
            OptionUtils.SharedOptions.RefreshOptions(isOnlineSession);
        }
        else
        {
            MyLogger.LogDebug($"{self.game.FirstAnyPlayer} is already present, ignoring.");
        }
    }

    public static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost)
        {
            OptionUtils.SharedOptions.SetOptions(null);

            MeadowUtils.RequestOptionsSync();
        }
    }
}