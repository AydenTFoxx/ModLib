using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ModLib.Logging;

namespace ModLib;

/// <summary>
///     A BaseUnityPlugin skeleton for quick prototyping and development.
/// </summary>
/// <remarks>
///     Notice: All visible methods are <c>virtual</c>, and may be overriden to load your mod's assets and hooks into the game.
/// </remarks>
public abstract class ModPlugin : BaseUnityPlugin
{
    /// <summary>
    ///     The REMIX option interface registered for this mod, if any. This field is read-only.
    /// </summary>
    protected readonly OptionInterface? Options;

    /// <summary>
    ///     Determines if this mod has successfuly been enabled.
    /// </summary>
    protected bool IsModEnabled { get; set; }

    /// <summary>
    ///     Determines if LoadResources has been successfully called during initialization.
    /// </summary>
    protected bool ResourcesLoaded { get; set; }

    /// <summary>
    ///     The custom logger instance for this mod.
    /// </summary>
    protected new ModLogger Logger { get; set; }

    /// <summary>
    ///     The <see cref="ManualLogSource"/> inherited from <see cref="BaseUnityPlugin"/> by this mod.
    /// </summary>
    protected ManualLogSource LogSource { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    ///     Creates a new ModPlugin instance with no REMIX option interface.
    /// </summary>
    public ModPlugin()
    {
        Initialize(Assembly.GetCallingAssembly(), null, null);
    }

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    public ModPlugin(OptionInterface? options)
    {
        Options = options;

        Initialize(Assembly.GetCallingAssembly(), options?.GetType(), LoggingAdapter.CreateLogger(base.Logger));
    }

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface and logger instance.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    /// <param name="logger">The logger instance to be used. If null, a new one is created and assigned to this mod.</param>
    public ModPlugin(OptionInterface? options, ModLogger? logger)
    {
        Options = options;

        Initialize(Assembly.GetCallingAssembly(), options?.GetType(), logger);
    }

    private void Initialize(Assembly caller, Type? optionHolder, ModLogger? logger)
    {
        LogSource = base.Logger;

        logger ??= LoggingAdapter.CreateLogger(LogSource);

        if (logger is not LogUtilsLogger { ModLibCreated: false })
        {
            string pathToLogFile = Path.Combine(Registry.DefaultLogsPath, Registry.SanitizeModName(Info.Metadata.Name) + ".log");

            if (File.Exists(pathToLogFile))
            {
                Extras.WrapAction(() => File.WriteAllText(pathToLogFile, ""), Core.Logger);
            }
        }

        Registry.RegisterAssembly(caller, Info.Metadata, optionHolder, logger);

        Logger = logger;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    ///     Applies hooks to the game, then marks the mod as enabled.
    ///     Override this to add behavior which should only occur once, while your mod is being loaded by the game.
    /// </summary>
    public virtual void OnEnable()
    {
        if (IsModEnabled) return;
        IsModEnabled = true;

        Extras.WrapAction(() =>
        {
            ApplyHooks();

            Logger.LogDebug("Successfully registered hooks to the game.");
        }, Logger);

        Logger.LogInfo($"Enabled {Info.Metadata.Name} successfully.");
    }

    /// <summary>
    ///     Removes hooks from the game, then marks the mod as disabled.
    ///     Override this to run behavior which should occur when your mod is disabled/reloaded by the game.
    /// </summary>
    /// <remarks>
    ///     This is most useful for Rain Reloader compatibility, but also seems to be called by the base game on exit.
    /// </remarks>
    public virtual void OnDisable()
    {
        if (!IsModEnabled) return;

        IsModEnabled = false;
        ResourcesLoaded = false;

        Extras.WrapAction(() =>
        {
            RemoveHooks();

            Logger.LogDebug("Removed all hooks successfully.");
        }, Logger);

        Logger.LogInfo($"Disabled {Info.Metadata.Name} successfully.");
    }

    /// <summary>
    ///     Load any resources, such as sprites or sounds. This also registers the mod's REMIX interface to the game.<br/>
    ///     <br/>
    ///     Override this to add behavior which must run exactly once, and only after all mods have been loaded into the game.
    /// </summary>
    protected virtual void LoadResources()
    {
        if (Options is null || MachineConnector.SetRegisteredOI(Info.Metadata.GUID, Options)) return;

        Logger.LogWarning("Failed to initialize registered option interface! Attempting to register directly to MachineConnector._registeredOIs instead.");

        try
        {
            MachineConnector._registeredOIs[Info.Metadata.GUID] = Options;

            MachineConnector._RefreshOIs();

            Logger.LogInfo($"Successfully registered option interface {Options} with mod ID \"{Info.Metadata.GUID}\".");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to register option interface to MachineConnector! {ex}");
        }
    }

    /// <summary>
    ///     Applies this mod's hooks to the game.
    /// </summary>
    protected virtual void ApplyHooks() => On.RainWorld.OnModsInit += OnModsInitHook;

    /// <summary>
    ///     Removes this mod's hooks from the game.
    /// </summary>
    protected virtual void RemoveHooks() => On.RainWorld.OnModsInit -= OnModsInitHook;

    /// <summary>
    ///     Loads this mod's resources to the game. For adding your own behavior, override <see cref="LoadResources"/> instead.
    /// </summary>
    protected void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        if (!ResourcesLoaded)
        {
            Extras.WrapAction(LoadResources, Logger);

            ResourcesLoaded = true;
        }
    }
}