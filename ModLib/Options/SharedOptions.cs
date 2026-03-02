using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ModLib.Loader;

namespace ModLib.Options;

/// <summary>
///     Static holder of all registered mods' REMIX options and their values, with support for automatic sync and serialization in Rain Meadow lobbies.
///     Can be configured to add, remove, and temporarily override any given option, all in a single centralized API.
/// </summary>
public static class SharedOptions
{
    /// <summary>
    ///     The types of the objects whose fields are retrieved to populate the options dictionary.
    /// </summary>
    private static readonly Dictionary<ConfigurableBase, Type> OptionHolders = [];

    /// <summary>
    ///     Contains the stored REMIX options of the client.
    /// </summary>
    private static readonly Dictionary<string, ConfigurableBase> _options = [];

    /// <summary>
    ///     Contains the identifiers of overriden option keys, as well as their original values.
    /// </summary>
    private static readonly Dictionary<string, ConfigurableBase> _tempOptions = [];

    /// <summary>
    ///     Returns a read-only dictionary containing all registered options and their values. This property is read-only.
    /// </summary>
    public static ReadOnlyDictionary<string, ConfigurableBase> MyOptions { get; } = new(_options);

    /// <summary>
    ///     Determines if the options dictionary has been populated at least once in the current session.
    /// </summary>
    private static bool _initialized;

    static SharedOptions()
    {
        Entrypoint.TryInitialize();
    }

    /// <inheritdoc cref="GetOptionValue{T}(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOptionValue<T>(Configurable<T> option) => GetOptionValue<T>(option?.key!);

    /// <summary>
    ///     Retrieves the current value of the specified option.
    /// </summary>
    /// <typeparam name="T">The type of the option's value.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <returns>The current value of the given option, or the default value for <typeparamref name="T"/> if <paramref name="option"/> is null or wasn't found in the options dictionary.</returns>
    public static T? GetOptionValue<T>(string option) =>
        !string.IsNullOrEmpty(option) && _options.ContainsKey(option) ? (T?)_options[option].BoxedValue : default;

    /// <inheritdoc cref="IsOptionEnabled(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptionEnabled(Configurable<bool> option) => IsOptionEnabled(option?.key!);

    /// <summary>
    ///     Determines if the given boolean option is enabled (i.e. its value is <c>true</c>).
    /// </summary>
    /// <param name="option">The option to be queried. Must be of type <c>bool</c>.</param>
    /// <returns><c>true</c> if the option is found and its value is <c>true</c>, <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentException">The provided option is not of <see cref="bool"/> type.</exception>
    public static bool IsOptionEnabled(string option) =>
        !string.IsNullOrEmpty(option) && _options.ContainsKey(option) && (_options[option].BoxedValue is bool v ? v : throw new ArgumentException($"Expected an option of type {typeof(bool)}, but got: {_options[option].BoxedValue.GetType()}", nameof(option)));

    /// <inheritdoc cref="IsOptionValue{T}(string, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptionValue<T>(Configurable<T> option, T value) => IsOptionValue(option?.key!, value);

    /// <summary>
    ///     Determines if a given object matches the specified option's stored value.
    /// </summary>
    /// <typeparam name="T">The type of the option's value.</typeparam>
    /// <param name="option">The name of the option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the expected value, <c>false</c> otherwise.</returns>
    public static bool IsOptionValue<T>(string option, T value) =>
        !string.IsNullOrEmpty(option) && _options.TryGetValue(option, out ConfigurableBase v) && (v.BoxedValue?.Equals(value) ?? false);

    /// <inheritdoc cref="IsEphemeral(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEphemeral<T>(Configurable<T> option) => IsEphemeral(option?.key!);

    /// <summary>
    ///     Determines if the specified option is ephemeral, i.e. an option whose value will be removed in the next refresh.
    /// </summary>
    /// <param name="optionKey">The option to be searched.</param>
    /// <returns><c>true</c> if the option is ephemeral, <c>false</c> otherwise.</returns>
    public static bool IsEphemeral(string optionKey) => !string.IsNullOrEmpty(optionKey) && optionKey.StartsWith("!", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc cref="IsOverriden(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOverriden<T>(Configurable<T> option) => IsOverriden(option?.key!);

    /// <summary>
    ///     Determines if the specified option is being overriden by a temporary value.
    /// </summary>
    /// <param name="option">The option to be searched.</param>
    /// <returns><c>true</c> if the option's value is being overriden, <c>false</c> otherwise.</returns>
    public static bool IsOverriden(string option) => !string.IsNullOrEmpty(option) && (_tempOptions.ContainsKey(option) || _tempOptions.ContainsKey($"!{option}"));

    /// <inheritdoc cref="IsOverriden(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetOption<T>(Configurable<T> option, T value, bool isEphemeral = false) => SetOption(option?.key!, value, isEphemeral);

    /// <summary>
    ///     Sets the specified key to the provided value.
    ///     If the key already had a value, it is temporarily overriden until removed (or if the new value is marked as ephemeral, until a new cycle starts).
    /// </summary>
    /// <typeparam name="T">The type of the value to be added.</typeparam>
    /// <param name="option">The key to be set.</param>
    /// <param name="value">The value to be added.</param>
    /// <param name="isEphemeral">If true, value will be removed on the next refresh (i.e. upon starting a new cycle or exiting to the menu).</param>
    /// <exception cref="ArgumentException">optionKey is already present and of a different type than <paramref name="value"/>.</exception>
    public static void SetOption<T>(string option, T value, bool isEphemeral = false)
    {
        if (string.IsNullOrEmpty(option)) return;

        if (_options.TryGetValue(option, out ConfigurableBase origValue))
        {
            if (origValue.BoxedValue.GetType() != (value?.GetType() ?? typeof(T)))
                throw new ArgumentException($"Option overrides must share the same type as their original values. (Expected: {origValue.BoxedValue.GetType()}; Actual: {value?.GetType() ?? typeof(T)})", nameof(value));

            _tempOptions[isEphemeral && !option.StartsWith("!", StringComparison.OrdinalIgnoreCase) ? $"!{option}" : option] = origValue;
        }

        _options[option] = new Configurable<T>(null, option, value, null);
    }

    /// <inheritdoc cref="RemoveOption(string, bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveOption<T>(Configurable<T> option, bool tempOnly = false) => RemoveOption(option?.key!, tempOnly);

    /// <summary>
    ///     Removes a given option key from the <see cref="SharedOptions"/> collection.
    ///     Can be set to only remove temporary overrides, preserving the key's base value.
    /// </summary>
    /// <param name="optionKey">The option key to be removed.</param>
    /// <param name="tempOnly">
    ///     If true and the option key was overriden, its original value is restored instead of removing it entirely.
    ///     Otherwise, the key is removed alongside any temporary overrides.
    /// </param>
    /// <returns>
    ///     <c>true</c> if key was successfully removed or restored to a previous value, <c>false</c> otherwise.
    ///     This method also returns <c>false</c> if no option was found with the given key.
    /// </returns>
    public static bool RemoveOption(string optionKey, bool tempOnly = false)
    {
        string tempKey = optionKey.StartsWith("!", StringComparison.OrdinalIgnoreCase)
            ? optionKey
            : _tempOptions.ContainsKey($"!{optionKey}")
                ? $"!{optionKey}"
                : optionKey;

        if (_tempOptions.TryGetValue(tempKey, out ConfigurableBase value))
        {
            _options[optionKey] = value;

            bool removedOverride = _tempOptions.Remove(tempKey);

            if (tempOnly)
                return removedOverride;
        }

        return _options.Remove(tempKey);
    }

    /// <summary>
    ///     Returns a string containing the <see cref="SharedOptions"/>' formatted local values.
    /// </summary>
    /// <returns>A string containing the <see cref="SharedOptions"/>' formatted local values.</returns>
    public static string FormatOptions() => $"{Environment.NewLine}{string.Join(Environment.NewLine, _options.Select(static kvp => $"{(IsOverriden(kvp.Key) ? "=" : IsEphemeral(kvp.Key) ? "*" : "-")} {kvp.Key}: {kvp.Value.BoxedValue};"))}{Environment.NewLine}";

    internal static void AddOptionSource(Type optionSource)
    {
        foreach (FieldInfo field in optionSource.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is ConfigurableBase configurable)
                OptionHolders.Add(configurable, optionSource);
        }
    }

    internal static void RemoveOptionSource(Type optionSource)
    {
        foreach (KeyValuePair<ConfigurableBase, Type> holder in OptionHolders)
        {
            if (holder.Value == optionSource)
                OptionHolders.Remove(holder.Key);
        }
    }

    /// <summary>
    ///     Sets the local holder's values to those from the provided dictionary.
    /// </summary>
    /// <param name="options">The dictionary whose values will be copied.</param>
    internal static void SetOptions(IDictionary<string, ConfigurableBase?> options)
    {
        foreach (KeyValuePair<string, ConfigurableBase?> pair in options)
        {
            if (!_options.TryGetValue(pair.Key, out _))
            {
                Core.Logger.LogWarning($"Unknown key [{pair.Key}], will not be synced.");
            }
            else if (pair.Value is null)
            {
                Core.Logger.LogDebug($"Removing key {pair.Key}.");

                _options.Remove(pair.Key);
                _tempOptions.Remove(pair.Key);
            }
            else
            {
                Core.Logger.LogDebug($"Setting key {pair.Key} to {pair.Value}.");

                _options[pair.Key] = pair.Value;
            }
        }
    }

    /// <summary>
    ///     Sets the local holder's values to those from the REMIX option interface.
    /// </summary>
    /// <param name="shallowRefresh">If true, only temporary options are refreshed, instead of all registered values.</param>
    internal static IDictionary<string, ConfigurableBase?> RefreshOptions(bool shallowRefresh = false)
    {
        Dictionary<string, ConfigurableBase?> changedOptions = [];

        if (_initialized)
        {
            foreach (KeyValuePair<string, ConfigurableBase> kvp in _tempOptions)
            {
                if (!IsEphemeral(kvp.Key)) continue;

                RemoveOption(kvp.Key, tempOnly: true);
                changedOptions.Add(kvp.Key, null);

                Core.Logger.LogDebug($"Removed temporary option: [{kvp.Key}]");
            }
        }

        if (!shallowRefresh)
        {
            foreach (ConfigurableBase configurable in OptionHolders.Keys)
            {
                if (IsOverriden(configurable.key)) continue;

                bool hasKey = _options.ContainsKey(configurable.key);
                if (!hasKey || _options[configurable.key] != configurable)
                {
                    Core.Logger.LogDebug($"{(hasKey ? "Updating" : "Setting")} configurable [{configurable.key}] to {configurable}.{(hasKey ? $" (Was: {_options[configurable.key]})" : "")}");

                    _options[configurable.key] = configurable;

                    changedOptions.Add(configurable.key, configurable);
                }
            }

            if (!_initialized)
            {
                Core.Logger.LogDebug($"{(Extras.IsOnlineSession ? "Online " : "")}REMIX options are: [{FormatOptions()}]");

                _initialized = true;
            }
            else if (changedOptions.Count is not 0)
            {
                Core.Logger.LogDebug($"Updated options are: [{FormatOptions()}]");
            }
        }

        return changedOptions;
    }
}