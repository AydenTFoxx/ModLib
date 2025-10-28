using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

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

    internal static bool CanInitialize { get; private set; } = true;

    private static bool _initializing;

    internal static void TryInitialize([CallerFilePath] string callerPath = "")
    {
        if (CanInitialize)
        {
            CanInitialize = false;

            if (!IsInitialized && !_initializing)
                Initialize([], callerPath);

            Extras.WrapAction(Core.Initialize, Core.Logger);
        }
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
        }
        catch { }

        IsInitialized = false;
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