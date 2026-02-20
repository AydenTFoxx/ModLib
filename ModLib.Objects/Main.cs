using System.Linq;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using ModLib.Loader;
using ModLib.Logging;
using ModLib.Meadow;
using ModLib.Objects.Meadow;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RainMeadow;
using Watcher;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ModLib.Objects;

internal class Main : IExtensionEntrypoint
{
    private static ManualLogSource LogSource = new("ModLib.Objects");

    internal static ModLogger Logger { get; private set; } = new FallbackLogger(LogSource);

    public BepInPlugin Metadata { get; } = new("ynhzrfxn.modlib-objects", "ModLib.Objects", "0.4.0.0");

    public void OnEnable()
    {
        Logger = LoggingAdapter.CreateLogger(LogSource);

        DeathProtectionHooks.ApplyHooks();

        if (Extras.IsMeadowEnabled)
        {
            MeadowProtectionHooks.ApplyHooks();

            MeadowUtils.PlayerJoinedLobby += RainMeadowAccess.SyncDeathProtections;
        }

        IL.RainWorldGame.Update += UpdateGUADsILHook;

        On.RainWorldGame.ExitGame += ClearGUADsOnExitHook;

        On.Watcher.LizardBlizzardModule.IsForbiddenToPull += ForbidPullingMarkedTypesHook;

        Logger.LogDebug("Successfully enabled Objects expansion for ModLib.");
    }

    public void OnDisable()
    {
        if (Extras.RainReloaderActive)
        {
            DeathProtectionHooks.RemoveHooks();

            if (Extras.IsMeadowEnabled)
            {
                MeadowProtectionHooks.RemoveHooks();

                MeadowUtils.PlayerJoinedLobby -= RainMeadowAccess.SyncDeathProtections;
            }

            IL.RainWorldGame.Update -= UpdateGUADsILHook;

            On.RainWorldGame.ExitGame -= ClearGUADsOnExitHook;

            On.Watcher.LizardBlizzardModule.IsForbiddenToPull -= ForbidPullingMarkedTypesHook;
        }

        Logger.LogDebug("Disabled Objects expansion for ModLib.");
        Logger = null!;

        BepInEx.Logging.Logger.Sources.Remove(LogSource);

        LogSource = null!;
    }

    private static void ClearGUADsOnExitHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
    {
        orig.Invoke(self, asDeath, asQuit);

        GlobalUpdatableAndDeletable.Instances.Clear();
    }

    private static void UpdateGUADsILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(MoveType.After, static x => x.MatchCallvirt(typeof(PathfinderResourceDivider).GetMethod(nameof(PathfinderResourceDivider.Update))))
         .MoveAfterLabels()
         .Emit(OpCodes.Ldarg_0)
         .EmitDelegate(UpdateGUADs);

        static void UpdateGUADs(RainWorldGame self)
        {
            foreach (GlobalUpdatableAndDeletable guad in GlobalUpdatableAndDeletable.Instances)
            {
                if (self.GamePaused)
                {
                    guad.PausedUpdate();
                }
                else
                {
                    guad.Update(self.evenUpdate);
                }
            }
        }
    }

    /// <summary>
    ///     Prevents Blizzard Lizards' blizzard shield from pushing around objects inheriting the <see cref="IForbiddenToPull"/> interface.
    /// </summary>
    private static bool ForbidPullingMarkedTypesHook(On.Watcher.LizardBlizzardModule.orig_IsForbiddenToPull orig, LizardBlizzardModule self, UpdatableAndDeletable uad) =>
        uad is not IForbiddenToPull && orig.Invoke(self, uad); // I can make this an IL hook

    private static class RainMeadowAccess
    {
        public static void SyncDeathProtections(OnlinePlayer player)
        {
            if (!OnlineManager.lobby.isOwner) return;

            player.SendRPCEvent(MyRPCs.SyncDeathProtections, new SerializableDictionary<OnlineCreature, OnlineDeathProtection>(DeathProtection.GetInstances().Select(MyRPCs.LocalToOnlineProtection).ToDictionary()));
        }
    }
}