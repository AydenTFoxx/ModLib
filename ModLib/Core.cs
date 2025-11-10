using System;
using System.IO;
using System.Reflection;
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
    public const string MOD_VERSION = "0.2.3.0";

    public static readonly BepInPlugin PluginData = new(MOD_GUID, MOD_NAME, MOD_VERSION);
    public static readonly ManualLogSource LogSource = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

    public static readonly string StreamingAssetsPath = Path.Combine(Paths.GameRootPath, "RainWorld_Data", "StreamingAssets");
    public static readonly string LogsPath = Path.Combine(StreamingAssetsPath, "Logs");

    public static IMyLogger Logger { get; private set; } = new FallbackLogger(LogSource);

    public static readonly Assembly MyAssembly = typeof(Core).Assembly;

    public static bool Initialized { get; private set; }

    public static void Initialize()
    {
        if (Initialized) return;

        Initialized = true;

#if DEBUG
        OptionUtils.SharedOptions.AddTemporaryOption("modlib.debug", new ConfigValue(true), false);
#else
        OptionUtils.SharedOptions.AddTemporaryOption("modlib.debug", new ConfigValue(ModManager.DevTools), false);
#endif

        if (Extras.LogUtilsAvailable)
        {
            Logger = LoggingAdapter.CreateLogger(LogSource);
        }

        Logger = new LogWrapper(Logger, OptionUtils.IsOptionEnabled("modlib.debug") ? LogLevel.All : LogLevel.Message);

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }
        else
        {
            On.GameSession.ctor += GameSessionHook;
            On.RainWorldGame.ExitGame += ExitGameHook;
        }

        if (Extras.IsIICEnabled || Extras.IsMeadowEnabled)
        {
            On.RainWorldGame.Update += GameUpdateHook;
        }

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

        if (Extras.IsIICEnabled || Extras.IsMeadowEnabled)
        {
            On.RainWorldGame.Update -= GameUpdateHook;
        }
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

        orig.Invoke(self);

        if (Extras.IsMeadowEnabled)
        {
            ModRPCManager.UpdateRPCs();
        }
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

                Logger.LogDebug($"Patcher update successful. (Previous: {oldVersion}; Current: {_latestLoaderVersion})");

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