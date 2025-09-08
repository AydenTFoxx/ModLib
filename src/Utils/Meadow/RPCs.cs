using System;
using System.Linq;
using RainMeadow;
using MyMod.Utils.Options;
using static MyMod.Utils.Options.OptionUtils;

namespace MyMod.Utils.Meadow;

/// <summary>
/// Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
public static class MyRPCs
{
    [SoftRPCMethod]
    public static void RequestRemixOptionsSync(RPCEvent rpcEvent, OnlinePlayer onlinePlayer)
    {
        if (!MeadowUtils.IsHost)
        {
            MyLogger.LogWarning("Player is not host; Cannot sync options with other players!");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        if (SharedOptions.MyOptions.Count < 1)
        {
            SharedOptions.RefreshOptions(true);
        }

        MyLogger.LogInfo($"Syncing REMIX options with player {onlinePlayer}...");

        onlinePlayer.SendRPCEvent(SyncRemixOptions, new OnlineServerOptions());
    }

    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, OnlineServerOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            MyLogger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        SharedOptions.SetOptions(options);

        MyLogger.LogInfo($"Synced REMIX options! New values are: {SharedOptions}");
    }

    public static void SendCreatureRPC<T>(Creature creature, T @delegate, params object[] args)
        where T : Delegate
    {
        OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature();

        if (onlineCreature is null) return;

        args = [.. args.Prepend(onlineCreature)];

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (onlinePlayer.isMe) continue;

            onlinePlayer.SendRPCEvent(@delegate, args);
        }
    }

    public static RPCEvent SendRPCEvent<T>(this OnlinePlayer onlinePlayer, T @delegate, params object[] args)
        where T : Delegate
    {
        RPCEvent rpcEvent = onlinePlayer
            .InvokeOnceRPC(typeof(MyRPCs).GetMethod(@delegate.Method.Name).CreateDelegate(typeof(T)), args)
            .Then(ResolveRPCEvent)
            .SetTimeout(1000);

        MyLogger.LogDebug($"Sending RPC event {rpcEvent} to {rpcEvent.to}...");

        return rpcEvent;
    }

    public static void ResolveRPCEvent(GenericResult result)
    {
        switch (result)
        {
            case GenericResult.Ok:
                MyLogger.LogInfo($"Successfully delivered RPC {result.referencedEvent} to {result.to}.");
                break;
            case GenericResult.Fail:
                MyLogger.LogWarning($"Could not run RPC {result.referencedEvent} as {result.to}.");
                break;
            default:
                MyLogger.LogWarning($"Failed to deliver RPC {result.referencedEvent} to {result.to}!");
                break;
        }

        if (result.referencedEvent is RPCEvent rpcEvent)
        {
            rpcEvent.RemoveTimeout();
        }
    }

    /// <summary>
    /// An online variant of <see cref="ServerOptions"/> which can be serialized by Rain Meadow.
    /// </summary>
    public class OnlineServerOptions : ServerOptions, Serializer.ICustomSerializable
    {
        public void CustomSerialize(Serializer serializer) =>
            serializer.Serialize(ref SharedOptions.MyOptions);

        public override string ToString() => $"{nameof(OnlineServerOptions)} => {FormatOptions()}";
    }
}