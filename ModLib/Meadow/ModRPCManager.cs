using System;
using System.Collections.Generic;
using System.Reflection;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     A simple tracker of sent RPC events, used to prevent unresolved SoftRPCs from hanging around indefinitely.
/// </summary>
public static class ModRPCManager
{
    internal static List<RPCTimeout> _activeRPCs = [];

    private static void RemoveTimeout(this RPCEvent self)
    {
        if (!TryGetTimeout(self, out RPCTimeout timeout)) return;

        _activeRPCs.Remove(timeout);
    }

    private static RPCEvent SetTimeout(this RPCEvent self, int lifetime)
    {
        if (TryGetTimeout(self, out RPCTimeout timeout))
        {
            timeout.Lifetime = lifetime;
        }
        else
        {
            _activeRPCs.Add(new RPCTimeout(self, lifetime));
        }

        return self;
    }

    private static bool TryGetTimeout(this RPCEvent self, out RPCTimeout timeout)
    {
        timeout = _activeRPCs.Find(t => t.Source == self);

        return timeout is not null;
    }

    /// <summary>
    ///     Clears all unresolved RPCs from the manager.
    /// </summary>
    internal static void ClearRPCs() => _activeRPCs.Clear();

    /// <summary>
    ///     Updates all pending <see cref="RPCEvent"/> instances, removing them on expiration.
    /// </summary>
    internal static void UpdateRPCs()
    {
        if (_activeRPCs.Count is 0) return;

        for (int i = _activeRPCs.Count - 1; i >= 0; i--)
        {
            _activeRPCs[i].Update();
        }

        _activeRPCs.RemoveAll(static t => t.IsExpired);
    }

    /// <summary>
    ///     Sends an RPC event to the online player, which is automatically aborted if the recipient does not answer after a certain time limit.
    /// </summary>
    /// <param name="onlinePlayer">The recipient who will receive this RPC event.</param>
    /// <param name="delegate">The RPC delegate to be sent.</param>
    /// <param name="args">Any arguments of the RPC method.</param>
    /// <returns>The <see cref="RPCEvent"/> instance sent to the online player, or <c>null</c> if the RPC fails to be delivered.</returns>
    public static RPCEvent? SendRPCEvent<T>(this OnlinePlayer onlinePlayer, T @delegate, params object[] args)
        where T : Delegate
    {
        try
        {
            RPCEvent rpcEvent = onlinePlayer
                .InvokeOnceRPC(@delegate.Method.DeclaringType.GetMethod(@delegate.Method.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).CreateDelegate(typeof(T)), args)
                .Then(ResolveRPCEvent)
                .SetTimeout(30 * 40); // 30s time limit

            Core.Logger.LogDebug($"Sending RPC event {rpcEvent} to {rpcEvent.to}...");

            return rpcEvent;
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Could not send RPC to {onlinePlayer}! {ex}");

            return null;
        }
    }

    /// <summary>
    ///     Sends a single RPC event to all players in the same room as the online entity.
    /// </summary>
    /// <param name="source">The online entity who will send the RPC event.</param>
    /// <param name="del">The RPC delegate to be sent.</param>
    /// <param name="args">Any arguments of the RPC method.</param>
    public static void BroadcastOnceRPCInRoom<T>(this OnlineEntity source, T del, params object[] args)
        where T : Delegate
    {
        if (source.currentlyJoinedResource is not RoomSession roomSession) return;

        foreach (OnlinePlayer participant in roomSession.participants)
        {
            if (participant.isMe) continue;

            participant.SendRPCEvent(del, args);
        }
    }

    /// <summary>
    ///     Sends a single RPC event to all players in the current lobby.
    /// </summary>
    /// <param name="del">The RPC method to be sent.</param>
    /// <param name="args">Any arguments of the RPC method.</param>
    public static void BroadcastOnceRPCInLobby<T>(T del, params object[] args)
        where T : Delegate
    {
        if (OnlineManager.lobby is null) return;

        foreach (OnlinePlayer participant in OnlineManager.lobby.participants)
        {
            if (participant.isMe) continue;

            participant.SendRPCEvent(del, args);
        }
    }

    /// <summary>
    ///     Logs the result of the resolved RPC event, then removes its timeout irrespective of its result.
    /// </summary>
    /// <param name="result">The result of the resolved RPC event.</param>
    public static void ResolveRPCEvent(GenericResult result)
    {
        switch (result)
        {
            case GenericResult.Ok:
                Core.Logger.LogInfo($"Successfully delivered RPC {result.referencedEvent} to {result.from}.");
                break;
            case GenericResult.Fail:
                Core.Logger.LogWarning($"Could not run RPC {result.referencedEvent} as {result.from}.");
                break;
            default:
                Core.Logger.LogWarning($"Failed to deliver RPC {result.referencedEvent} to {result.from}!");
                break;
        }

        if (result.referencedEvent is RPCEvent rpcEvent)
        {
            rpcEvent.RemoveTimeout();
        }
    }

    /// <summary>
    ///     A self-contained timer which automatically aborts a given RPC once its internal timer runs out.
    /// </summary>
    /// <param name="Source">The RPC event this timeout is tied to.</param>
    /// <param name="Lifetime">The duration of the internal timer.</param>
    internal sealed record RPCTimeout(RPCEvent Source, int Lifetime)
    {
        public RPCEvent Source { get; } = Source;

        public int Lifetime { get; set; } = Lifetime;
        public bool IsExpired { get; private set; }

        public void Update()
        {
            if (IsExpired) return;

            if (Source.aborted)
            {
                IsExpired = true;
                return;
            }

            Lifetime--;

            if (Lifetime < 1)
            {
                Core.Logger.LogWarning($"RPC event {Source} failed to be delivered; Timed out waiting for response.");

                Source.Abort();

                IsExpired = true;
            }
        }

        public void Abort() => Lifetime = 0;
    }
}