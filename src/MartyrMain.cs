using BepInEx;
using Martyr.Utils;

namespace Martyr;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class MartyrMain : BaseUnityPlugin
{
    public const string MOD_GUID = "ynhzrfxn.martyr";
    public const string MOD_NAME = "The Martyr";
    public const string MOD_VERSION = "0.1.0";

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

        ApplyAllHooks();

        InputHandler.Keys.InitKeybinds();

        Logger.LogInfo($"Enabled {MOD_NAME} successfully.");
    }

    public void OnDisable()
    {
        if (!isModEnabled) return;
        isModEnabled = !isModEnabled;

        RemoveAllHooks();

        Logger.LogInfo($"Disabled {MOD_NAME} successfully.");
    }


    // Load any resources, such as sprites or sounds
    private static void LoadResources(RainWorld _) =>
        MachineConnector.SetRegisteredOI(MOD_GUID, options);


    private static void ApplyAllHooks()
    {
        On.RainWorld.OnModsInit += MyExtras.WrapInit(LoadResources);

        On.GameSession.ctor += MyExtras.GameSessionHook;
        On.GameSession.AddPlayer += MyExtras.AddPlayerHook;

        Slugcat.Features.Feature.ApplyFeatures();
    }

    private static void RemoveAllHooks()
    {
        On.RainWorld.OnModsInit -= MyExtras.WrapInit(LoadResources);

        On.GameSession.ctor -= MyExtras.GameSessionHook;
        On.GameSession.AddPlayer -= MyExtras.AddPlayerHook;

        Slugcat.Features.Feature.RemoveFeatures();
    }
}