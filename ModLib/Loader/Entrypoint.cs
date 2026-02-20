using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using ModLib.Extensions;
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
    private static readonly List<IExtensionEntrypoint> LoadedExtensions = [];

    private static ManualLogSource LogSource = Logger.CreateLogSource("ModLib.Entrypoint");
    private static ILHook? _initHook;

    private static bool _initializing;

    /// <summary>
    ///     Whether or not ModLib has successfully initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    ///     Attempts to initialize ModLib if it hasn't been initialized already.
    /// </summary>
    /// <param name="callerPath">The compiler-provided file path to the caller of this method.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TryInitialize([CallerFilePath] string callerPath = "") => Initialize([], true, callerPath);

    /// <inheritdoc cref="Initialize(IList{string}, bool, string)"/>
    [Obsolete("Prefer the overload `Initialize(IList<string>, bool, string)`; This method will be removed in a future update.")]
    internal static void Initialize(IList<string> compatibilityPaths, [CallerFilePath] string callerPath = "") =>
        Initialize(compatibilityPaths, false, callerPath); // Required for upgrading from ModLib.Loader < v0.2.1.0

    /// <summary>
    ///     Initializes ModLib and loads its resources into the game.
    /// </summary>
    /// <remarks>
    ///     This is an internal method of ModLib, exposed for usage by <c>ModLib.Loader</c>.
    ///     Unless working within that context, you should not call this function.
    /// </remarks>
    /// <param name="compatibilityPaths">The list of paths to config files, retrieved during the preloading process.</param>
    /// <param name="callerPath">The compiler-provided name of the caller.</param>
    /// <param name="isLateInit">Whether the current initialization was triggered outside of the preloading process.</param>
    internal static void Initialize(IList<string> compatibilityPaths, bool isLateInit, [CallerFilePath] string callerPath = "")
    {
        if (IsInitialized || _initializing) return;

        _initializing = true;

        LoadExtensionEntrypoints();

        try
        {
            LogSource.LogInfo($"{nameof(CompatibilityManager)}: Reading {compatibilityPaths.Count} file{(compatibilityPaths.Count != 1 ? "s" : "")} for config overrides...{(isLateInit ? $" (Late initialization from {callerPath})" : "")}");

            new CompatibilityManager.ConfigLoader(LogSource).Initialize(compatibilityPaths);

            Extras.Initialize();

            if (isLateInit)
            {
                CoreInitialize();
            }
            else
            {
                (_initHook ??= new ILHook(
                    typeof(BepInEx.MultiFolderLoader.ChainloaderHandler).GetMethod("PostFindPluginTypes", BindingFlags.NonPublic | BindingFlags.Static),
                    InitializeCoreILHook)).Apply();
            }
        }
        catch (Exception ex)
        {
            LogSource.LogError("Failed to initialize ModLib! Some systems may not work as expected. (Init Phase: #1)");
            LogSource.LogError($"Exception: {ex}");
        }

        static void InitializeCoreILHook(ILContext context)
        {
            _ = new ILCursor(context).EmitDelegate(CoreInitialize);
        }
    }

    /// <summary>
    ///     Disables ModLib and removes its resources from the game.
    /// </summary>
    internal static void Disable()
    {
        if (LoadedExtensions.Count > 0)
        {
            for (int i = 0; i < LoadedExtensions.Count; i++)
            {
                IExtensionEntrypoint entrypoint = LoadedExtensions[i];

                try
                {
                    entrypoint.OnDisable();
                }
                catch (Exception ex)
                {
                    LogSource.LogError($"Failed to invoke OnDisable() for entrypoint [{entrypoint.GetType().AssemblyQualifiedName}]!");
                    LogSource.LogError($"Exception: {ex}");
                }
            }
        }

        try
        {
            Core.Disable();

            if (Extras.RainReloaderActive)
                _initHook?.Undo();
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Thrown exception while unloading ModLib: {ex}");
        }

        _initHook = null;

        _initializing = false;
        IsInitialized = false;

        LoadedExtensions.Clear();
    }

    private static void CoreInitialize()
    {
        if (LoadedExtensions.Count > 0)
        {
            for (int i = 0; i < LoadedExtensions.Count; i++)
            {
                IExtensionEntrypoint entrypoint = LoadedExtensions[i];

                try
                {
                    entrypoint.OnEnable();
                }
                catch (Exception ex)
                {
                    LogSource.LogError($"Failed to invoke OnEnable() for entrypoint [{entrypoint.GetType().AssemblyQualifiedName}]!");
                    LogSource.LogError($"Exception: {ex}");
                }
            }
        }

        try
        {
            Core.Initialize();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            LogSource.LogError("Failed to initialize ModLib! Some systems may not work as expected. (Init Phase: #2)");
            LogSource.LogError($"Exception: {ex}");
        }

        try
        {
            Core.PatchLoader.Initialize();
        }
        catch (Exception ex)
        {
            LogSource.LogError("Patch loader failed to initialize; Cannot verify local ModLib.Loader assembly.");
            LogSource.LogError($"Exception: {ex}");
        }

        if (IsInitialized)
        {
            Logger.Sources.Remove(LogSource);
            LogSource = null!;
        }

        _initializing = false;
    }

    private static void LoadExtensionEntrypoints()
    {
        List<Type> types = [.. AssemblyExtensions.GetAllTypes().Where(static t => t is { IsInterface: false, IsAbstract: false } && typeof(IExtensionEntrypoint).IsAssignableFrom(t))];

        if (types.Count == 0)
        {
            LogSource.LogDebug("No extension entrypoints found, skipping loading.");
            return;
        }

        for (int i = 0; i < types.Count; i++)
        {
            Type type = types[i];

            try
            {
                IExtensionEntrypoint entrypoint = (IExtensionEntrypoint)Activator.CreateInstance(type);

                LogSource.LogDebug($"Loading extension entrypoint: [{type.AssemblyQualifiedName}]");

                Registry.RegisterAssembly(type.Assembly, entrypoint.Metadata, null, null);

                LoadedExtensions.Add(entrypoint);
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Failed to initialize extension entrypoint: [{type.AssemblyQualifiedName}]");
                LogSource.LogError($"Exception: {ex}");
            }
        }
    }
}