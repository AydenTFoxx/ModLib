using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModLib.Meadow;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace ModLib.Objects.Meadow;

internal static class MeadowProtectionHooks
{
    private static IDetour[]? manualHooks;

    public static void ApplyHooks()
    {
        MeadowUtils.PlayerJoinedLobby += SyncDeathProtections;

        manualHooks = [
            new Hook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.ShouldShowDeath)), IgnoreDeathMessageIfProtectedHook),
            new Hook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.CreatureDeath)), IgnoreDeathIfProtectedHook),
            new Hook(typeof(RPCs).GetMethod(nameof(RPCs.Creature_Die)), IgnoreDeathRPCIfProtectedHook),
            new ILHook(typeof(RainMeadow.RainMeadow).GetMethod("AbstractWorldEntity_Destroy", BindingFlags.NonPublic | BindingFlags.Instance), PreventCreatureDestructionILHook),
            new ILHook(typeof(RainMeadow.RainMeadow).GetMethod("Player_Destroy", BindingFlags.NonPublic | BindingFlags.Instance), PreventPlayerDestructionILHook)
        ];

        foreach (IDetour detour in manualHooks)
        {
            detour.Apply();
        }
    }

    public static void RemoveHooks()
    {
        MeadowUtils.PlayerJoinedLobby -= SyncDeathProtections;

        if (manualHooks is not null)
        {
            foreach (IDetour detour in manualHooks)
            {
                detour.Apply();
            }
            manualHooks = null;
        }
    }

    /// <summary>
    ///     Ignore cause-of-death messages for creatures under protection.
    /// </summary>
    private static bool IgnoreDeathMessageIfProtectedHook(Func<OnlinePhysicalObject, bool> orig, OnlinePhysicalObject opo) =>
        !DeathProtection.HasProtection(opo.apo.realizedObject as Creature) && orig.Invoke(opo);

    private static void IgnoreDeathIfProtectedHook(Action<Creature> orig, Creature crit)
    {
        if (!DeathProtection.HasProtection(crit))
            orig.Invoke(crit);
    }

    private static void IgnoreDeathRPCIfProtectedHook(Action<OnlinePhysicalObject, OnlinePhysicalObject> orig, OnlinePhysicalObject opo, OnlinePhysicalObject saint)
    {
        if (!DeathProtection.HasProtection(opo.apo.realizedObject as Creature))
            orig.Invoke(opo, saint);
    }

    private static void PreventCreatureDestructionILHook(ILContext context)
    {
        _ = new ILCursor(context).GotoNext(
            static x => x.MatchBrfalse(out _),
            static x => x.MatchBr(out _)
        ).MoveAfterLabels()
         .Emit(OpCodes.Ldloc_0)
         .EmitDelegate(IgnoreDestructionIfProtected);

        static bool IgnoreDestructionIfProtected(bool orig, AbstractPhysicalObject? apo)
        {
            return (apo is AbstractCreature abstractCreature && DeathProtection.HasProtection(abstractCreature.realizedCreature)) || orig;
        }
    }

    private static void PreventPlayerDestructionILHook(ILContext context)
    {
        _ = new ILCursor(context)
         .GotoNext(static x => x.MatchBrfalse(out _))
         .MoveAfterLabels()
         .Emit(OpCodes.Ldarg_2)
         .EmitDelegate(IgnoreDestructionIfProtected);

        static bool IgnoreDestructionIfProtected(bool orig, Player self)
        {
            return DeathProtection.HasProtection(self) || orig;
        }
    }

    internal static void SyncDeathProtections(OnlinePlayer player)
    {
        if (!OnlineManager.lobby.isOwner) return;

        player.SendRPCEvent(MyRPCs.SyncDeathProtections, DeathProtection.Instances.Select(static kvp => new KeyValuePair<OnlineCreature, OnlineProtectionSnapshot>(kvp.Key.abstractCreature.GetOnlineCreature()!, kvp.Value.ToOnlineSnapshot())).ToDictionary());
    }
}