using System;
using System.Collections.Generic;
using System.Linq;
using ModLib.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace ModLib.Objects;

// Notice: The following methods are not included here, but must be hooked to as well to actually prevent death:
//      Creature.Die(), UpdatableAndDeletable.Destroy(), Player.Destroy()
//
// The implementation for these hooks can be found in the Possession.PossessionHooks class
internal static class DeathProtectionHooks
{
    private static Hook[]? manualHooks;

    public static void ApplyHooks()
    {
        Extras.WrapAction(static () =>
        {
            IL.BigEel.JawsSnap += IgnoreLeviathanBiteILHook;
            IL.BigEelAI.IUseARelationshipTracker_UpdateDynamicRelationship += IgnoreProtectedCreatureILHook;

            IL.Creature.RippleViolenceCheck += NoViolenceWhileProtectedILHook;

            IL.BulletDrip.Strike += PreventRainDropStunILHook;
            IL.RoomRain.ThrowAroundObjects += PreventRoomRainPushILHook;

            IL.WormGrass.WormGrassPatch.Update += IgnoreRepulsiveCreatureILHook;
        }, Core.Logger);

        On.AbstractWorldEntity.Destroy += PreventCreatureDestructionHook;

        On.Player.Die += PlayerDeathHook;

        On.RainWorldGame.GameOver += InterruptGameOverHook;

        On.RoomRain.CreatureSmashedInGround += IgnorePlayerRainDeathHook;

        On.Watcher.WarpPoint.SpawnPendingObject += WarpDeathProtectionHook;

        manualHooks = new Hook[3];

        manualHooks[0] = new Hook(
            typeof(Creature).GetProperty(nameof(Creature.windAffectiveness)).GetGetMethod(),
            IgnoreWindAffectivenessHook);

        manualHooks[1] = new Hook(
            typeof(Creature).GetProperty(nameof(Creature.WormGrassGooduckyImmune)).GetGetMethod(),
            AvoidImmunePlayerHook);

        manualHooks[2] = new Hook(
            typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.SandstormImmune)).GetGetMethod(),
            GrantSandstormImmunityHook
        );

        foreach (Hook hook in manualHooks)
        {
            hook.Apply();
        }
    }

    public static void RemoveHooks()
    {
        Extras.WrapAction(static () =>
        {
            IL.BigEelAI.IUseARelationshipTracker_UpdateDynamicRelationship -= IgnoreProtectedCreatureILHook;
            IL.BigEel.JawsSnap -= IgnoreLeviathanBiteILHook; // No idea why yet, but this fella throws if placed before the line above?

            IL.Creature.RippleViolenceCheck -= NoViolenceWhileProtectedILHook;

            IL.BulletDrip.Strike -= PreventRainDropStunILHook;
            IL.RoomRain.ThrowAroundObjects -= PreventRoomRainPushILHook;

            IL.WormGrass.WormGrassPatch.Update -= IgnoreRepulsiveCreatureILHook;
        }, Core.Logger);

        On.AbstractWorldEntity.Destroy -= PreventCreatureDestructionHook;

        On.Player.Die -= PlayerDeathHook;

        On.RainWorldGame.GameOver -= InterruptGameOverHook;

        On.RoomRain.CreatureSmashedInGround -= IgnorePlayerRainDeathHook;

        On.Watcher.WarpPoint.SpawnPendingObject -= WarpDeathProtectionHook;

        if (manualHooks is not null)
        {
            foreach (Hook hook in manualHooks)
            {
                hook?.Undo();
            }
        }
        manualHooks = null;
    }

    /// <summary>
    /// Prevents the destruction of creatures who are under death protection.
    /// </summary>
    private static void PreventCreatureDestructionHook(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
    {
        if (self is Creature crit and not Player && TrySaveFromDestruction(crit)) return;

        orig.Invoke(self);
    }

    /// <summary>
    /// Disposes of the player's PossessionManager when Slugcat is destroyed. Also prevents death-protected players from being destroyed.
    /// </summary>
    private static void DisposePossessionManagerHook(On.Player.orig_Destroy orig, Player self)
    {
        if (TrySaveFromDestruction(self)) return;

        orig.Invoke(self);
    }

    private static void CreatureDeathHook(On.Creature.orig_Die orig, Creature self)
    {
        if (DeathProtection.TryGetProtection(self, out DeathProtection protection)
            && EvaluateDeathPrevention(protection, self))
        {
            return;
        }

        orig.Invoke(self);
    }

    /// <summary>
    /// Grants the player Worm Grass immunity when protected from death.
    /// </summary>
    private static bool AvoidImmunePlayerHook(Func<Creature, bool> orig, Creature self) =>
        (DeathProtection.TryGetProtection(self, out DeathProtection protection) && (protection.ProtectAgainstEnvironment || protection.ProtectAgainstDestruction)) || orig.Invoke(self);

    /// <summary>
    /// Makes death-immune creatures also immune to end-of-cycle sandstorms.
    /// </summary>
    private static bool GrantSandstormImmunityHook(Func<PhysicalObject, bool> orig, PhysicalObject self) =>
        self is Creature crit && DeathProtection.TryGetProtection(crit, out DeathProtection protection) && protection.ProtectAgainstEnvironment;

    /// <summary>
    /// Prevents the end of cycle rain from affecting Slugcat if protected.
    /// </summary>
    private static void IgnorePlayerRainDeathHook(On.RoomRain.orig_CreatureSmashedInGround orig, RoomRain self, Creature crit, float speed)
    {
        if (DeathProtection.TryGetProtection(crit, out DeathProtection protection) && protection.ProtectAgainstEnvironment) return;

        orig.Invoke(self, crit, speed);
    }

    /// <summary>
    /// Prevents the player from being affected by wind while protected.
    /// </summary>
    private static float IgnoreWindAffectivenessHook(Func<Creature, float> orig, Creature self) =>
        DeathProtection.TryGetProtection(self, out DeathProtection protection) && protection.ProtectAgainstEnvironment
            ? 0f
            : orig.Invoke(self);

    /// <summary>
    /// Prevents the game over screen from showing up if a player is currently being protected.
    /// </summary>
    private static void InterruptGameOverHook(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
    {
        if (ModManager.CoopAvailable && self.Players.Any(static ac => ac.realizedCreature is Player { AI: null } player && DeathProtection.HasProtection(player))) return;

        orig.Invoke(self, dependentOnGrasp);
    }

    private static void PlayerDeathHook(On.Player.orig_Die orig, Player self)
    {
        if (DeathProtection.TryGetProtection(self, out DeathProtection protection)
            && EvaluateDeathPrevention(protection, self))
        {
            return;
        }

        orig.Invoke(self);
    }

    /// <summary>
    /// Prevents a creature's abstract representation from being destroyed while death-immune.
    /// </summary>
    private static void PreventCreatureDestructionHook(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
    {
        if (self is AbstractCreature abstractCreature
            && DeathProtection.HasProtection(abstractCreature.realizedCreature)) return;

        orig.Invoke(self);
    }

    private static bool WarpDeathProtectionHook(On.Watcher.WarpPoint.orig_SpawnPendingObject orig, Watcher.WarpPoint self, AbstractPhysicalObject nextObject, bool immediateSpawn)
    {
        if (orig.Invoke(self, nextObject, immediateSpawn))
        {
            if (nextObject is AbstractCreature abstractCreature
                && DeathProtection.TryGetProtection(abstractCreature.realizedCreature, out DeathProtection protection))
            {
                protection.RemoveFromRoom();

                self.room.AddObject(protection);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Causes Leviathan bites to ignore death-protected creatures, just like it now ignores the Safari mode Overseer.
    /// </summary>
    private static void IgnoreLeviathanBiteILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(static x => x.MatchStloc(7))
         .GotoNext(MoveType.After,
            static x => x.MatchIsinst(typeof(BigEel)),
            x => x.MatchBrtrue(out target)
        ).MoveAfterLabels();

        // Target: if (!(this.room.physicalObjects[j][k] is BigEel) && ...) { ... }
        //                                                         ^ HERE (Insert)

        c.Emit(OpCodes.Ldarg_0)
         .Emit(OpCodes.Ldfld, typeof(UpdatableAndDeletable).GetField(nameof(UpdatableAndDeletable.room)))
         .Emit(OpCodes.Ldfld, typeof(Room).GetField(nameof(Room.physicalObjects)))
         .Emit(OpCodes.Ldloc_S, (byte)6)
         .Emit(OpCodes.Ldelem_Ref)
         .Emit(OpCodes.Ldloc_S, (byte)7)
         .Emit(OpCodes.Callvirt, typeof(List<PhysicalObject>).GetMethod("get_Item"))
         .Emit(OpCodes.Isinst, typeof(Creature))
         .EmitDelegate(ShouldIgnoreBite);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (!(this.room.physicalObjects[j][k] is BigEel) && !ShouldIgnoreBite(this.room.physicalObjects[j][k] as Creature) && ...) { ... }

        static bool ShouldIgnoreBite(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection)
                && (protection.ProtectAgainstViolence || protection.ProtectAgainstDestruction);
        }
    }

    /// <summary>
    ///     Causes Leviathans to ignore creatures under death protection.
    /// </summary>
    private static void IgnoreProtectedCreatureILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(MoveType.After,
            static x => x.MatchCallvirt(typeof(BigEel).GetMethod(nameof(BigEel.AmIHoldingCreature))),
            x => x.MatchBrtrue(out target)
        ).MoveAfterLabels();

        // Target: if (this.eel.AmIHoldingCreature(dRelation.trackerRep.representedCreature) || dRelation.trackerRep.representedCreature.creatureTemplate.smallCreature) { ... }
        //                                                                                 ^ HERE (Insert)

        c.Emit(OpCodes.Ldarg_1)
         .Emit(OpCodes.Ldfld, typeof(RelationshipTracker.DynamicRelationship).GetField(nameof(RelationshipTracker.DynamicRelationship.trackerRep)))
         .Emit(OpCodes.Ldfld, typeof(Tracker.CreatureRepresentation).GetField(nameof(Tracker.CreatureRepresentation.representedCreature)))
         .Emit(OpCodes.Callvirt, typeof(AbstractCreature).GetProperty(nameof(AbstractCreature.realizedCreature)).GetGetMethod())
         .EmitDelegate(ShouldIgnoreCreature);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (this.eel.AmIHoldingCreature(dRelation.trackerRep.representedCreature) || DeathProtection.HasProtection(dRelation.trackerRep.representedCreature.realizedCreature) || dRelation.trackerRep.representedCreature.creatureTemplate.smallCreature) { ... }

        static bool ShouldIgnoreCreature(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection)
                && (protection.ProtectAgainstViolence || protection.ProtectAgainstDestruction);
        }
    }

    /// <summary>
    /// Causes Worm Grass patches to fully ignore death-protected creatures.
    /// </summary>
    private static void IgnoreRepulsiveCreatureILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(MoveType.After,
            static x => x.MatchLdloc(0),
            x => x.MatchBrfalse(out target)
        ).MoveAfterLabels();

        // Target: if (realizedCreature != null && ...) { ... }
        //                                     ^ HERE (Insert)

        c.Emit(OpCodes.Ldloc_0)
         .EmitDelegate(ShouldIgnoreCreature);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (realizedCreature != null && !DeathProtection.HasProtection(realizedCreature) && ...) { ... }

        static bool ShouldIgnoreCreature(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection)
                && (protection.ProtectAgainstEnvironment || protection.ProtectAgainstDestruction);
        }
    }

    /// <summary>
    ///     Prevents any violence against creatures who are death-protected. Why is it an IL hook? Because silly.
    /// </summary>
    private static void NoViolenceWhileProtectedILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILCursor d = new(context);

        ILLabel? target = null;

        c.GotoNext(x => x.MatchBrfalse(out target));

        // Target: if (source != null && source.owner.abstractPhysicalObject.rippleLayer != this.abstractCreature.rippleLayer && !source.owner.abstractPhysicalObject.rippleBothSides && !this.abstractCreature.rippleBothSides) { ... }
        //                           ^ HERE (Insert)

        d.Emit(OpCodes.Ldarg_1).EmitDelegate(ShouldIgnoreCreature);
        d.Emit(OpCodes.Brtrue, target);

        // Result: if (source != null && !DeathProtection.HasProtection(source.owner as Creature) && source.owner.abstractPhysicalObject.rippleLayer != this.abstractCreature.rippleLayer && !source.owner.abstractPhysicalObject.rippleBothSides && !this.abstractCreature.rippleBothSides) { ... }

        static bool ShouldIgnoreCreature(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection) && protection.ProtectAgainstViolence;
        }
    }

    /// <summary>
    /// Prevents rain drops from stunning creatures while protected.
    /// </summary>
    private static void PreventRainDropStunILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(MoveType.After,
            static x => x.MatchIsinst(typeof(Creature)),
            x => x.MatchBrfalse(out target)
        ).MoveAfterLabels();

        // Target: if (collisionResult.chunk.owner is Creature) { ... }
        //                                                    ^ HERE (Append)

        c.Emit(OpCodes.Ldloc_2)
         .Emit(OpCodes.Ldfld, typeof(SharedPhysics.CollisionResult).GetField(nameof(SharedPhysics.CollisionResult.chunk)))
         .Emit(OpCodes.Callvirt, typeof(BodyChunk).GetProperty(nameof(BodyChunk.owner)).GetGetMethod())
         .Emit(OpCodes.Isinst, typeof(Creature))
         .EmitDelegate(ShouldIgnoreCreature);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (collisionResult.chunk.owner is Creature && !DeathProtection.HasProtection(collisionResult.chunk.owner as Creature)) { ... }

        static bool ShouldIgnoreCreature(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection) && protection.ProtectAgainstEnvironment;
        }
    }

    /// <summary>
    /// Prevents the rain from pushing and stunning protected creatures.
    /// </summary>
    private static void PreventRoomRainPushILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(MoveType.After,
            static x => x.MatchLdfld(typeof(PhysicalObject).GetField(nameof(PhysicalObject.abstractPhysicalObject))),
            static x => x.MatchLdfld(typeof(AbstractPhysicalObject).GetField(nameof(AbstractPhysicalObject.rippleLayer))),
            x => x.MatchBrtrue(out target)
        ).GotoPrev(MoveType.Before,
            static x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Watcher)))
        ).MoveAfterLabels();

        // Target: if (!ModManager.Watcher || !this.room.game.IsStorySession || this.room.physicalObjects[i][j].abstractPhysicalObject.rippleLayer == 0) { ... }
        //             ^ HERE (Prepend)

        c.Emit(OpCodes.Ldarg_0)
         .Emit(OpCodes.Ldfld, typeof(UpdatableAndDeletable).GetField(nameof(UpdatableAndDeletable.room)))
         .Emit(OpCodes.Ldfld, typeof(Room).GetField(nameof(Room.physicalObjects)))
         .Emit(OpCodes.Ldloc_0)
         .Emit(OpCodes.Ldelem_Ref)
         .Emit(OpCodes.Ldloc_1)
         .Emit(OpCodes.Callvirt, typeof(List<PhysicalObject>).GetMethod("get_Item"))
         .Emit(OpCodes.Isinst, typeof(Creature))
         .EmitDelegate(ShouldIgnoreCreature);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (!DeathProtection.HasProtection(this.room.physicalObjects[i][j] as Creature) && (!ModManager.Watcher || !this.room.game.IsStorySession || this.room.physicalObjects[i][j].abstractPhysicalObject.rippleLayer == 0)) { ... }

        static bool ShouldIgnoreCreature(Creature? creature)
        {
            return DeathProtection.TryGetProtection(creature, out DeathProtection protection) && protection.ProtectAgainstEnvironment;
        }
    }

    /// <summary>
    /// Attempts to save the given creature from being destroyed with <see cref="UpdatableAndDeletable.Destroy"/>.
    /// Most often occurs with death pits, but should also work for Leviathan bites and the likes.
    /// </summary>
    /// <param name="creature">The creature to be saved.</param>
    /// <returns><c>true</c> if the creature has been saved (in this method call or another), <c>false</c> otherwise.</returns>
    internal static bool TrySaveFromDestruction(Creature creature)
    {
        if (creature.inShortcut
            || !DeathProtection.TryGetProtection(creature, out DeathProtection protection)
            || !protection.ProtectAgainstDestruction
            || !protection.SafePos.HasValue) return false;

        if (protection.SaveCooldown > 0) return true;

        if (creature.room is null)
        {
            creature.room = creature.abstractCreature.world.GetAbstractRoom(protection.SafePos.Value.room)?.realizedRoom;

            creature.room ??= protection.room;
            creature.room ??= creature.abstractCreature.Room?.realizedRoom;

            if (creature.room is null)
            {
                Core.Logger.LogWarning($"[DeathProtection] Could not retrieve a room for {creature}; Destruction will not be avoided.");
                return false;
            }
        }

        Vector2 revivePos = creature.room.MiddleOfTile(protection.SafePos.Value);

        if (creature is Player player)
        {
            player.SuperHardSetPosition(revivePos);

            player.animation = Player.AnimationIndex.StandUp;
            player.allowRoll = 0;
            player.rollCounter = 0;
            player.rollDirection = 0;
        }
        else
        {
            foreach (BodyChunk bodyChunk in creature.bodyChunks)
            {
                bodyChunk.HardSetPosition(revivePos);
            }
        }

        Vector2 bodyVel = new(0f, 8f + creature.room.gravity);
        foreach (BodyChunk bodyChunk in creature.bodyChunks)
        {
            bodyChunk.vel = bodyVel;
        }

        if (creature.grabbedBy.Count > 0)
            creature.StunAllGrasps(80);

        creature.room.AddObject(new KarmicShockwave(creature, revivePos, 80, 48f * protection.Power, 64f * protection.Power));
        creature.room.AddObject(new Explosion.ExplosionLight(revivePos, 180f * protection.Power, 1f, 80, RainWorld.GoldRGB));

        creature.room.PlaySound(SoundID.SB_A14, creature.mainBodyChunk, false, 1f, 1.25f + (UnityEngine.Random.value * 0.5f));

        Core.Logger.LogInfo($"[DeathProtection] {creature} was saved from destruction!");
        return true;
    }

    private static bool EvaluateDeathPrevention(DeathProtection protection, Creature self)
    {
        if (protection.SaveCooldown > 0) return true;

        if (self.killTag is not null && protection.ProtectAgainstViolence)
        {
            self.StunAllGrasps(40);
            return true;
        }

        return !self.slatedForDeletetion
            && (protection.ProtectAgainstEnvironment || (protection.ProtectAgainstDestruction && protection.SafePos.HasValue));
    }
}