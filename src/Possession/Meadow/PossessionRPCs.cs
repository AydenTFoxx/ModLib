using System;
using RainMeadow;
using Random = UnityEngine.Random;
using Martyr.Utils.Options;
using static Martyr.Utils.OptionUtils;
using System.Linq;

namespace Martyr.Possession.Meadow;

public static class PossessionRPCs
{
    [SoftRPCMethod]
    public static void ApplyPossessionEffects(RPCEvent rpcEvent, OnlineCreature onlineTarget, bool isPossession)
    {
        if (onlineTarget.realizedCreature is not Creature target || target.room is null)
        {
            MyLogger.LogWarning($"Target or room is invalid; Target: {onlineTarget.realizedCreature} | Room: {onlineTarget.realizedCreature?.room}");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        if (isPossession)
        {
            target.room.AddObject(new TemplarCircle(target, target.mainBodyChunk.pos, 48f, 8f, 2f, 12, true));
            target.room.AddObject(new ShockWave(target.mainBodyChunk.pos, 100f, 0.08f, 4, false));
            target.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, target.mainBodyChunk, loop: false, 1f, 1.25f + (Random.value * 1.25f));
        }
        else
        {
            target.room.AddObject(new ReverseShockwave(target.mainBodyChunk.pos, 64f, 0.05f, 24));
            target.room.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);
        }
    }

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
    public static void SetCreatureControl(RPCEvent rpcEvent, OnlineCreature onlineTarget, bool controlled)
    {
        if (onlineTarget.realizedCreature is not Creature target)
        {
            MyLogger.LogWarning($"{onlineTarget} is not a controllable creature.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        target.abstractCreature.controlled = controlled;

        MyLogger.LogInfo($"{target} is {(controlled ? "now" : "no longer")} being controlled by {rpcEvent.from}.");
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

    internal static RPCEvent SendRPCEvent<T>(this OnlinePlayer onlinePlayer, T @delegate, params object[] args)
        where T : Delegate
    {
        RPCEvent rpcEvent = onlinePlayer
            .InvokeOnceRPC(typeof(PossessionRPCs).GetMethod(@delegate.Method.Name).CreateDelegate(typeof(T)), args)
            .Then(ResolveRPCEvent)
            .SetTimeout(1000);

        MyLogger.LogDebug($"Sending RPC event {rpcEvent} to {rpcEvent.to}...");

        return rpcEvent;
    }

    private static void ResolveRPCEvent(GenericResult result)
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

    public class OnlineServerOptions : ServerOptions, Serializer.ICustomSerializable
    {
        public void CustomSerialize(Serializer serializer) =>
            serializer.Serialize(ref SharedOptions.MyOptions);

        public override string ToString() => $"{nameof(OnlineServerOptions)} => {FormatOptions()}";
    }
}