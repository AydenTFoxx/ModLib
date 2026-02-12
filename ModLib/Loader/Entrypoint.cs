using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace ModLib.Loader;

/// <summary>
///     ModLib's entrypoint for initializing core systems and being available for usage as early as possible.
/// </summary>
/// <remarks>
///     This is an internal class of ModLib, exposed for usage by extension assemblies.
///     Unless working strictly in that context, you should not access this class or any of its members directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class Entrypoint
{
    private static readonly EventHandlerList eventHandlerList = new();

    private const byte preInitializeKey = 0;
    private const byte onInitializeKey = 1;
    private const byte preDisableKey = 2;
    private const byte onDisableKey = 3;

    private static ILHook? _initHook;
    private static bool _initializing;

    /// <summary>
    ///     Whether or not ModLib was successfully initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    ///     Invoked just before ModLib starts its initialization process.
    /// </summary>
    public static event Action? PreInitialize
    {
        add => eventHandlerList.AddHandler(preInitializeKey, value);
        remove => eventHandlerList.RemoveHandler(preInitializeKey, value);
    }

    /// <summary>
    ///     Invoked after ModLib finishes initializing.
    /// </summary>
    public static event Action? OnInitialize
    {
        add => eventHandlerList.AddHandler(onInitializeKey, value);
        remove => eventHandlerList.RemoveHandler(onInitializeKey, value);
    }

    /// <summary>
    ///     Invoked just before ModLib starts its shutdown process.
    /// </summary>
    public static event Action? PreDisable
    {
        add => eventHandlerList.AddHandler(preDisableKey, value);
        remove => eventHandlerList.RemoveHandler(preDisableKey, value);
    }

    /// <summary>
    ///     Invoked after ModLib has been disabled.
    /// </summary>
    public static event Action? OnDisable
    {
        add => eventHandlerList.AddHandler(onDisableKey, value);
        remove => eventHandlerList.RemoveHandler(onDisableKey, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TryInitialize([CallerFilePath] string callerPath = "") => Initialize([], callerPath);

    /// <summary>
    ///     Initializes ModLib and loads its resources into the game.
    /// </summary>
    /// <remarks>
    ///     This is an internal method of ModLib, exposed for usage by <c>ModLib.Loader</c>.
    ///     Unless working within that context, you should not call this function.
    /// </remarks>
    /// <param name="compatibilityPaths">The list of paths to config files, retrieved during the preloading process.</param>
    /// <param name="callerPath">The compiler-provided file path to the calling method.</param>
    internal static void Initialize(IList<string> compatibilityPaths, [CallerFilePath] string callerPath = "")
    {
        if (IsInitialized || _initializing) return;

        ModLibExtensionAttribute.LoadAllEntrypoints();

        GetEventByKey<Action>(preInitializeKey)?.Invoke();

        _initializing = true;

        ManualLogSource logSource = Logger.CreateLogSource("ModLib.Entrypoint");

        bool isForcedInit = !callerPath.Contains("ModLib.Loader");

        try
        {
            InitializeCompatManager(compatibilityPaths, logSource, isForcedInit, callerPath);

            Extras.Initialize();

            if (!isForcedInit)
            {
                (_initHook ??= new ILHook(
                    typeof(BepInEx.MultiFolderLoader.ChainloaderHandler).GetMethod("PostFindPluginTypes", BindingFlags.NonPublic | BindingFlags.Static),
                    InitializeCoreILHook)).Apply();
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
    internal static void Disable()
    {
        GetEventByKey<Action>(preDisableKey)?.Invoke();

        try
        {
            Core.Disable();

            _initHook?.Undo();
        }
        catch { }

        _initHook = null;

        _initializing = false;
        IsInitialized = false;

        GetEventByKey<Action>(onDisableKey)?.Invoke();

        ModLibExtensionAttribute.UnloadAllEntrypoints();
    }

    private static void InitializeCoreILHook(ILContext context) =>
        new ILCursor(context).EmitDelegate(CoreInitialize);

    private static void CoreInitialize()
    {
        Core.Initialize();

        GetEventByKey<Action>(onInitializeKey)?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GetEventByKey<T>(byte key) where T : Delegate => (T)eventHandlerList[key];

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