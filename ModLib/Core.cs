using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using ModLib.Input;
using ModLib.Logging;
using ModLib.Meadow;
using ModLib.Options;

namespace ModLib;

internal static class Core
{
    public const string MOD_GUID = "ynhzrfxn.modlib";
    public const string MOD_NAME = "ModLib";
    public const string MOD_VERSION = "0.3.1.0";

    public static readonly Assembly MyAssembly = typeof(Core).Assembly;

    public static readonly BepInPlugin PluginData = new(MOD_GUID, MOD_NAME, MOD_VERSION);
    public static readonly ManualLogSource LogSource = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

    public static readonly string StreamingAssetsPath = Path.Combine(Paths.GameRootPath, "RainWorld_Data", "StreamingAssets");
    public static readonly string LogsPath = Path.Combine(StreamingAssetsPath, "Logs");

    private static readonly string DataPath = Path.Combine(StreamingAssetsPath, "modlib.json");

    public static ModLogger Logger { get; private set; } = new FallbackLogger(LogSource);

    public static ModLibData MyData { get; private set; }
    public static bool Initialized { get; private set; }

    public static void Initialize()
    {
        if (Initialized) return;

        Initialized = true;

        if (Extras.LogUtilsAvailable)
        {
            Logger = LoggingAdapter.CreateLogger(LogSource);
        }

        Extras.WrapAction(static () =>
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
        }, Logger);

        Logger = new FilteredLogWrapper(Logger);

        OptionUtils.SharedOptions.AddTemporaryOption("modlib.debug", new ConfigValue(MyData.DevToolsActive), false);

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }
        else
        {
            On.GameSession.ctor += GameSessionHook;
            On.RainWorldGame.ExitGame += ExitGameHook;
        }

        On.RainWorld.OnModsInit += OnModsInitHook;
        On.RainWorldGame.Update += GameUpdateHook;

        Registry.RegisterAssembly(MyAssembly, PluginData, null, Logger);

        try
        {
            PatchLoader.Initialize();
        }
        catch (Exception ex)
        {
            Logger.LogError("Patch loader failed to initialize; Cannot verify local ModLib.Loader assembly.");
            Logger.LogError($"Exception: {ex}");
        }
    }

    public static void Disable()
    {
        if (!Initialized) return;

        Initialized = false;

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

        On.RainWorld.OnModsInit -= OnModsInitHook;
        On.RainWorldGame.Update -= GameUpdateHook;

        Extras.WrapAction(static () =>
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
        }, Logger);
    }

    private static void ExitGameHook(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
    {
        orig.Invoke(self, asDeath, asQuit);

        Extras.InGameSession = false;
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        OptionUtils.SharedOptions.RefreshOptions(Extras.InGameSession);

        Extras.InGameSession = true;
    }

    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        if (Extras.IsIICEnabled)
        {
            ImprovedInputHelper.UpdateInput();
        }
        else
        {
            global::Options? options = UnityEngine.Object.FindObjectOfType<RainWorld>()?.options;
            int maxPlayers = Keybind.MaxPlayers;

            foreach (Keybind keybind in Keybind.Keybinds)
            {
                if (maxPlayers == 1)
                {
                    keybind.Update(options, 0);
                }
                else
                {
                    for (int i = 0; i < maxPlayers; i++)
                    {
                        keybind.Update(options, i);
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

    private static void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        MyData = new(ModManager.DevTools, MOD_VERSION);

        OptionUtils.SharedOptions.AddTemporaryOption("modlib.debug", new ConfigValue(MyData.DevToolsActive), false);

        if (FilteredLogWrapper.DynamicInstances.Count > 0)
        {
            foreach (FilteredLogWrapper logWrapper in FilteredLogWrapper.DynamicInstances)
            {
                logWrapper.MaxLogLevel = MyData.DevToolsActive ? LogLevel.All : LogLevel.Info;
            }

            FilteredLogWrapper.DynamicInstances.Clear();
        }
    }

    [DataContract]
    public readonly struct ModLibData(bool devToolsActive, string lastLoadedVersion)
    {
        [DataMember]
        public readonly bool DevToolsActive = devToolsActive;

        [DataMember]
        public readonly string LastLoadedVersion = lastLoadedVersion;
    }

    private static class PatchLoader
    {
        private const string TARGET_DLL = "ModLib.Loader.dll";

        private static readonly Version _latestLoaderVersion = new("0.2.0.6");

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