using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using ModLib.Loader;
using ModLib.Logging;
using ModLib.Objects.Meadow;
using ModLib.Storage;
using Watcher;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ModLib.Objects;

internal class Main : IExtensionEntrypoint
{
    private static readonly BepInPlugin PluginData = new("ynhzrfxn.modlib-objects", "ModLib.Objects", "0.5.0.0");
    private static ManualLogSource LogSource = BepInEx.Logging.Logger.CreateLogSource("ModLib.Objects");

    private static bool _calledPostModsInit;

    internal static ModLogger Logger { get; private set; } = new FallbackLogger(LogSource);
    internal static ModData ModData { get; }

    static Main()
    {
        Registry.RegisterMod(PluginData, null, Logger);

        ModData = new(autoSave: false);
    }

    public BepInPlugin Metadata => PluginData;

    public void OnEnable()
    {
        if (Extras.LogUtilsAvailable)
            Logger = LoggingAdapter.CreateLogger(LogSource);

        GlobalUpdatableAndDeletable.Hooks.Apply();
        DeathProtection.Hooks.Apply();

        On.Watcher.LizardBlizzardModule.IsForbiddenToPull += ForbidPullingMarkedTypesHook;

        On.RainWorld.PostModsInit += PostModsInitHook;

        Logger.LogDebug("Successfully enabled Objects expansion for ModLib.");
    }

    public void OnDisable()
    {
        if (Extras.RainReloaderActive)
        {
            GlobalUpdatableAndDeletable.Hooks.Remove();
            DeathProtection.Hooks.Remove();

            if (Extras.IsMeadowEnabled)
                MeadowProtectionHooks.RemoveHooks();

            On.Watcher.LizardBlizzardModule.IsForbiddenToPull -= ForbidPullingMarkedTypesHook;

            On.RainWorld.PostModsInit -= PostModsInitHook;
        }

        Logger.LogDebug("Disabled Objects expansion for ModLib.");
        Logger = null!;

        BepInEx.Logging.Logger.Sources.Remove(LogSource);

        LogSource = null!;
    }

    private static void PostModsInitHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        if (!_calledPostModsInit)
        {
            _calledPostModsInit = true;

            if (Extras.IsMeadowEnabled)
                MeadowProtectionHooks.ApplyHooks();

            if (CompatibilityManager.IsModEnabled(CompatibilityManager.DEV_CONSOLE_ID))
                ModDebuggerExtension.RegisterCommands();
        }
    }

    /// <summary>
    ///     Prevents Blizzard Lizards' blizzard shield from pushing around objects inheriting the <see cref="IForbiddenToPull"/> interface.
    /// </summary>
    private static bool ForbidPullingMarkedTypesHook(On.Watcher.LizardBlizzardModule.orig_IsForbiddenToPull orig, LizardBlizzardModule self, UpdatableAndDeletable uad) =>
        uad is not IForbiddenToPull && orig.Invoke(self, uad); // I can make this an IL hook
}