using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
internal static class ModRPCs
{
    /// <summary>
    ///     Writes the provided system message to the player's chat.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="message">The message to be displayed</param>
    [SoftRPCMethod]
    public static GenericResult LogSystemMessage(RPCEvent rpcEvent, string message)
    {
        ChatLogManager.LogSystemMessage(message);

        Core.Logger.LogMessage($"-> {message}");

        return new GenericResult.Ok(rpcEvent);
    }

    /// <summary>
    ///     Overrides the player's local <see cref="SharedOptions"/> collection with the host's own REMIX options.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="onlineOptions">The serializable values of the host's <see cref="SharedOptions"/> collection.</param>
    [SoftRPCMethod]
    public static GenericResult SyncRemixOptions(RPCEvent rpcEvent, SerializableOptions onlineOptions)
    {
        if (OnlineManager.lobby.isOwner || OnlineManager.lobby.owner != rpcEvent.from || onlineOptions.Options.Count is 0)
            return new GenericResult.Fail(rpcEvent);

        SharedOptions.SetOptions(onlineOptions.Options);

        Core.Logger.LogInfo($"Synced REMIX options with {rpcEvent.from}! New values are: {SharedOptions.FormatOptions()}");

        return new GenericResult.Ok(rpcEvent);
    }
}