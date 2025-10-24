using System;
using System.Collections.Generic;
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
    ///     The local holder of REMIX options' values.
    /// </summary>
    public Dictionary<string, ConfigValue> MyOptions { get; } = [];

    /// <summary>
    ///     Sets the local holder's values to those from the REMIX option interface.
    /// </summary>
    /// <param name="resetValues">If true, all values are set to the default underlying value (<c>0</c>) instead.</param>
    public void RefreshOptions(bool resetValues = false)
    {
        foreach (ConfigurableBase configurable in OptionHolders.Keys)
        {
            MyOptions[configurable.key] = resetValues ? default : ConfigValue.FromObject(configurable.BoxedValue);
        }

        Core.Logger.LogDebug($"{(Extras.IsOnlineSession ? "Online " : "")}REMIX options are: {this}");
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
    public void SetOptions(Dictionary<string, ConfigValue> options)
    {
        foreach (KeyValuePair<string, ConfigValue> pair in options)
        {
            if (!MyOptions.TryGetValue(pair.Key, out _))
            {
                Core.Logger.LogWarning($"Unknown key [{pair.Key}], will not be synced.");
                continue;
            }

            Core.Logger.LogDebug($"Setting key {pair.Key} to {pair.Value}.");

            MyOptions[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    ///     Returns a string containing the <see cref="ServerOptions"/>' local values.
    /// </summary>
    /// <returns>A string containing the <see cref="ServerOptions"/>' local values.</returns>
    public override string ToString() => $"[{FormatOptions()}]";

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

    private string FormatOptions()
    {
        StringBuilder stringBuilder = new(Environment.NewLine);

        foreach (KeyValuePair<string, ConfigValue> kvp in MyOptions)
        {
            stringBuilder.Append($"{kvp.Key}: {kvp.Value};{Environment.NewLine}");
        }

        stringBuilder.Append(Environment.NewLine);

        return stringBuilder.ToString().Trim();
    }
}

/// <summary>
/// Determines a given REMIX option is not to be synced in an online context (e.g. a Rain Meadow lobby).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}