using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ModLib.Collections;
using ModLib.Extensions;
using ModLib.Meadow;
using ModLib.Objects.Meadow;
using MoreSlugcats;
using RainMeadow;
using UnityEngine;
using Watcher;

namespace ModLib.Objects;

/// <summary>
///     Prevents all death of its target creature for a configurable delay and/or condition.
///     This class cannot be inherited.
/// </summary>
public sealed partial class DeathProtection : GlobalUpdatableAndDeletable
{
    private const byte MAX_SAVING_THROWS = 64;
    private const byte MAX_REVIVALS = 20;

    private const int MAX_STUN_DURATION = 80;

    private static readonly WeakDictionary<Creature, DeathProtection> _activeInstances = [];

    /// <summary>
    ///
    /// </summary>
    public static readonly ReadOnlyDictionary<Creature, DeathProtection> Instances = new(_activeInstances);

    /// <summary>
    ///     The targeted creature.
    /// </summary>
    public Creature Target { get; }

    /// <summary>
    ///     The last known position considered "safe"; If the protected creature is destroyed, it is instead teleported to this position.
    /// </summary>
    public WorldCoordinate? SafePos { get; internal set; }

    /// <summary>
    ///     If greater than <c>0</c>, a cooldown where the creature is assumed to be alive; Prevents repeated attempts to revive the target causing no revival at all.
    /// </summary>
    public byte SaveCooldown { get; internal set; }

    /// <summary>
    ///     The amount of times the protection has saved Target from destruction in a row. If exceeding a given limit, the protection is destroyed.
    /// </summary>
    public byte SavingThrows { get; internal set; }

    /// <summary>
    ///     The amount of times the protection will attempt to revive its target creature. If this number reaches zero, the protection is destroyed.
    /// </summary>
    public byte RevivalsLeft { get; internal set; } = MAX_REVIVALS;

    /// <summary>
    ///     An optional lifespan for the protection. If a condition is also provided, this acts as the "grace" timer before the protection can actually expire.
    /// </summary>
    public ushort Lifespan { get; internal set; }

    /// <summary>
    ///     If true, lifespan is ignored and the protection lasts until manually destroyed.
    /// </summary>
    public bool IsInfinite { get; }

    /// <summary>
    ///     If true, the protection instance will be saved to disk whenever a cycle is complete.
    /// </summary>
    public bool IsPersistent { get; }

    /// <summary>
    ///     The power value for visual effects.
    /// </summary>
    internal float Power { get; }

    /// <summary>
    ///     If true, deep bodies of water are considered as "safe positions" for returning Target in case of destruction.
    /// </summary>
    private readonly bool isWaterBreathingCrit;

    /// <summary>
    ///     The BodyChunk to use as reference for setting and retrieving the target creature's "safe position".
    /// </summary>
    private readonly byte safeChunkIndex;

    /// <summary>
    ///     The natural immunities Target had prior to being protected, in the following order: Hypothermia, Lava, Tentacle.
    ///     When the protection is removed, their respective fields are restored to their original values.
    /// </summary>
    private readonly bool[] originalImmunities;

    /// <summary>
    ///     Creates a new death protection instance which lasts for the specified amount of time.
    /// </summary>
    /// <param name="target">The creature to protect.</param>
    /// <param name="lifespan">The amount of time to wait before the protection can be removed.</param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="isPersistent">If the protection instance should persist across cycles.</param>
    internal DeathProtection(Creature target, ushort lifespan, WorldCoordinate? safePos, bool isPersistent)
    {
        Target = target;
        Power = Mathf.Clamp(target.TotalMass / target.Template.bodySize, 0.5f, 5f);

        Lifespan = lifespan;
        IsInfinite = !Extras.IsLocalObject(target) || lifespan == 0;

        IsPersistent = isPersistent;

        SafePos = safePos ?? target.abstractCreature.pos;

        isWaterBreathingCrit = target.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious
                            || target.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly;

        safeChunkIndex = (byte)(Target is Player ? 1 : Target.mainBodyChunkIndex);

        originalImmunities = [target.abstractCreature.HypothermiaImmune, target.abstractCreature.lavaImmune, target.abstractCreature.tentacleImmune];

        ToggleImmunities(target.abstractCreature, true);
    }

    /// <inheritdoc/>
    public override void Update(bool eu)
    {
        base.Update(eu);

        if (Target is null)
        {
            Main.Logger.LogWarning($"Protection target was destroyed while being protected! Removing protection object.");
            DestroyInternal(false);
            return;
        }

        Target.rainDeath = 0f;

        if (Target.stun > MAX_STUN_DURATION)
            Target.stun = MAX_STUN_DURATION;

        if (Target is Player player)
        {
            player.airInLungs = 1f;

            if (ModManager.MSC)
                player.playerState.permanentDamageTracking = 0d;

            if (ModManager.Watcher)
            {
                player.rippleDeathTime = 0;
                player.rippleDeathIntensity = 0f;
            }
        }
        else if (Target is AirBreatherCreature airBreatherCreature)
            airBreatherCreature.lungs = 1f;

        if (ModManager.Watcher)
            Target.repelLocusts = Math.Max(Target.repelLocusts, 10 * Math.Max(Lifespan, (short)2));

        if (Target.grabbedBy.Count > 0)
        {
            for (int i = Target.grabbedBy.Count - 1; i >= 0; i--)
            {
                Creature.Grasp grasp = Target.grabbedBy[i];

                if (grasp is null or { grabber: null or Player }) return;

                grasp.grabber.ReleaseGrasp(grasp.graspUsed);
                grasp.grabber.Stun(Math.Min(Target.stun + 20, MAX_STUN_DURATION));
            }
        }

        if (Target.State is HealthState healthState)
        {
            healthState.health = 1f;
            healthState.alive = true;
        }

        if (Target.room is not null)
        {
            foreach (RoomCamera camera in Target.room.game.cameras)
            {
                camera.hud?.textPrompt?.gameOverMode = false;
            }

            if (SafePos is null || ShouldUpdateSafePos())
            {
                SafePos = Target.room.GetWorldCoordinate(Target.bodyChunks[safeChunkIndex].pos);

                if (SavingThrows > 0)
                    SavingThrows--;
            }

            float num6 = -Target.bodyChunks[0].restrictInRoomRange + 1f;
            if (Target is Player plr && Target.bodyChunks[0].restrictInRoomRange == Target.bodyChunks[0].defaultRestrictInRoomRange)
            {
                num6 = plr.bodyMode == Player.BodyModeIndex.WallClimb
                    ? Mathf.Max(num6, -250f)
                    : Mathf.Max(num6, -500f);
            }

            if (Target.bodyChunks[0].pos.y < (num6 + 200f)
                && this is { Target.room: not { water: true, waterInverted: false, defaultWaterLevel: >= -10 }, Target: not { Template.canFly: true, Stunned: false, dead: false } })
            {
                Hooks.TrySaveFromDestruction(Target);
            }
        }
        else if (this is { Target.inShortcut: false, SafePos: not null })
        {
            if (SavingThrows < MAX_SAVING_THROWS)
            {
                Main.Logger.LogWarning($"{Target} not found in a room while being protected! Performing saving throw to prevent destruction.");

                Hooks.TrySaveFromDestruction(Target);

                SavingThrows += 4;
            }
            else
            {
                Main.Logger.LogWarning($"{Target} exceeded limit of destruction-saving attempts, destroying protection instead.");

                Destroy();
                return;
            }
        }

        if (SaveCooldown > 0)
            SaveCooldown--;

        if (Target.dead)
        {
            bool canRevive = this is { RevivalsLeft: > 0, SaveCooldown: 0 };

            Main.Logger.LogWarning($"{Target} was killed while protected! Will revive? {canRevive}");

            if (canRevive)
            {
                RevivalHelper.ReviveCreature(Target);
                SaveCooldown = 10;
            }
            else
            {
                Destroy();
            }

            return;
        }

        if (IsInfinite) return;

        if (Lifespan > 0)
        {
            Lifespan--;
        }
        else if (Target.stun == 0 && (Target is Player plr ? plr is { canJump: > 0 } or { Submersion: >= 0.5f } : Target is { Submersion: >= 0.5f } || Target.IsTileSolid(safeChunkIndex, 0, -1)))
        {
            Destroy();
        }
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        if (Target is null || Extras.IsLocalObject(Target))
            DestroyInternal(true);
        else
            RainMeadowAccess.RequestStopProtection(Target);
    }

    internal void DestroyInternal(bool warnIfNull)
    {
        base.Destroy();

        if (Target is not null)
        {
            ToggleImmunities(Target.abstractCreature, false);

            _activeInstances.Remove(Target);

            if (Extras.IsOnlineSession && MeadowUtils.IsMine(Target))
                RainMeadowAccess.SyncStopProtection(Target);

            Main.Logger.LogDebug($"{Target} is no longer being protected.");
        }
        else
        {
            if (warnIfNull)
                Main.Logger.LogWarning($"Protection object was destroyed while Target was null! Attempting to remove instance directly.");

            foreach (KeyValuePair<Creature, DeathProtection> kvp in _activeInstances)
            {
                if (kvp.Value == this)
                {
                    Main.Logger.LogInfo($"Removing detached protection instance from: {kvp.Key}");

                    _activeInstances.Remove(kvp.Key);
                    return;
                }
            }

            Main.Logger.LogWarning($"Protection instance not found within active instances; Assumed to be inaccessible or managed by someone else.");
        }
    }

    private bool ShouldUpdateSafePos()
    {
        if (this is { SaveCooldown: 0, Target.dead: false, Target.grabbedBy.Count: 0 })
        {
            Room.Tile tile = Target.room.GetTile(Target.bodyChunks[safeChunkIndex].pos);

            if ((isWaterBreathingCrit || !tile.DeepWater) && !tile.wormGrass)
            {
                return Target.IsTileSolid(safeChunkIndex, 0, -1)
                    && !Target.IsTileSolid(safeChunkIndex, 0, 0)
                    && !Target.IsTileSolid(safeChunkIndex, 0, 1);
            }
        }
        return false;
    }

    private void ToggleImmunities(AbstractCreature? target, bool enable)
    {
        if (target is null) return;

        target.HypothermiaImmune = enable || originalImmunities[0];
        target.lavaImmune = enable || originalImmunities[1];
        target.tentacleImmune = enable || originalImmunities[2];
    }

    /// <summary>
    ///     Creates a new death protection instance which lasts for the given amount of ticks.
    /// </summary>
    /// <param name="target">The creature to protect.</param>
    /// <param name="lifespan">The duration of the protection. Set to <c>null</c> or a negative value for an indefinitely-lasting protection.</param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="isPersistent">Whether or not the protection should be saved and persist across cycles.</param>
    /// <exception cref="ArgumentNullException">target is null.</exception>
    /// <exception cref="ArgumentException">target is not in a valid room. -or- target is of an unsupported type.</exception>
    /// <exception cref="InvalidOperationException">target is already being protected.</exception>
    public static void CreateInstance(Creature target, ushort? lifespan = null, WorldCoordinate? safePos = null, bool isPersistent = false)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target), "Protected creature cannot be null.");

        if (target.room is null)
            throw new ArgumentException("Cannot protect a target that is not in a valid room.", nameof(target));

        if (target is Overseer or BigJellyFish or DrillCrab or MothGrub or RippleSpider)
            throw new ArgumentException($"Creature type is not supported: {target.GetType()}", nameof(target));

        if (HasProtection(target))
            throw new InvalidOperationException($"{target} is already protected against death.");

        lifespan ??= 0;

        if (Extras.IsLocalObject(target))
        {
            DeathProtection protection = new(target, lifespan.Value, safePos, isPersistent);

            if (target.dead)
                RevivalHelper.ReviveCreature(target);

            if (Extras.IsOnlineSession)
                RainMeadowAccess.SyncNewProtection(target, protection);

            _activeInstances.Add(target, protection);

            Main.Logger.LogInfo($"Preventing all death from {target} {(lifespan is 0 ? "indefinitely" : $"for {lifespan.Value} ticks")}.");
        }
        else
        {
            RainMeadowAccess.RequestNewProtection(target, lifespan ?? 0, safePos);
        }
    }

    /// <summary>
    ///     Determines if a given creature is being protected from death.
    /// </summary>
    /// <param name="target">The creature to be tested.</param>
    /// <returns><c>true</c> if the given creature is being protected from death, <c>false</c> otherwise.</returns>
    public static bool HasProtection(Creature? target) => target is not null && _activeInstances.TryGetValue(target, out _);

    /// <summary>
    ///     Attempts to retrieve the <see cref="DeathProtection"/> instance of a given creature, if any.
    /// </summary>
    /// <param name="target">The creature to be queried.</param>
    /// <param name="protection">The retrieved protection instance. If none is found, this value is <c>null</c>.</param>
    /// <returns><c>true</c> if a valid protection instance was found, <c>false</c> otherwise.</returns>
    public static bool TryGetProtection(Creature? target, out DeathProtection protection)
    {
        if (target is null)
        {
            protection = null!;
            return false;
        }

        return _activeInstances.TryGetValue(target, out protection);
    }

    internal ProtectionSnapshot ToLocalSnapshot() => new(this);

    internal OnlineProtectionSnapshot ToOnlineSnapshot() => new(this);

    internal static void RestoreFromSnapshot(Creature key, in ProtectionSnapshot snapshot)
    {
        DeathProtection protection = new(key, snapshot.Lifespan, snapshot.SafePos, snapshot.IsPersistent)
        {
            RevivalsLeft = snapshot.RevivalsLeft,
            SaveCooldown = snapshot.SaveCooldown,
            SavingThrows = snapshot.SavingThrows
        };

        if (protection.Target.dead)
            RevivalHelper.ReviveCreature(protection.Target);

        _activeInstances.Add(protection.Target, protection);
    }

    internal static void SaveInstancesToDisk()
    {
        Dictionary<int, ProtectionSnapshot> data = [.. _activeInstances.Where(static kvp => kvp.Value.IsPersistent).Select(static kvp => new KeyValuePair<int, ProtectionSnapshot>(kvp.Key.abstractCreature.ID.number, kvp.Value.ToLocalSnapshot()))];

        Main.Logger.LogDebug($"Saving {data.Count} protection instances...");

        Main.ModData.SetData("DeathProtections", data);

        _activeInstances.Clear();
    }

    internal readonly struct ProtectionSnapshot(int target, WorldCoordinate? safePos, ushort lifespan, byte saveCooldown, byte savingThrows, byte revivalsLeft, bool isPersistent) : IEquatable<ProtectionSnapshot>
    {
        public readonly int Target = target;
        public readonly WorldCoordinate? SafePos = safePos;
        public readonly ushort Lifespan = lifespan;
        public readonly byte SaveCooldown = saveCooldown;
        public readonly byte SavingThrows = savingThrows;
        public readonly byte RevivalsLeft = revivalsLeft;
        public readonly bool IsPersistent = isPersistent;

        public ProtectionSnapshot(DeathProtection protection)
            : this(protection.Target.abstractCreature.ID.number, protection.SafePos, protection.Lifespan, protection.SaveCooldown, protection.SavingThrows, protection.RevivalsLeft, protection.IsPersistent)
        {
        }

        public void Deconstruct(out int target, out WorldCoordinate? safePos, out ushort lifespan, out byte saveCooldown, out byte savingThrows, out byte revivalsLeft, out bool isPersistent)
        {
            target = Target;
            safePos = SafePos;
            lifespan = Lifespan;
            saveCooldown = SaveCooldown;
            savingThrows = SavingThrows;
            revivalsLeft = RevivalsLeft;
            isPersistent = IsPersistent;
        }

        public bool Equals(ProtectionSnapshot other) =>
            other.Target == Target
            && (other.SafePos is null ? SafePos is null : SafePos is not null && other.SafePos == SafePos)
            && other.Lifespan == Lifespan
            && other.SaveCooldown == SaveCooldown
            && other.SavingThrows == SavingThrows
            && other.RevivalsLeft == RevivalsLeft
            && other.IsPersistent == IsPersistent;

        public override bool Equals(object obj) => obj is ProtectionSnapshot other && Equals(other);

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"DeathProtection: {Target}; {SafePos}, {(Lifespan > 0 ? $"{Lifespan}" : "Infinite")}, {SaveCooldown}c, {SavingThrows}t, {RevivalsLeft}r, {IsPersistent}";

        public static bool operator ==(in ProtectionSnapshot x, in ProtectionSnapshot y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(in ProtectionSnapshot x, in ProtectionSnapshot y)
        {
            return !x.Equals(y);
        }
    }

    private static partial class RainMeadowAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestNewProtection(Creature creature, ushort lifespan, WorldCoordinate? safePos)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            onlineCreature.owner.SendRPCEvent(MyRPCs.RequestNewProtection, onlineCreature, lifespan, safePos!);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SyncNewProtection(Creature creature, DeathProtection protection)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            ModRPCManager.BroadcastOnceRPCInLobby(MyRPCs.SyncNewProtection, onlineCreature, protection.ToOnlineSnapshot());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestStopProtection(Creature creature)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            onlineCreature.owner.SendRPCEvent(MyRPCs.RequestStopProtection, onlineCreature);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SyncStopProtection(Creature creature)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            ModRPCManager.BroadcastOnceRPCInLobby(MyRPCs.SyncStopProtection, onlineCreature);
        }
    }
}