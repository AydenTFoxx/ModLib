using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace ModLib.Options;

/// <summary>
///     Holds the client's current REMIX options; Provides the ability to override these options, as well as serialize them in an online context.
/// </summary>
public class ServerOptions
{
    private static readonly Dictionary<ConfigurableBase, Type> OptionHolders = [];

    /// <summary>
    ///     Contains the actual option values of the client.
    /// </summary>
    private readonly Dictionary<string, ConfigValue> _options = [];

    /// <summary>
    ///     Contains the identifiers of overriden option keys, as well as their original values.
    /// </summary>
    private readonly Dictionary<string, ConfigValue> _tempOptions = [];

    /// <summary>
    ///     A read-only view of the local holder of option values.
    /// </summary>
    public ReadOnlyDictionary<string, ConfigValue> MyOptions { get; }

    private bool _initialized;

    /// <summary>
    ///     Creates a new <see cref="ServerOptions"/> instance with an empty local holder.
    /// </summary>
    /// <remarks>
    ///     Note: Consider using <see cref="OptionUtils.SharedOptions"/> for an automatically-managed instance of this class instead.
    /// </remarks>
    public ServerOptions()
    {
        MyOptions = new(_options);
    }

    /// <summary>
    ///     Creates a new <see cref="ServerOptions"/> instance with the same held values of the existing instance.
    /// </summary>
    /// <remarks>
    ///     Note: Consider using <see cref="OptionUtils.SharedOptions"/> for an automatically-managed instance of this class instead.
    /// </remarks>
    /// <param name="source">The source whose values will be copied.</param>
    public ServerOptions(ServerOptions source)
        : this()
    {
        _options = source._options;
        _tempOptions = source._tempOptions;
    }

    /// <summary>
    ///     Adds a temporary option to this <see cref="ServerOptions"/> instance with the provided key and value.
    /// </summary>
    /// <remarks>
    ///     Temporary options are raw representations of option values;
    ///     They can be used like any other option key, but are not saved to disk.
    /// </remarks>
    /// <param name="optionKey">The unique key for identifying the temporary option. If an existing option has the same key, it is overriden.</param>
    /// <param name="optionValue">The value to be saved with the given option key.</param>
    /// <param name="removeOnRefresh">If true, this temporary option will be removed the next time <see cref="RefreshOptions"/> is called.</param>
    public void AddTemporaryOption(string optionKey, ConfigValue optionValue, bool removeOnRefresh = true)
    {
        string tempKey = removeOnRefresh && !optionKey.StartsWith("!", StringComparison.OrdinalIgnoreCase) ? $"!{optionKey}" : optionKey;

        _tempOptions[tempKey] = _options.TryGetValue(optionKey, out ConfigValue value) ? value : default;

        _options[optionKey] = optionValue;
    }

    /// <summary>
    ///     Removes a given temporary option from this <see cref="ServerOptions"/> instance.
    /// </summary>
    /// <param name="optionKey">The option key to be removed.</param>
    /// <returns>
    ///     <c>true</c> if the option was successfully removed, <c>false</c> otherwise.
    ///     This method returns <c>false</c> if no temporary option is found with the given key.
    /// </returns>
    public bool RemoveTemporaryOption(string optionKey)
    {
        string tempKey = optionKey.StartsWith("!", StringComparison.OrdinalIgnoreCase)
            ? optionKey
            : _tempOptions.ContainsKey($"!{optionKey}")
                ? $"!{optionKey}"
                : optionKey;

        if (_tempOptions.TryGetValue(tempKey, out ConfigValue value) && value != default)
        {
            _options[optionKey] = value;
        }

        return _tempOptions.Remove(tempKey);
    }

    /// <summary>
    ///     Determines if a given option key is temporary or not.
    /// </summary>
    /// <param name="optionKey">The option key to be searched.</param>
    /// <returns><c>true</c> if the option is temporary, <c>false</c> otherwise.</returns>
    public bool IsTemporaryOption(string optionKey) => _tempOptions.ContainsKey(optionKey) || _tempOptions.ContainsKey($"!{optionKey}");

    /// <summary>
    ///     Sets the local holder's values to those from the REMIX option interface.
    /// </summary>
    /// <param name="shallowRefresh">If true, only temporary options are refreshed, instead of all registered values.</param>
    public void RefreshOptions(bool shallowRefresh = false)
    {
        bool changedOptions = false;

        if (_initialized)
        {
            foreach (KeyValuePair<string, ConfigValue> kvp in _tempOptions)
            {
                if (!kvp.Key.StartsWith("!", StringComparison.OrdinalIgnoreCase)) continue;

                RemoveTemporaryOption(kvp.Key);
                changedOptions = true;

                Core.Logger.LogDebug($"Removed temporary option: [{kvp.Key}]");
            }
        }

        if (shallowRefresh) return;

        foreach (ConfigurableBase configurable in OptionHolders.Keys)
        {
            if (IsTemporaryOption(configurable.key)) continue;

            ConfigValue value = ConfigValue.FromObject(configurable.BoxedValue);

            bool hasKey = _options.ContainsKey(configurable.key);
            if (!hasKey || _options[configurable.key] != value)
            {
                Core.Logger.LogDebug($"{(hasKey ? "Updating" : "Setting")} configurable [{configurable.key}] to {value}.{(hasKey ? $" (Was: {_options[configurable.key]})" : "")}");

                _options[configurable.key] = value;

                changedOptions = true;
            }
        }

        if (!_initialized)
        {
            Core.Logger.LogDebug($"{(Extras.IsOnlineSession ? "Online " : "")}REMIX options are: [{this}]");

            _initialized = true;
        }
        else if (changedOptions)
        {
            Core.Logger.LogDebug($"Updated options are: [{this}]");
        }
    }

    /// <summary>
    ///     Sets the local holder's values to those from the given source.
    /// </summary>
    /// <param name="source">The source whose values will be copied.</param>
    public void SetOptions(ServerOptions source) => SetOptions(source.MyOptions);

    /// <summary>
    ///     Sets the local holder's values to those from the provided dictionary.
    /// </summary>
    /// <param name="options">The dictionary whose values will be copied.</param>
    public void SetOptions(IDictionary<string, ConfigValue> options)
    {
        foreach (KeyValuePair<string, ConfigValue> pair in options)
        {
            if (!_options.TryGetValue(pair.Key, out _))
            {
                Core.Logger.LogWarning($"Unknown key [{pair.Key}], will not be synced.");
                continue;
            }

            Core.Logger.LogDebug($"Setting key {pair.Key} to {pair.Value}.");

            _options[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    ///     Returns a string containing the <see cref="ServerOptions"/>' formatted local values.
    /// </summary>
    /// <returns>A string containing the <see cref="ServerOptions"/>' formatted local values.</returns>
    public override string ToString()
    {
        StringBuilder stringBuilder = new(Environment.NewLine);

        foreach (KeyValuePair<string, ConfigValue> kvp in _options)
        {
            stringBuilder.AppendLine($"{(IsTemporaryOption(kvp.Key) ? "*" : "-")} {kvp.Key}: {kvp.Value};");
        }

        return stringBuilder.ToString();
    }

    internal static void AddOptionSource(Type optionSource)
    {
        foreach (FieldInfo field in optionSource.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not ConfigurableBase configurable
                || Attribute.GetCustomAttribute(field, typeof(ClientOptionAttribute)) is not null)
            {
                continue;
            }

            OptionHolders.Add(configurable, optionSource);
        }
    }

    internal static void RemoveOptionSource(Type optionSource)
    {
        foreach (KeyValuePair<ConfigurableBase, Type> holder in OptionHolders)
        {
            if (holder.Value == optionSource)
            {
                OptionHolders.Remove(holder.Key);
            }
        }
    }
}

/// <summary>
/// Determines a given REMIX option is not to be synced in an online context (e.g. a Rain Meadow lobby).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}