using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModLib.Meadow;
using RainMeadow;

namespace ModLib.Objects.Meadow;

internal static class MyRPCs
{
    [RPCMethod(security = RPCSecurity.InLobby)]
    public static void SyncDeathProtections(RPCEvent rpcEvent, SerializableDictionary<OnlineCreature, OnlineDeathProtection> data)
    {
        ThrowIfTrue(OnlineManager.lobby.isOwner || OnlineManager.lobby.owner != rpcEvent.from, $"Received invalid OnlineProtections sync request; Ignoring. (Reason: {(OnlineManager.lobby.isOwner ? "I'm host" : $"{rpcEvent.from} is not host")})");

        Main.Logger.LogInfo($"Syncing local OnlineProtections data with {rpcEvent.from}!");

        DeathProtection.SetInstances(data.ToLocalCollection(OnlineToLocalProtection));
    }

    [RPCMethod(security = RPCSecurity.InLobby)]
    public static void RequestProtection(RPCEvent rpcEvent, OnlineDeathProtection onlineProtection)
    {
        if (!Extras.InGameSession) return;

        Main.Logger.LogInfo($"Received protection object from {rpcEvent.from}! Adding to collection... (Target: {onlineProtection.Target})");

        DeathProtection.AddInstance(onlineProtection.Target.realizedCreature, onlineProtection.ToLocalProtection());
    }

    [RPCMethod(security = RPCSecurity.InLobby)]
    public static void StopProtection(RPCEvent rpcEvent, OnlineDeathProtection onlineProtection)
    {
        if (!Extras.InGameSession) return;

        Main.Logger.LogInfo($"Stopping protection of {onlineProtection.Target}! (Requested by: {rpcEvent.from})");

        onlineProtection.ToLocalProtection().Destroy();
    }

    internal static KeyValuePair<Creature, DeathProtection> OnlineToLocalProtection(KeyValuePair<OnlineCreature, OnlineDeathProtection> onlinePair)
    {
        return new KeyValuePair<Creature, DeathProtection>(
            onlinePair.Key.realizedCreature ?? throw new ArgumentException($"Could not find realized creature of {onlinePair.Key}"),
            onlinePair.Value.ToLocalProtection()
        );
    }

    internal static KeyValuePair<OnlineCreature, OnlineDeathProtection> LocalToOnlineProtection(KeyValuePair<Creature, DeathProtection> localPair)
    {
        return new KeyValuePair<OnlineCreature, OnlineDeathProtection>(
            localPair.Key.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not find online representation for {localPair.Key}"),
            localPair.Value.ToOnlineProtection()
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfTrue(bool condition, string message)
    {
        if (condition)
        {
            Main.Logger.LogWarning(message);

            throw new InvalidProgrammerException(message);
        }
    }
}