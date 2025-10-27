using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using ModLib.Logging;
using ModLib.Options;

namespace ModLib;

/// <summary>
///     The entrypoint for registering mods to ModLib.
/// </summary>
public static class Registry
{
    private static readonly ConditionalWeakTable<Assembly, ModEntry> RegisteredMods = new();

    /// <summary>
    ///     Retrieves the registered metadata of the current mod.
    /// </summary>
    /// <remarks>
    ///     If this property is accessed before the mod is registered to ModLib, a <see cref="ModNotFoundException"/> is thrown.
    /// </remarks>
    /// <exception cref="ModNotFoundException">The current mod assembly was not registered to ModLib.</exception>
    public static ModEntry MyMod => GetMod(Assembly.GetCallingAssembly());

    /// <summary>
    ///     The default path for log files.
    /// </summary>
    public static string DefaultLogsPath => Core.LogsPath;

    static Registry()
    {
        Core.Initialize();

        RegisteredMods.Add(Core.Assembly, new ModEntry(Core.PluginData, null, Core.Logger));
    }

    /// <inheritdoc cref="RegisterMod(BaseUnityPlugin, Type, ManualLogSource)"/>
    public static void RegisterMod(BaseUnityPlugin plugin, Type? optionHolder) =>
        RegisterAssembly(Assembly.GetCallingAssembly(), plugin.Info.Metadata, optionHolder, null);

    /// <summary>
    ///     Registers the current mod assembly to ModLib. This should be done sometime during the mod-loading process,
    ///     typically from the <c>Main</c>/<c>Plugin</c> class constructor, <c>Awake()</c> or <c>OnEnable()</c> methods.
    /// </summary>
    /// <param name="plugin">The <c>Plugin</c> class from which this mod is being registered.</param>
    /// <param name="optionHolder">
    ///     A class with <c>public static</c> fields of type <see cref="Configurable{T}"/>,
    ///     which are retrieved via reflection to determine the mod's REMIX options.
    /// </param>
    /// <param name="logSource">The log source of this mod. If LogUtils is present, a <see cref="LogUtils.Logger"/> will be created with this parameter as its <c>LogSource</c> value.</param>
    /// <exception cref="InvalidOperationException">The current mod assembly is already registered to ModLib.</exception>
    public static void RegisterMod(BaseUnityPlugin plugin, Type? optionHolder, ManualLogSource logSource) =>
        RegisterAssembly(Assembly.GetCallingAssembly(), plugin.Info.Metadata, optionHolder, LoggingAdapter.CreateLogger(logSource));

    /// <summary>
    ///     Registers the current mod assembly to ModLib. This should be done sometime during the mod-loading process,
    ///     typically from the <c>Main</c>/<c>Plugin</c> class constructor, <c>Awake()</c> or <c>OnEnable()</c> methods.
    /// </summary>
    /// <param name="plugin">The <c>Plugin</c> class from which this mod is being registered.</param>
    /// <param name="optionHolder">
    ///     A class with <c>public static</c> fields of type <see cref="Configurable{T}"/>,
    ///     which are retrieved via reflection to determine the mod's REMIX options.
    /// </param>
    /// <param name="logger">The wrapped logger for usage by this mod.</param>
    /// <exception cref="InvalidOperationException">The current mod assembly is already registered to ModLib.</exception>
    public static void RegisterMod(BaseUnityPlugin plugin, Type? optionHolder, IMyLogger? logger) =>
        RegisterAssembly(Assembly.GetCallingAssembly(), plugin.Info.Metadata, optionHolder, logger);

    /// <summary>
    ///     Removes the current mod assembly from ModLib's registry.
    /// </summary>
    /// <returns><c>true</c> if the mod was successfully unregistered, <c>false</c> otherwise (e.g. if it was not registered at all).</returns>
    public static bool UnregisterMod()
    {
        Assembly caller = Assembly.GetCallingAssembly();

        if (!RegisteredMods.TryGetValue(caller, out ModEntry entry)) return false;

        if (entry.OptionHolder is not null)
        {
            ServerOptions.RemoveOptionSource(entry.OptionHolder);
        }

        return RegisteredMods.Remove(caller);
    }

    /// <summary>
    ///     Retrieves the mod metadata for the given assembly.
    /// </summary>
    /// <param name="caller">The assembly to be queried.</param>
    /// <returns>The mod metadata registered for the given assembly.</returns>
    /// <exception cref="ModNotFoundException">The provided assembly was not registered to ModLib.</exception>
    internal static ModEntry GetMod(Assembly caller)
    {
        return RegisteredMods.TryGetValue(caller, out ModEntry metadata)
            ? metadata
            : throw new ModNotFoundException($"Could not find mod for assembly: {caller.FullName}");
    }

    /// <summary>
    ///     Registers the given assembly to ModLib, binding the provided arguments as its metadata.
    /// </summary>
    /// <param name="caller">The assembly to be registered.</param>
    /// <param name="plugin">The plugin data for registry.</param>
    /// <param name="optionHolder">The option holder class for this mod, if any.</param>
    /// <param name="logger">The logger instance for this mod. If null, a new one is created.</param>
    /// <returns>The newly registered mod entry.</returns>
    /// <exception cref="InvalidOperationException">The given assembly is already registered to ModLib.</exception>
    internal static void RegisterAssembly(Assembly caller, BepInPlugin plugin, Type? optionHolder, IMyLogger? logger)
    {
        if (RegisteredMods.TryGetValue(caller, out _))
            throw new InvalidOperationException($"{plugin.Name} is already registered to ModLib.");

        RegisteredMods.Add(caller, new ModEntry(plugin, optionHolder, logger));

        if (optionHolder is not null)
        {
            ServerOptions.AddOptionSource(optionHolder);
        }
    }

    /// <summary>
    ///     Represents a mod entry within ModLib's registry.
    /// </summary> // TODO: Rewrite for clarity
    public record ModEntry
    {
        /// <summary>
        ///     The plugin metadata of this mod.
        /// </summary>
        public BepInPlugin Plugin { get; }

        /// <summary>
        ///     The option holder interface of this mod, if any.
        /// </summary>
        public Type? OptionHolder { get; }

        /// <summary>
        ///     The unique LogID of this mod, if any.
        /// </summary>
        public object? LogID { get; set; }

        /// <summary>
        ///     The logger instance of this mod, if any.
        /// </summary>
        public IMyLogger? Logger { get; set; }

        internal ModEntry(BepInPlugin plugin, Type? optionHolder, ManualLogSource logger)
            : this(plugin, optionHolder, LoggingAdapter.CreateLogger(logger))
        {
        }

        internal ModEntry(BepInPlugin plugin, Type? optionHolder, IMyLogger? logger)
        {
            Plugin = plugin;
            OptionHolder = optionHolder;

            Logger = logger;

            if (Extras.LogUtilsAvailable)
            {
                LogUtilsHelper.InitLogID(this, logger is LogUtilsAdapter adapter && adapter.ModLibCreated);
            }
        }
    }

    /// <summary>
    ///     The exception that is thrown when a ModLib method is called from an unregistered mod assembly.
    /// </summary>
    public sealed class ModNotFoundException : InvalidOperationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class.
        /// </summary>
        public ModNotFoundException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <inheritdoc/>
        public ModNotFoundException(string message)
            : base(message + " (Did you remember to register your mod before calling this method?)")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class with a specified error message
        ///     and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <inheritdoc/>
        public ModNotFoundException(string message, Exception innerException)
            : base(message + " (Did you remember to register your mod before calling this method?)", innerException)
        {
        }
    }
}