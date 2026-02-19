using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Represents a collection of online keys and values which can be converted to an equivalent local representation.
/// </summary>
/// <typeparam name="TOnlineKey">The type of the online representation of the dictionary's keys.</typeparam>
/// <typeparam name="TOnlineValue">The type of the online representation of the dictionary's values.</typeparam>
public class SerializableDictionary<TOnlineKey, TOnlineValue> : Dictionary<TOnlineKey, TOnlineValue>, Serializer.ICustomSerializable
{
    private static readonly MethodInfo Serializer_GetSerializationMethod = typeof(Serializer).GetMethod("GetSerializationMethod", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException($"Could not retrieve RainMeadow method Serializer.GetSerializationMethod(). (This is a ModLib error; Please report it on its GitHub repository if you're seeing this message)");

    private readonly MethodInfo? _serializeKeyMethod = GetSerializerMethod(typeof(TOnlineKey));
    private readonly MethodInfo? _serializeValueMethod = GetSerializerMethod(typeof(TOnlineValue));

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that is empty, has the default initial capacity, and uses the default equality comparer for the key type.
    /// </summary>
    public SerializableDictionary() : base() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that is empty, has the specified initial capacity, and uses the default equality comparer for the key type.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
    public SerializableDictionary(int capacity) : base(capacity) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that is empty, has the default initial capacity, and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="comparer">
    ///     The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys,
    ///     or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.
    /// </param>
    public SerializableDictionary(IEqualityComparer<TOnlineKey> comparer) : base(comparer) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that is empty, has the specified initial capacity, and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> can contain.</param>
    /// <param name="comparer">
    ///     The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys,
    ///     or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
    public SerializableDictionary(int capacity, IEqualityComparer<TOnlineKey> comparer) : base(capacity, comparer) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the default equality comparer for the key type.
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/>.</param>
    /// <exception cref="ArgumentNullException">dictionary is null.</exception>
    /// <exception cref="ArgumentException">dictionary contains one or more duplicate keys.</exception>
    public SerializableDictionary(IDictionary<TOnlineKey, TOnlineValue> dictionary) : base(dictionary) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class
    ///     that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/> and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/>.</param>
    /// <param name="comparer">
    ///     The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys,
    ///     or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.
    /// </param>
    /// <exception cref="ArgumentNullException">dictionary is null.</exception>
    /// <exception cref="ArgumentException">dictionary contains one or more duplicate keys.</exception>
    public SerializableDictionary(IDictionary<TOnlineKey, TOnlineValue> dictionary, IEqualityComparer<TOnlineKey> comparer) : base(dictionary, comparer) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/> class with serialized data.
    /// </summary>
    /// <remarks>
    ///     This constructor is inherited from <see cref="Dictionary{TKey, TValue}"/>, and is NOT meant for usage with Rain Meadow.
    /// </remarks>
    /// <param name="info">A <see cref="SerializationInfo"/> object containing the information required to serialize the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/>.</param>
    /// <param name="context">A <see cref="StreamingContext"/> structure containing the source and destination of the serialized stream associated with the <see cref="SerializableDictionary{TOnlineKey, TOnlineValue}"/>.</param>
    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Reads or writes the contents of the dictionary to a serializer in an online context.
    /// </summary>
    /// <param name="serializer">The serializer instance used for reading/writing the dictionary's contents.</param>
    public virtual void CustomSerialize(Serializer serializer)
    {
        if (serializer.IsWriting)
        {
            serializer.writer.Write(Count);

            foreach (KeyValuePair<TOnlineKey, TOnlineValue> kvp in this)
            {
                TOnlineKey key = kvp.Key;
                TOnlineValue value = kvp.Value;

                try
                {
                    SerializeElement(serializer, ref key, ref value);
                }
                catch (Exception ex)
                {
                    Core.Logger.LogError($"[{GetType()}] Failed to serialize <{key}, {value}> (Write)! {ex}");
                }
            }
        }

        if (serializer.IsReading)
        {
            for (int i = serializer.reader.ReadInt32(); i > 0; i--)
            {
                TOnlineKey key = default!;
                TOnlineValue value = default!;

                try
                {
                    SerializeElement(serializer, ref key, ref value);

                    Add(key, value);
                }
                catch (Exception ex)
                {
                    Core.Logger.LogError($"[{GetType()}] Failed to serialize <{key}, {value}> (Read)! {ex}");
                }
            }
        }
    }

    /// <summary>
    ///     Returns a new dictionary containing the local representation of all elements in this dictionary,
    ///     using the provided delegate for the conversion.
    /// </summary>
    /// <param name="conversionMethod">The delegate for converting online types to their local counterparts.</param>
    /// <returns>A new dictionary containing the local representation of all elements in this dictionary.</returns>
    public IDictionary<TLocalKey, TLocalValue> ToLocalCollection<TLocalKey, TLocalValue>(Func<KeyValuePair<TOnlineKey, TOnlineValue>, KeyValuePair<TLocalKey, TLocalValue>> conversionMethod)
    {
        Dictionary<TLocalKey, TLocalValue> result = [];

        foreach (KeyValuePair<TOnlineKey, TOnlineValue> kvp in this)
        {
            KeyValuePair<TLocalKey, TLocalValue> conversion = conversionMethod.Invoke(kvp);

            try
            {
                result.Add(conversion.Key, conversion.Value);
            }
            catch (ArgumentException ex)
            {
                Core.Logger.LogError($"[{GetType()}] Failed to convert <{kvp.Key}, {kvp.Value}> to local value! {ex}");
            }
        }

        return result;
    }

    /// <summary>
    ///     Serializes a given pair of elements with the given serializer.
    /// </summary>
    /// <param name="serializer">The serializer used for the operation.</param>
    /// <param name="key">The key to be serialized.</param>
    /// <param name="value">The value to be serialized.</param>
    protected void SerializeElement(Serializer serializer, ref TOnlineKey key, ref TOnlineValue value)
    {
        if (key is Serializer.ICustomSerializable sKey)
            sKey.CustomSerialize(serializer);
        else
            _serializeKeyMethod?.Invoke(serializer, [key]);

        if (value is Serializer.ICustomSerializable sValue)
            sValue.CustomSerialize(serializer);
        else
            _serializeValueMethod?.Invoke(serializer, [value]);
    }

    /// <summary>
    ///     Retrieves the registered method used for serializing a given type.
    /// </summary>
    /// <param name="type">The type whose serialization method will be searched.</param>
    /// <returns>The method used for serializing <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentException">No serialization method was found for <paramref name="type"/>.</exception>
    private static MethodInfo? GetSerializerMethod(Type type) =>
        type is Serializer.ICustomSerializable
            ? null
            : (MethodInfo)Serializer_GetSerializationMethod.Invoke(null, [type, (!type.IsValueType && (!type.IsArray || !type.GetElementType().IsValueType)) || Nullable.GetUnderlyingType(type) is not null, true, true]) ?? throw new ArgumentException($"Could not find serializer method for type {type}.", nameof(type));
}