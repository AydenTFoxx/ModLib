using BepInEx;
using MyMod.Slugcat;
using MyMod.Utils;

namespace MyMod;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class Main : BaseUnityPlugin
{
    public const string MOD_GUID = "example.mymod";
    public const string MOD_NAME = "Example Mod";
    public const string MOD_VERSION = "0.1.0";

    private static Options? options;

    private bool isModEnabled;


    public Main()
        : base()
    {
        MyMod.Logger.CleanLogFile();

        options = new();
    }

    public void OnEnable()
    {
        if (isModEnabled) return;
        isModEnabled = true;

        CompatibilityManager.CheckModCompats();

        Extras.WrapAction(() =>
        {
            ApplyAllHooks();

            MyMod.Logger.LogDebug("Successfully registered hooks to the game.");
        });

        InputHandler.Keys.InitKeybinds();

        Logger.LogInfo($"Enabled {MOD_NAME} successfully.");
    }

    public void OnDisable()
    {
        if (!isModEnabled) return;
        isModEnabled = false;

        Extras.WrapAction(() =>
        {
            RemoveAllHooks();

            MyMod.Logger.LogDebug("Removed all hooks successfully.");
        });

        Logger.LogInfo($"Disabled {MOD_NAME} successfully.");
    }


    // Load any resources, such as sprites or sounds
    private static void LoadResources() =>
        MachineConnector.SetRegisteredOI(MOD_GUID, options);


    private static void ApplyAllHooks()
    {
        On.RainWorld.OnModsInit += OnModsInitHook;

        On.GameSession.ctor += Extras.GameSessionHook;
        On.GameSession.AddPlayer += Extras.AddPlayerHook;

        PlayerHooks.AddHooks();
    }

    private static void RemoveAllHooks()
    {
        On.RainWorld.OnModsInit -= OnModsInitHook;

        On.GameSession.ctor -= Extras.GameSessionHook;
        On.GameSession.AddPlayer -= Extras.AddPlayerHook;

        PlayerHooks.RemoveHooks();
    }

    private static void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        Extras.WrapAction(LoadResources);
    }
}