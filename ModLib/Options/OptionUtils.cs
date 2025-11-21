using System.Runtime.CompilerServices;
using ModLib.Loader;

namespace ModLib.Options;

/// <summary>
///     Utility methods for retrieving the mod's REMIX options.
/// </summary>
/// <remarks>This also allows for overriding the player's local options without touching their REMIX values.</remarks>
public static class OptionUtils
{
    /// <summary>
    ///     The client's local <see cref="ServerOptions"/> instance, overriden when joining an online lobby.
    /// </summary>
    public static ServerOptions SharedOptions { get; } = new();

    static OptionUtils()
    {
        Entrypoint.TryInitialize();
    }

    /// <summary>
    ///     Directly requests for the client's REMIX options, then retrieves its value.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <returns>The configured value for the given option.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetClientOptionValue<T>(Configurable<T>? option) => option is not null ? option.Value : default;

    /// <summary>
    ///     Directly requests for the client's REMIX options, then determines whether it is enabled or not.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClientOptionEnabled(Configurable<bool>? option) => option?.Value ?? false;

    /// <summary>
    ///     Directly requests for the client's REMIX options, then compares its values to the provided argument.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClientOptionValue<T>(Configurable<T>? option, T value) => option?.Value?.Equals(value) ?? false;

    /// <summary>
    ///     Determines if a given option is enabled in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <returns>The local value for the given option.</returns>
    /// <seealso cref="GetClientOptionValue{T}(Configurable{T}?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOptionValue<T>(Configurable<T>? option) => GetOptionValue<T>(option?.key ?? "none");

    /// <summary>
    ///     Determines if a given option is enabled in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns>The configured value for the given option.</returns>
    /// <seealso cref="IsClientOptionEnabled(Configurable{bool}?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptionEnabled(Configurable<bool>? option) => IsOptionEnabled(option?.key ?? "none");

    /// <summary>
    ///     Determines if a given option has the provided value in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    /// <seealso cref="IsClientOptionValue{T}(Configurable{T}?, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptionValue<T>(Configurable<T>? option, T value) => IsOptionValue(option?.key ?? "none", value);

    /// <summary>
    ///     Retrieves the value of the given option from the local <c>SharedOptions</c> property.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns>The value stored in the local <c>SharedOptions</c> property.</returns>
    public static T? GetOptionValue<T>(string option) =>
        SharedOptions.MyOptions.TryGetValue(option, out ConfigValue value)
            ? (T?)value.GetBoxedValue() : default;

    /// <summary>
    ///     Determines if the local <c>SharedOptions</c> property has the given option enabled.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    public static bool IsOptionEnabled(string option) =>
        SharedOptions.MyOptions.TryGetValue(option, out ConfigValue value) && value.TryGetBool(out bool v) && v;

    /// <summary>
    ///     Determines if the local <c>SharedOptions</c> property has the given option set to the provided value.
    /// </summary>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The name of the option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    public static bool IsOptionValue<T>(string option, T value) =>
        SharedOptions.MyOptions.TryGetValue(option, out ConfigValue v) && (v.GetBoxedValue()?.Equals(value) ?? false);
}