using System.Collections.Generic;
using ModLib.Meadow;
using MoreSlugcats;
using RainMeadow;
using UnityEngine;

namespace ModLib.Objects.Meadow;

internal static class MyRPCs
{
    [SoftRPCMethod]
    public static GenericResult SyncDeathProtections(RPCEvent rpcEvent, SerializableDictionary<OnlineCreature, OnlineProtectionSnapshot> data)
    {
        if (OnlineManager.lobby is null or { isOwner: true } || OnlineManager.lobby.owner != rpcEvent.from)
            return new GenericResult.Fail(rpcEvent);

        foreach (KeyValuePair<OnlineCreature, OnlineProtectionSnapshot> kvp in data)
        {
            DeathProtection.RestoreFromSnapshot(kvp.Key.realizedCreature, kvp.Value);
        }

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestNewProtection(RPCEvent rpcEvent, OnlineCreature onlineCreature, ushort lifespan, WorldCoordinate? safePos)
    {
        if (!onlineCreature.isMine || DeathProtection.HasProtection(onlineCreature.realizedCreature))
            return new GenericResult.Fail(rpcEvent);

        DeathProtection.CreateInstance(onlineCreature.realizedCreature, lifespan, safePos);

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestStopProtection(RPCEvent rpcEvent, OnlineCreature onlineCreature)
    {
        if (!onlineCreature.isMine
            || !DeathProtection.TryGetProtection(onlineCreature.realizedCreature, out DeathProtection protection)
            || protection.slatedForDeletetion)
        {
            return new GenericResult.Fail(rpcEvent);
        }

        protection.DestroyInternal(true);

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult SyncNewProtection(RPCEvent rpcEvent, OnlineCreature onlineCreature, OnlineProtectionSnapshot snapshot)
    {
        if (onlineCreature.isMine || onlineCreature.owner != rpcEvent.from || DeathProtection.HasProtection(onlineCreature.realizedCreature))
            return new GenericResult.Fail(rpcEvent);

        DeathProtection.RestoreFromSnapshot(onlineCreature.realizedCreature, snapshot);

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult SyncStopProtection(RPCEvent rpcEvent, OnlineCreature onlineCreature)
    {
        if (onlineCreature.isMine
            || onlineCreature.owner != rpcEvent.from
            || !DeathProtection.TryGetProtection(onlineCreature.realizedCreature, out DeathProtection protection)
            || protection.slatedForDeletetion)
        {
            return new GenericResult.Fail(rpcEvent);
        }

        protection.DestroyInternal(true);

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestSaveFromDestruction(RPCEvent rpcEvent, OnlineCreature onlineCreature)
    {
        return onlineCreature.isMine && DeathProtection.Hooks.TrySaveFromDestruction(onlineCreature.realizedCreature)
            ? new GenericResult.Ok(rpcEvent)
            : new GenericResult.Fail(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult SyncSaveFromDestruction(RPCEvent rpcEvent, OnlineCreature onlineCreature, Vector2 revivePos)
    {
        if (onlineCreature.isMine || onlineCreature.owner != rpcEvent.from || !DeathProtection.TryGetProtection(onlineCreature.realizedCreature, out DeathProtection protection))
            return new GenericResult.Fail(rpcEvent);

        DeathProtection.Hooks.DisplaySavingThrowEffects(onlineCreature.realizedCreature, protection, revivePos);

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestCreatureRevival(RPCEvent rpcEvent, OnlineCreature onlineCreature)
    {
        return onlineCreature.isMine && RevivalHelper.ReviveCreature(onlineCreature.realizedCreature)
            ? new GenericResult.Ok(rpcEvent)
            : new GenericResult.Fail(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestOracleRevival(RPCEvent rpcEvent, OnlinePhysicalObject onlineOracle)
    {
        return onlineOracle.isMine && onlineOracle.apo.realizedObject is Oracle oracle && RevivalHelper.ReviveOracle(oracle)
            ? new GenericResult.Ok(rpcEvent)
            : new GenericResult.Fail(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestRevivePebbles(RPCEvent rpcEvent, OnlinePhysicalObject opo)
    {
        if (!OnlineManager.lobby.isOwner || opo.apo.realizedObject is not Oracle oracle || oracle.room?.game.session is not StoryGameSession storySession)
            return new GenericResult.Fail(rpcEvent);

        if (ModManager.MSC)
            storySession.saveState.miscWorldSaveData.halcyonStolen = (oracle.oracleBehavior as CLOracleBehavior)?.halcyon is null;

        storySession.saveState.deathPersistentSaveData.ripPebbles = false;

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestReviveMoon(RPCEvent rpcEvent, OnlinePhysicalObject opo)
    {
        if (!OnlineManager.lobby.isOwner || opo.apo.realizedObject is not Oracle oracle || oracle.room?.game.session is not StoryGameSession storySession)
            return new GenericResult.Fail(rpcEvent);

        storySession.saveState.deathPersistentSaveData.ripMoon = false;

        return new GenericResult.Ok(rpcEvent);
    }

    [SoftRPCMethod]
    public static GenericResult RequestRemoveFromRespawnList(RPCEvent rpcEvent, OnlineCreature onlineCreature)
    {
        if (!OnlineManager.lobby.isOwner) return new GenericResult.Fail(rpcEvent);

        RevivalHelper.RemoveFromRespawnList(onlineCreature.realizedCreature);

        return new GenericResult.Ok(rpcEvent);
    }
}