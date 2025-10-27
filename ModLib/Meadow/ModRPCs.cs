using System.Collections.Generic;
using ModLib.Options;
using RainMeadow;
using static ModLib.Options.OptionUtils;

namespace ModLib.Meadow;

/// <summary>
///     Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
public static class ModRPCs
{
    /// <summary>
    ///     Writes the provided system message to the player's chat.
    /// </summary>
    /// <param name="message">The message to be displayed</param>
    [SoftRPCMethod]
    public static void LogSystemMessage(string message)
    {
        ChatLogManager.LogSystemMessage(message);

        Core.Logger.LogMessage($"-> {message}");
    }

    /// <summary>
    ///     Requests the owner of the current lobby to sync their REMIX options with this client.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="onlinePlayer">The player who called this event.</param>
    [SoftRPCMethod]
    public static void RequestSyncRemixOptions(RPCEvent rpcEvent, OnlinePlayer onlinePlayer)
    {
        if (!MeadowUtils.IsHost)
        {
            Core.Logger.LogWarning("Player is not host; Cannot sync REMIX options!");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        if (SharedOptions.MyOptions.Count < 1)
        {
            SharedOptions.RefreshOptions();
        }

        Core.Logger.LogDebug($"Syncing local REMIX options with client {onlinePlayer}...");

        onlinePlayer.SendRPCEvent(SyncRemixOptions, new SerializableOptions(SharedOptions.MyOptions));
    }

    /// <summary>
    ///     Overrides the player's local <see cref="SharedOptions"/> instance with the host's own REMIX options.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="options">The serializable values of the host's <see cref="ServerOptions"/> instance.</param>
    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, SerializableOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            Core.Logger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        SharedOptions.SetOptions(options.Options);

        Core.Logger.LogInfo($"Synced REMIX options! New values are: {SharedOptions}");
    }

    /// <summary>
    ///     A serializable wrapper around a <see cref="ServerOptions"/>' local options dictionary.
    /// </summary>
    public class SerializableOptions : Serializer.ICustomSerializable
    {
        /// <summary>
        ///     The internally held option values;
        /// </summary>
        public Dictionary<string, ConfigValue> Options = [];

        /// <summary>
        ///     Creates a new <see cref="SerializableOptions"/> instance with the provided options for serialization.
        /// </summary>
        /// <remarks>
        ///     Options prefixed with an underscore (<c>_</c>) are ignored for serialization purposes.
        /// </remarks>
        /// <param name="options">The options dictionary for serialization.</param>
        public SerializableOptions(IDictionary<string, ConfigValue> options)
        {
            foreach (KeyValuePair<string, ConfigValue> optionPair in options)
            {
                if (optionPair.Key.StartsWith("_", System.StringComparison.Ordinal)) continue;

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
        public void CustomSerialize(Serializer serializer) => serializer.Serialize(ref Options);
    }
}