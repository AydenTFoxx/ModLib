using System;
using System.Collections.Generic;
using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     A serializable wrapper around a <see cref="SharedOptions"/>' local options dictionary.
/// </summary>
internal sealed class SerializableOptions : Serializer.ICustomSerializable
{
    private const string NullKey = "<NULL>";

    /// <summary>
    ///     The internally held option values;
    /// </summary>
    public Dictionary<string, ConfigurableBase?> Options = [];

    /// <summary>
    ///     Creates a new <see cref="SerializableOptions"/> instance with the provided options for serialization.
    /// </summary>
    /// <remarks>
    ///     Options prefixed with an underscore (<c>_</c>) are ignored for serialization purposes.
    /// </remarks>
    /// <param name="options">The options dictionary for serialization.</param>
    public SerializableOptions(IDictionary<string, ConfigurableBase?> options)
    {
        foreach (KeyValuePair<string, ConfigurableBase?> optionPair in options)
        {
            if (optionPair.Key.StartsWith("_", StringComparison.OrdinalIgnoreCase)) continue;

            Options.Add(optionPair.Key, optionPair.Value);
        }
    }

    /// <summary>
    ///     Creates a new <see cref="SerializableOptions"/> instance with an empty options dictionary for serialization.
    /// </summary>
    public SerializableOptions()
    {
    }

    /// <summary>
    ///     Serializes or de-serializes the referenced local options, using the provided serializer object.
    /// </summary>
    /// <param name="serializer">The serializer for usage by this method.</param>
    public void CustomSerialize(Serializer serializer)
    {
        try
        {
            if (serializer.IsWriting)
            {
                serializer.writer.Write(Options.Count);

                foreach (KeyValuePair<string, ConfigurableBase?> kvp in Options)
                {
                    serializer.writer.Write(kvp.Key);
                    serializer.writer.Write(kvp.Value?.BoxedValue.ToString() ?? NullKey);
                }
            }

            if (serializer.IsReading)
            {
                int count = serializer.reader.ReadInt32();

                if (count is 0) return;

                for (int i = 0; i < count; i++)
                {
                    string key = serializer.reader.ReadString();
                    object? valueData = CastFromString(serializer.reader.ReadString());

                    try
                    {
                        Type valueType = typeof(Configurable<>).MakeGenericType(valueData?.GetType() ?? typeof(object));

                        Options.Add(key, (ConfigurableBase)Activator.CreateInstance(valueType, null, key, valueData, null));
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.LogError($"Failed to serialize option key \"{key}\"!");
                        Core.Logger.LogError($"Exception: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError("Failed to serialize options! Defaulting to empty dictionary.");
            Core.Logger.LogError($"Exception: {ex}");
        }
    }

    private static object? CastFromString(string value)
    {
        return value is NullKey
            ? null
            : bool.TryParse(value, out bool b)
            ? b
            : byte.TryParse(value, out byte y)
            ? y
            : sbyte.TryParse(value, out sbyte sy)
            ? sy
            : short.TryParse(value, out short s)
            ? s
            : ushort.TryParse(value, out ushort us)
            ? us
            : int.TryParse(value, out int i)
            ? i
            : uint.TryParse(value, out uint ui)
            ? ui
            : long.TryParse(value, out long L)
            ? L
            : ulong.TryParse(value, out ulong uL)
            ? uL
            : float.TryParse(value, out float f)
            ? f
            : double.TryParse(value, out double d)
            ? d
            : decimal.TryParse(value, out decimal m)
            ? m
            : value;
    }
}