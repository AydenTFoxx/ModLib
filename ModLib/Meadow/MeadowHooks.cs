using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Rain Meadow-specific hooks and events, which are only enabled when the mod itself is present.
/// </summary>
internal static class MeadowHooks
{
    /// <summary>
    ///     Applies all Rain Meadow-specific hooks to the game.
    /// </summary>
    public static void AddHooks()
    {
        On.GameSession.ctor += GameSessionHook;
        On.RainWorldGame.ExitGame += ExitGameHook;
    }

    /// <summary>
    ///     Removes all Rain Meadow-specific hooks from the game.
    /// </summary>
    public static void RemoveHooks()
    {
        On.GameSession.ctor -= GameSessionHook;
        On.RainWorldGame.ExitGame -= ExitGameHook;
    }

    private static void ExitGameHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
    {
        orig.Invoke(self, asDeath, asQuit);

        Extras.InGameSession = false;

        ModRPCManager.ClearRPCs();

        OptionUtils.SharedOptions.RefreshOptions();
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        OptionUtils.SharedOptions.RefreshOptions(Extras.InGameSession);

        if (!Extras.InGameSession)
            MeadowUtils.OnJoinedGameSession(self);

        Extras.InGameSession = true;

        if (!MeadowUtils.IsHost)
        {
            OnlineManager.lobby.owner.SendRPCEvent(ModRPCs.RequestSyncRemixOptions, OnlineManager.mePlayer);
        }
    }
}