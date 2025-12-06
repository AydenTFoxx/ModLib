using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace ModLib.Loader;

/// <summary>
///     ModLib's entry point for initializing core systems and being available for usage as early as possible.
/// </summary>
/// <remarks>
///     This is an internal class of ModLib, exposed for usage by other assemblies.
///     Unless working specifically in that context, you should not have to access this class or any of its members directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Entrypoint
{
    /// <summary>
    ///     Whether or not ModLib was successfully initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    private static ILHook? _initHook;
    private static bool _initializing;

    internal static void TryInitialize([CallerFilePath] string callerPath = "")
    {
        if (IsInitialized || _initializing) return;

        Initialize([], callerPath);
    }

    /// <summary>
    ///     Initializes ModLib and loads its resources into the game.
    /// </summary>
    /// <remarks>
    ///     This is an internal method of ModLib, exposed for usage by <c>ModLib.Loader</c>.
    ///     Unless working within that context, you should not have to call this function.
    /// </remarks>
    /// <param name="compatibilityPaths">The list of paths to config files, retrieved during the preloading process.</param>
    /// <param name="callerPath">The compiler-provided file path to the calling method.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Initialize(IList<string> compatibilityPaths, [CallerFilePath] string callerPath = "")
    {
        if (IsInitialized) return;

        _initializing = true;

        ManualLogSource logSource = Logger.CreateLogSource("ModLib.Entrypoint");

        bool isForcedInit = !callerPath.Contains("ModLib.Loader");

        try
        {
            InitializeCompatManager(compatibilityPaths, logSource, isForcedInit, callerPath);

            Extras.Initialize();

            if (!isForcedInit)
            {
                _initHook ??= new ILHook(
                    typeof(BepInEx.MultiFolderLoader.ChainloaderHandler).GetMethod("PostFindPluginTypes", BindingFlags.NonPublic | BindingFlags.Static),
                    InitializeCoreILHook
                );
                _initHook.Apply();
            }
            else
            {
                CoreInitialize();
            }

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            logSource.LogError("Failed to initialize ModLib! Some systems may not work as expected.");
            logSource.LogError($"Exception: {ex}");
        }
        finally
        {
            Logger.Sources.Remove(logSource);

            _initializing = false;
        }
    }

    /// <summary>
    ///     Disables ModLib and removes its resources from the game.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Disable()
    {
        try
        {
            Core.Disable();

            _initHook?.Undo();
        }
        catch { }

        _initHook = null;

        _initializing = false;
        IsInitialized = false;
    }

    /// <summary>
    ///     Adds a CorePlugin object to the GameObject all mods are tied to, so ModLib can more accurately detect when the game is shutting down.
    /// </summary>
    private static void InitializeCoreILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(MoveType.After, static x => x.MatchBrfalse(out _))
         .MoveAfterLabels()
         .EmitDelegate(CoreInitialize);
    }

    private static void CoreInitialize()
    {
        if (Chainloader.ManagerObject.TryGetComponent<CorePlugin>(out _)) return;

        Chainloader.ManagerObject.AddComponent<CorePlugin>();

        using Stream stream = Core.MyAssembly.GetManifestResourceStream("HOOKS_ModLib.dll");

        byte[] assemblyData = new byte[stream.Length];
        stream.Read(assemblyData, 0, assemblyData.Length);

        Assembly.Load(assemblyData);
    }

    /// <summary>
    ///     Initializes compatibility checking with the provided paths to config files.
    /// </summary>
    /// <param name="compatibilityPaths">The list of paths to config files, retrieved during the preloader process.</param>
    /// <param name="logger">The logger for usage by this method.</param>
    /// <param name="isForcedInit">Whether or not this is a forced initalization. If the caller is not ModLib's Loader, this should always be <c>true</c>.</param>
    /// <param name="caller">The provided file path to the calling method.</param>
    private static void InitializeCompatManager(IList<string> compatibilityPaths, ManualLogSource logger, bool isForcedInit = false, string caller = "")
    {
        logger.LogInfo($"{nameof(CompatibilityManager)}: Reading {compatibilityPaths.Count} file{(compatibilityPaths.Count != 1 ? "s" : "")} for config overrides...{(isForcedInit ? $" (Forced initialization by {caller})" : "")}");

        using CompatibilityManager.ConfigLoader loader = new(logger);

        loader.Initialize(compatibilityPaths);
    }
}