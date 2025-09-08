using BepInEx;
using Martyr.Slugcat.Features;
using Martyr.Utils;

namespace Martyr;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class MartyrMain : BaseUnityPlugin
{
    public const string MOD_GUID = "ynhzrfxn.martyr";
    public const string MOD_NAME = "The Martyr";
    public const string MOD_VERSION = "0.1.0";

    public static SlugcatStats.Name Martyr = new("Martyr");

    private static MyOptions? options;

    private bool isModEnabled;


    public MartyrMain()
        : base()
    {
        MyLogger.CleanLogFile();

        options = new();
    }

    public void OnEnable()
    {
        if (isModEnabled) return;
        isModEnabled = true;

        CompatibilityManager.CheckModCompats();

        MyExtras.WrapAction(() =>
        {
            ApplyAllHooks();

            MyLogger.LogDebug("Successfully registered hooks to the game.");
        });

        InputHandler.Keys.InitKeybinds();

        Logger.LogInfo($"Enabled {MOD_NAME} successfully.");
    }

    public void OnDisable()
    {
        if (!isModEnabled) return;
        isModEnabled = !isModEnabled;

        MyExtras.WrapAction(() =>
        {
            RemoveAllHooks();

            MyLogger.LogDebug("Removed all hooks successfully.");
        });

        Logger.LogInfo($"Disabled {MOD_NAME} successfully.");
    }


    // Load any resources, such as sprites or sounds
    private static void LoadResources() =>
        MachineConnector.SetRegisteredOI(MOD_GUID, options);


    private static void ApplyAllHooks()
    {
        On.RainWorld.OnModsInit += OnModsInitHook;

        On.GameSession.ctor += MyExtras.GameSessionHook;
        On.GameSession.AddPlayer += MyExtras.AddPlayerHook;

        FeatureManager.ApplyFeatures();

        MyExtras.WrapAction(Slugcat.SaintMechanicsHooks.ApplyHooks);
    }

    private static void RemoveAllHooks()
    {
        On.RainWorld.OnModsInit -= OnModsInitHook;

        On.GameSession.ctor -= MyExtras.GameSessionHook;
        On.GameSession.AddPlayer -= MyExtras.AddPlayerHook;

        FeatureManager.RemoveFeatures();

        MyExtras.WrapAction(Slugcat.SaintMechanicsHooks.RemoveHooks);
    }

    private static void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        MyExtras.WrapAction(LoadResources);
    }
}