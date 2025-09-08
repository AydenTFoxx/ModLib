using System.Collections.Generic;
using MyMod.Utils.Generics;
using RainMeadow;

namespace MyMod.Utils.Meadow;

/// <summary>
/// A simple tracker of sent RPC events, used to prevent unresolved SoftRPCs from hanging around indefinitely.
/// </summary>
public static class RPCManager
{
    private static readonly WeakDictionary<RPCEvent, RPCTimeout> _activeRPCs = [];

    public static void RemoveTimeout(this RPCEvent self)
    {
        if (!TryGetTimeout(self, out _)) return;

        _activeRPCs.Remove(self);
    }

    public static RPCEvent SetTimeout(this RPCEvent self, int lifetime)
    {
        if (TryGetTimeout(self, out RPCTimeout timeout))
        {
            timeout.Lifetime = lifetime;
        }
        else
        {
            _activeRPCs[self] = new(lifetime);
        }

        return self;
    }

    public static bool TryGetTimeout(this RPCEvent self, out RPCTimeout timeout) =>
        _activeRPCs.TryGetValue(self, out timeout);

    public static void UpdateRPCs()
    {
        if (_activeRPCs.Count < 1) return;

        foreach (KeyValuePair<RPCEvent, RPCTimeout> managedRPC in _activeRPCs)
        {
            managedRPC.Value.Lifetime--;

            if (managedRPC.Value.Lifetime < 1)
            {
                Logger.LogWarning($"RPC event {managedRPC.Key} failed to be delivered; Timed out waiting for response.");

                managedRPC.Key.Abort();

                _activeRPCs.Remove(managedRPC.Key);
            }
        }
    }

    public class RPCTimeout(int lifetime)
    {
        public int Lifetime { get; set; } = lifetime;
    }
}