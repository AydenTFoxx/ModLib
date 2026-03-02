using System;

namespace ModLib.Options;

/// <summary>
///     Determines a given REMIX option is not to be synced in a Rain Meadow lobby.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}