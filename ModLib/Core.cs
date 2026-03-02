using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using ModLib.Debug;
using ModLib.Input;
using ModLib.Loader;
using ModLib.Logging;
using ModLib.Meadow;
using ModLib.Options;
using ModLib.Storage;
using UnityEngine;

namespace ModLib;

internal static class Core
{
    public const string MOD_GUID = "ynhzrfxn.modlib";
    public const string MOD_NAME = "ModLib";
    public const string MOD_VERSION = "0.5.0.0";

    public static readonly Assembly MyAssembly = typeof(Core).Assembly;

    public static readonly BepInPlugin PluginData = new(MOD_GUID, MOD_NAME, MOD_VERSION);
    public static readonly ManualLogSource LogSource = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

    public static readonly string StreamingAssetsPath = Path.Combine(Paths.GameRootPath, "RainWorld_Data", "StreamingAssets");

    public static readonly string LogsPath = Path.Combine(StreamingAssetsPath, "Logs");
    private static readonly string DataPath = Path.Combine(StreamingAssetsPath, "modlib.json");

    public static ModLogger Logger { get; private set; } = new FallbackLogger(LogSource);

    public static bool Initialized { get; private set; }

    internal static bool InputModuleActivated { get; set; }

    private static ModLibData MyData { get; set; }

    public static void Initialize()
    {
        if (Initialized) return;

        Initialized = true;

        try
        {
            ReadModLibData();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to retrieve ModLib data: {ex}");
        }

        SharedOptions.SetOption("modlib.debug", MyData.DevToolsActive);
        SharedOptions.SetOption("modlib.preview", false);

        Extras.DebugMode = SharedOptions.IsOptionEnabled("modlib.debug");

        if (Extras.LogUtilsAvailable)
        {
            LogSource.LogDebug("Switching Core logger to LogUtils!");

            Logger = LoggingAdapter.CreateLogger(LogSource, MyData.DevToolsActive ? ModLogger.DefaultLogLevels : ModLogger.NonDebugLevels);
        }
        else
        {
            LogSource.LogDebug("Using fallback logger for ModLib.");

            Logger ??= new FallbackLogger(LogSource, MyData.DevToolsActive ? ModLogger.DefaultLogLevels : ModLogger.NonDebugLevels);
        }

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }
        else
        {
            On.GameSession.ctor += GameSessionHook;
            On.RainWorldGame.ExitGame += ExitGameHook;
        }

        On.RainWorld.PostModsInit += PostModsInitHook;
        On.RainWorldGame.Update += GameUpdateHook;

        Application.quitting += Entrypoint.Disable;

        Registry.RegisterAssembly(MyAssembly, PluginData, null, Logger);
    }

    public static void Disable()
    {
        if (!Initialized) return;

        Initialized = false;

        if (Extras.RainReloaderActive)
        {
            CompatibilityManager.Clear();

            if (Extras.IsMeadowEnabled)
            {
                MeadowHooks.RemoveHooks();
            }
            else
            {
                On.GameSession.ctor -= GameSessionHook;
                On.RainWorldGame.ExitGame -= ExitGameHook;
            }

            On.RainWorld.PostModsInit -= PostModsInitHook;
            On.RainWorldGame.Update -= GameUpdateHook;

            Application.quitting -= Entrypoint.Disable;
        }

        try
        {
            WriteModLibData();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to write ModLib data: {ex}");
        }

        foreach (ModData saveData in ModData.StoredInstances.Where(static md => md.AutoSave))
        {
            saveData.SaveToFile();
        }
    }

    private static void ExitGameHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
    {
        orig.Invoke(self, asDeath, asQuit);

        Extras.GameSession = null;
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        SharedOptions.RefreshOptions(Extras.InGameSession);

        Extras.GameSession = self;
    }

    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        if (InputModuleActivated)
        {
            if (Extras.IsIICEnabled)
            {
                ImprovedInputHelper.UpdateInput();
            }
            else
            {
                foreach (Keybind keybind in Keybind.Keybinds)
                {
                    for (int i = 0; i < Keybind.MaxPlayers; i++)
                    {
                        keybind.Update(RWCustom.Custom.rainWorld?.options, i);
                    }
                }
            }
        }

        orig.Invoke(self);

        if (Extras.IsMeadowEnabled)
        {
            ModRPCManager.UpdateRPCs();
        }
    }

    private static void PostModsInitHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        MyData = new(ModManager.DevTools, MOD_VERSION);

        SharedOptions.RemoveOption("modlib.debug");
        SharedOptions.SetOption("modlib.debug", MyData.DevToolsActive);

        Extras.DebugMode = SharedOptions.IsOptionEnabled("modlib.debug");

        if (!Extras.IsIICEnabled)
            Extras.IsIICEnabled = CompatibilityManager.IsIICEnabled();

        if (!Extras.IsMeadowEnabled)
            Extras.IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();

        if (!Extras.IsFakeAchievementsEnabled)
            Extras.IsFakeAchievementsEnabled = CompatibilityManager.IsFakeAchievementsEnabled();

        if (CompatibilityManager.IsModEnabled(CompatibilityManager.DEV_CONSOLE_ID))
        {
            try
            {
                ModDebugger.RegisterCommands();
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Failed to initialize DevConsole commands! {ex}");
            }
        }
    }

    private static void ReadModLibData()
    {
        if (!File.Exists(DataPath))
        {
            MyData = new(false, MOD_VERSION);
            return;
        }

        string data = File.ReadAllText(Path.Combine(StreamingAssetsPath, "modlib.json"));

        if (!string.IsNullOrWhiteSpace(data))
        {
            DataContractJsonSerializer serializer = new(typeof(ModLibData));

            using MemoryStream ms = new(Encoding.UTF8.GetBytes(data));

            MyData = (ModLibData)serializer.ReadObject(ms);
        }
    }

    private static void WriteModLibData()
    {
        DataContractJsonSerializer serializer = new(typeof(ModLibData));

        using MemoryStream ms = new();

        serializer.WriteObject(ms, MyData);

        string data = Encoding.UTF8.GetString(ms.ToArray());

        if (!string.IsNullOrWhiteSpace(data))
        {
            File.WriteAllText(DataPath, data);

            Logger.LogInfo($"Saved ModLib data to StreamingAssets/modlib.json.");
        }
    }

    [DataContract]
    private readonly struct ModLibData(bool devToolsActive, string lastLoadedVersion)
    {
        [DataMember]
        public readonly bool DevToolsActive = devToolsActive;

        [DataMember]
        public readonly string LastLoadedVersion = lastLoadedVersion;
    }

    internal static class PatchLoader
    {
        private const string TARGET_DLL = "ModLib.Loader.dll";

        private static readonly Version _latestLoaderVersion = new("0.2.2.0");

        private static readonly string _targetPath = Path.Combine(Paths.PatcherPluginPath, TARGET_DLL);

        public static void Initialize()
        {
            // Non-whitelisted DLLs are sent to a backup/ folder by MultiFolderLoader
            // When updating ModLib's patcher, we simply un-whitelist it so it's moved elsewhere;
            // ModLib can then deploy the latest version while safely removing the old assembly.
            string backupPath = Path.Combine(Paths.BepInExRootPath, "backup", TARGET_DLL);

            if (File.Exists(backupPath) && File.Exists(_targetPath))
            {
                Version oldVersion = AssemblyName.GetAssemblyName(backupPath).Version;

                Logger.LogInfo($"Patcher update successful. (Previous: {oldVersion}; Current: {_latestLoaderVersion})");

                File.Delete(backupPath);
            }
            else
            {
                DeployVersionLoader();
            }
        }

        private static void DeployVersionLoader()
        {
            Version? localVersion = null;

            if (File.Exists(_targetPath))
            {
                localVersion = AssemblyName.GetAssemblyName(_targetPath).Version;

                if (localVersion >= _latestLoaderVersion)
                {
                    Logger.LogDebug($"Local ModLib patcher is up to date, skipping deploy action. ({localVersion} vs {_latestLoaderVersion})");
                    return;
                }
            }

            if (!File.Exists(_targetPath))
            {
                Logger.LogInfo("Deploying new ModLib.Loader assembly to the game.");

                using Stream stream = MyAssembly.GetManifestResourceStream("ModLib.Loader.dll");

                byte[] block = new byte[stream.Length];
                stream.Read(block, 0, block.Length);

                WriteAssemblyFile(_targetPath, block);

                WhitelistPatcher();
            }
            else
            {
                Logger.LogInfo($"Removing ModLib.Loader patcher from whitelist for update ({localVersion} -> {_latestLoaderVersion}).");

                RemoveFromWhitelist();
            }
        }

        public static void RemoveFromWhitelist()
        {
            string path = Path.Combine(StreamingAssetsPath, "whitelist.txt");

            StringBuilder whitelistBuilder = new();

            using StreamReader reader = File.OpenText(path);

            while (!reader.EndOfStream)
            {
                string entry = reader.ReadLine().ToLowerInvariant();

                if (entry != "modlib.loader.dll")
                {
                    whitelistBuilder.AppendLine(entry);
                }
            }

            reader.Close();

            File.WriteAllText(path, whitelistBuilder.ToString());
        }

        private static void WhitelistPatcher()
        {
            string path = Path.Combine(StreamingAssetsPath, "whitelist.txt");

            using StreamReader reader = File.OpenText(path);

            while (!reader.EndOfStream)
            {
                string entry = reader.ReadLine().ToLowerInvariant();

                if (entry == "modlib.loader.dll")
                {
                    Logger.LogDebug("ModLib.Loader is already whitelisted, skipping action.");
                    return;
                }
            }

            reader.Close();

            try
            {
                File.AppendAllText(path, "modlib.loader.dll" + Environment.NewLine);

                Logger.LogInfo("Added ModLib.Loader to the game's whitelist.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to whitelist patcher assembly! {ex}");
            }
        }

        private static void WriteAssemblyFile(string path, byte[] assemblyData)
        {
            try
            {
                File.WriteAllBytes(path, assemblyData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to write data to {path}: {ex}");
            }
        }
    }
}