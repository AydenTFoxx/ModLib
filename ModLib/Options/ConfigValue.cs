using System;
using System.Runtime.InteropServices;

namespace ModLib.Options;

// Thanks to kugutsuhasu for helping with this code :P

/// <summary>
///     A holder of supported values for <see cref="Configurable{T}"/> options.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct ConfigValue : IComparable, IComparable<ConfigValue>, IEquatable<ConfigValue>
{
    [FieldOffset(0)] private readonly int _intValue;
    [FieldOffset(0)] private readonly float _floatValue;
    [FieldOffset(0)] private readonly bool _boolValue;
    [FieldOffset(8)] private readonly string? _stringValue; // ref type stored separately

    /// <summary>
    ///     Determines the internally held type of this object.
    /// </summary>
    [field: FieldOffset(4)]
    public readonly ValueKind Kind { get; }

    /// <summary>
    ///     Creates a new configurable value holding the provided value type.
    /// </summary>
    /// <param name="value">The value type to be stored. Must be an integer, float, or boolean.</param>
    /// <exception cref="NotSupportedException">The provided value type is not one of the above supported types.</exception>
    public ConfigValue(ValueType? value)
    {
        switch (value)
        {
            case bool:
                {
                    _boolValue = (bool)value;
                    Kind = ValueKind.Bool;
                    break;
                }
            case int:
                {
                    _intValue = (int)value;
                    Kind = ValueKind.Int;
                    break;
                }
            case float:
                {
                    _floatValue = (float)value;
                    Kind = ValueKind.Float;
                    break;
                }
            default:
                throw new NotSupportedException($"Option type must be one of {typeof(int)}, {typeof(float)}, {typeof(bool)} or {typeof(string)}.");
        }
    }

    /// <summary>
    ///     Creates a new configurable value holding the provided string object.
    /// </summary>
    /// <param name="value">The string to be stored.</param>
    public ConfigValue(string value)
    {
        _stringValue = value;
        Kind = ValueKind.String;
    }

    /// <summary>
    ///     Creates a new configurable value holding the provided object.
    /// </summary>
    /// <param name="value">The object to be stored. Must be either an integer, float, boolean or string.</param>
    /// <returns>The newly created <see cref="ConfigValue"/> instance.</returns>
    public static ConfigValue FromObject(object? value)
    {
        return value is string s
            ? new ConfigValue(s)
            : new ConfigValue(value as ValueType);
    }

    /// <summary>
    ///     Retrieves a boxed representation of the internal value stored by this <see cref="ConfigValue"/> instance.
    /// </summary>
    /// <returns>The boxed internally held value of this instance, or <c>null</c> if none is found.</returns>
    public object? GetBoxedValue()
    {
        return Kind switch
        {
            ValueKind.Int => _intValue,
            ValueKind.Float => _floatValue,
            ValueKind.Bool => _boolValue,
            ValueKind.String => _stringValue,
            _ => default,
        };
    }

    /// <summary>
    ///     Determines if the internally held value is of a numeric type.
    /// </summary>
    /// <returns><c>true</c> if the internally held value is of a numeric type, <c>false</c> otherwise.</returns>
    public bool IsNumeric() => Kind is ValueKind.Int or ValueKind.Float;

    /// <summary>
    ///     Attempts to retrieve a stored integer from the configurable object, if there is any.
    /// </summary>
    /// <param name="v">The retrieved value, or <c>0</c> if none is found.</param>
    /// <returns><c>true</c> if the internally held value is of type <see cref="int"/>, <c>false</c> otherwise.</returns>
    public bool TryGetInt(out int v) => TryGetValue(ValueKind.Int, in _intValue, out v);

    /// <summary>
    ///     Attempts to retrieve a stored float from the configurable object, if there is any.
    /// </summary>
    /// <param name="v">The retrieved value, or <c>0.0F</c> if none is found.</param>
    /// <returns><c>true</c> if the internally held value is of type <see cref="float"/>, <c>false</c> otherwise.</returns>
    public bool TryGetFloat(out float v) => TryGetValue(ValueKind.Float, in _floatValue, out v);

    /// <summary>
    ///     Attempts to retrieve a stored boolean from the configurable object, if there is any.
    /// </summary>
    /// <param name="v">The retrieved value, or <c>false</c> if none is found.</param>
    /// <returns><c>true</c> if the internally held value is of type <see cref="bool"/>, <c>false</c> otherwise.</returns>
    public bool TryGetBool(out bool v) => TryGetValue(ValueKind.Bool, in _boolValue, out v);

    /// <summary>
    ///     Attempts to retrieve a stored string from the configurable object, if there is any.
    /// </summary>
    /// <param name="v">The retrieved value, or <c>null</c> if none is found.</param>
    /// <returns><c>true</c> if the internally held value is of type <see cref="string"/>, <c>false</c> otherwise.</returns>
    public bool TryGetString(out string v) => TryGetNullable(ValueKind.String, in _stringValue, out v!);

    /// <summary>
    ///     Attempts to retrieve a stored number from the configurable object, if there is any.
    /// </summary>
    /// <param name="v">The retrieved value (either an <c>int</c> or <c>float</c>), or <c>null</c> if none is found.</param>
    /// <returns><c>true</c> if the internally held value is of a numeric type, <c>false</c> otherwise.</returns>
    public bool TryGetNumber(out ValueType? v)
    {
        v = TryGetInt(out int i)
            ? i
            : TryGetFloat(out float f)
                ? f
                : (ValueType?)default;

        return v is not null;
    }

    /// <summary>
    ///     Attempts to retrieve a value-type object of the given type from the provided field.
    /// </summary>
    /// <typeparam name="T">The type of the value to be retrieved.</typeparam>
    /// <param name="kind">
    ///     The kind of the value type to be retrieved.
    ///     If this instance's <see cref="Kind"/> does not match this argument, the default value for <typeparamref name="T"/> is returned.
    /// </param>
    /// <param name="holder">The field whose value will be retrieved.</param>
    /// <param name="value">The output value from this operation.</param>
    /// <returns>
    ///     The value of <paramref name="holder"/> if <paramref name="kind"/> matches the instance's <see cref="Kind"/> value,
    ///     or the default value for <typeparamref name="T"/> otherwise.
    /// </returns>
    private bool TryGetValue<T>(ValueKind kind, in T holder, out T value) where T : struct
    {
        bool hasValue = Kind == kind;

        value = hasValue ? holder : default;

        return hasValue;
    }

    /// <summary>
    ///     Attempts to retrieve a reference-type object of the given type from the provided field.
    /// </summary>
    /// <typeparam name="T">The type of the value to be retrieved.</typeparam>
    /// <param name="kind">
    ///     The kind of the reference type to be retrieved.
    ///     If this instance's <see cref="Kind"/> does not match this argument, <c>null</c> is instead returned.
    /// </param>
    /// <param name="holder">The field whose value will be retrieved.</param>
    /// <param name="value">The output value from this operation.</param>
    /// <returns>
    ///     The value of <paramref name="holder"/> if <paramref name="kind"/> matches the instance's <see cref="Kind"/> value, or <c>null</c> otherwise.
    /// </returns>
    private bool TryGetNullable<T>(ValueKind kind, in T? holder, out T? value) where T : class
    {
        bool hasValue = Kind == kind;

        value = hasValue ? holder : default;

        return hasValue && value is not null;
    }

    /// <inheritdoc/>
    public int CompareTo(object obj)
    {
        return obj is not ConfigValue other
            ? 0
            : TryGetInt(out int xi) && other.TryGetInt(out int yi)
                ? xi.CompareTo(yi)
                : TryGetFloat(out float xf) && other.TryGetFloat(out float yf)
                    ? xf.CompareTo(yf)
                    : Kind.CompareTo(other.Kind);
    }

    /// <inheritdoc/>
    public int CompareTo(ConfigValue other) => other.Kind.CompareTo(Kind);

    /// <inheritdoc/>
    public bool Equals(ConfigValue other)
    {
        return Kind == other.Kind && Kind switch
        {
            ValueKind.Int => _intValue == other._intValue,
            ValueKind.Float => _floatValue == other._floatValue,
            ValueKind.Bool => _boolValue == other._boolValue,
            ValueKind.String => _stringValue == other._stringValue,
            _ => true,
        };
    }

    /// <inheritdoc/>
    public static bool operator ==(ConfigValue x, ConfigValue y)
    {
        return x.Equals(y);
    }

    /// <inheritdoc/>
    public static bool operator !=(ConfigValue x, ConfigValue y)
    {
        return !x.Equals(y);
    }

    /// <summary>
    ///     Returns the string representation of the internally held value by this instance.
    /// </summary>
    /// <returns>The string representation of the internally held value by this instance.</returns>
    public override string ToString() => GetBoxedValue()?.ToString()!;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is ConfigValue other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (GetBoxedValue()?.GetHashCode() ?? 0) + Kind.GetHashCode();

    /// <summary>
    ///     The kind of the internally held value from the configurable struct.
    /// </summary>
    public enum ValueKind : byte
    {
        /// <summary>
        ///     The internally held value is of type <see cref="int"/>.
        /// </summary>
        Int,
        /// <summary>
        ///     The internally held value is of type <see cref="float"/>.
        /// </summary>
        Float,
        /// <summary>
        ///     The internally held value is of type <see cref="bool"/>.
        /// </summary>
        Bool,
        /// <summary>
        ///     The internally held value is of type <see cref="string"/>.
        /// </summary>
        String
    }
}