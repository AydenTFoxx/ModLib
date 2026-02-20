using System.Reflection;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Rain Meadow-specific hooks and events, which are only enabled when the mod itself is present.
/// </summary>
internal static class MeadowHooks
{
    private static ILHook[]? manualHooks;

    /// <summary>
    ///     Applies all Rain Meadow-specific hooks to the game.
    /// </summary>
    public static void AddHooks()
    {
        On.GameSession.ctor += GameSessionHook;
        On.RainWorldGame.ExitGame += ExitGameHook;

        manualHooks = [
            new ILHook(typeof(Lobby).GetMethod("NewParticipantImpl", BindingFlags.NonPublic | BindingFlags.Instance), OnPlayerJoinedILHook)
        ];

        for (int i = 0; i < manualHooks.Length; i++)
        {
            manualHooks[i].Apply();
        }
    }

    /// <summary>
    ///     Removes all Rain Meadow-specific hooks from the game.
    /// </summary>
    public static void RemoveHooks()
    {
        On.GameSession.ctor -= GameSessionHook;
        On.RainWorldGame.ExitGame -= ExitGameHook;

        if (manualHooks is not null)
        {
            for (int i = 0; i < manualHooks.Length; i++)
            {
                manualHooks[i].Undo();
            }
            manualHooks = null;
        }
    }

    private static void OnPlayerJoinedILHook(ILContext context) => new ILCursor(context).Emit(OpCodes.Ldarg_1).EmitDelegate(MeadowUtils.OnPlayerJoinedLobby);

    private static void ExitGameHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
    {
        orig.Invoke(self, asDeath, asQuit);

        Extras.GameSession = null;

        ModRPCManager.ClearRPCs();

        OptionUtils.SharedOptions.RefreshOptions();
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        OptionUtils.SharedOptions.RefreshOptions(Extras.InGameSession);

        if (!Extras.InGameSession)
            MeadowUtils.OnJoinedGameSession(self);

        Extras.GameSession = self;

        if (!MeadowUtils.IsHost)
        {
            OnlineManager.lobby.owner.SendRPCEvent(ModRPCs.RequestSyncRemixOptions, OnlineManager.mePlayer);
        }
    }
}