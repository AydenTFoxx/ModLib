using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModLib.Collections;
using ModLib.Meadow;
using ModLib.Objects.Meadow;
using UnityEngine;

namespace ModLib.Objects;

/// <summary>
///     Prevents all death of its target creature for a configurable delay and/or condition.
///     This class cannot be inherited.
/// </summary>
public sealed class DeathProtection : UpdatableAndDeletable
{
    private const byte MAX_SAVING_THROWS = 128;

    private static readonly WeakDictionary<Creature, DeathProtection> _activeInstances = [];

    /// <summary>
    ///     The targeted creature.
    /// </summary>
    public Creature Target { get; }

    /// <summary>
    ///     The last known position considered "safe"; If the protected creature is destroyed, it is instead teleported to this position.
    /// </summary>
    public WorldCoordinate? SafePos { get; private set; }

    /// <summary>
    ///     The power value for visual effects.
    /// </summary>
    public float Power { get; }

    /// <summary>
    ///     If greater than <c>0</c>, a cooldown where the creature is assumed to be alive; Prevents repeated attempts to revive the target causing no revival at all.
    /// </summary>
    public byte SaveCooldown { get; private set; }

    /// <summary>
    ///     The amount of times the protection has saved Target from destruction in a row. If exceeding a given limit, the protection is destroyed.
    /// </summary>
    public byte SavingThrows { get; set; }

    /// <summary>
    ///     The amount of times the protection will attempt to revive its target creature. If this number reaches zero, the protection is destroyed.
    /// </summary>
    private byte revivalsLeft = 10;

    /// <summary>
    ///     An optional lifespan for the protection. If a condition is also provided, this acts as the "grace" timer before the protection can actually expire.
    /// </summary>
    private short lifespan;

    /// <summary>
    ///     If true, lifespan is ignored and the protection lasts until manually destroyed.
    /// </summary>
    private readonly bool isInfinite;

    /// <summary>
    ///     If true and Target somehow dies while under this protection, it is instead revived.
    /// </summary>
    private readonly bool forceRevive;

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
    ///     Creates a new death protection instance which lasts until the given condition returns <c>true</c>.
    /// </summary>
    /// <param name="target">The creature to protect.</param>
    /// <param name="lifespan">The amount of time to wait before the protection can be removed.</param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="forceRevive">If true and <paramref name="target"/> somehow dies while protected, it is revived. This also immediately ends the protection.</param>
    private DeathProtection(Creature target, short lifespan, WorldCoordinate? safePos, bool forceRevive)
    {
        Target = target;
        Power = Mathf.Max(target.Template.meatPoints * 0.5f, 0.5f);

        this.forceRevive = forceRevive;
        this.lifespan = lifespan;

        isInfinite = lifespan < 0;

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
            Main.Logger.LogWarning($"[DeathProtection] Target was destroyed while being protected! Removing protection object.");
            Destroy(false);
            return;
        }

        Target.rainDeath = 0f;

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
            Target.repelLocusts = Math.Max(Target.repelLocusts, 10 * Math.Max(lifespan, (short)2));

        if (Target.grabbedBy.Count > 0)
        {
            for (int i = Target.grabbedBy.Count - 1; i >= 0; i--)
            {
                Creature.Grasp grasp = Target.grabbedBy[i];

                if (grasp is null or { grabber: null or Player }) return;

                grasp.grabber.ReleaseGrasp(grasp.graspUsed);
                grasp.grabber.Stun(20);
            }
        }

        if (Target.State is HealthState healthState)
        {
            healthState.health = 1f;
            healthState.alive = true;
        }

        if (Target.room is not null)
        {
            if (Target.room != room)
            {
                Main.Logger.LogInfo($"[DeathProtection] Moving DeathProtection object to Target room.");

                room?.RemoveObject(this);
                Target.room.AddObject(this);
            }

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
        }
        else if (this is { Target.inShortcut: false, SafePos: not null })
        {
            if (SavingThrows < MAX_SAVING_THROWS)
            {
                Main.Logger.LogWarning($"{Target} not found in a room while being protected! Performing saving throw to prevent destruction.");

                DeathProtectionHooks.TrySaveFromDestruction(Target);

                SavingThrows += 10;
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
            bool canRevive = this is { forceRevive: true, revivalsLeft: > 0, SaveCooldown: 0 };

            Main.Logger.LogWarning($"{Target} was killed while protected! Will revive? {canRevive}");

            if (canRevive)
                ReviveTarget();
            else
                Destroy();

            return;
        }

        if (isInfinite) return;

        if (lifespan > 0)
        {
            lifespan--;
        }
        else if (Target.stun == 0 && (Target is Player plr ? plr is { canJump: > 0 } or { Submersion: >= 0.5f } : Target is { Submersion: >= 0.5f } || Target.IsTileSolid(safeChunkIndex, 0, -1)))
        {
            Destroy();
        }
    }

    /// <inheritdoc/>
    public override void Destroy() => Destroy(true);

    private void Destroy(bool warnIfNull)
    {
        base.Destroy();

        if (Target is not null)
        {
            ToggleImmunities(Target.abstractCreature, false);

            _activeInstances.Remove(Target);

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

        if (Extras.IsOnlineSession && Target is not null && MeadowUtils.IsMine(Target))
            RainMeadowAccess.BroadcastProtectionDestruction(this);
    }

    private void ReviveTarget()
    {
        Target.dead = false;

        if (Target.State is HealthState healthState)
        {
            healthState.alive = true;
            healthState.health = 1f;
        }
        else if (Target is Player player)
        {
            player.playerState.alive = true;

            if (ModManager.CoopAvailable)
            {
                player.playerState.permaDead = false;
            }
        }

        Target.abstractCreature.abstractAI?.SetDestinationNoPathing(Target.abstractCreature.pos, false);

        SaveCooldown = 10;
        revivalsLeft--;

        if (Extras.IsOnlineSession && MeadowUtils.IsMine(Target))
            MeadowUtils.LogSystemMessage($"{(Target is Player plr ? MeadowUtils.GetOnlineName(plr) : Target.Template.name)} was revived!");

        Main.Logger.LogDebug($"Revived {Target}!");
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
    /// <param name="lifespan">The duration of the protection. Set to <c>-1</c> for an indefinitely-lasting protection.</param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="forceRevive">If true and <paramref name="target"/> somehow dies while protected, it is revived. This also immediately ends the protection.</param>
    /// <exception cref="ArgumentNullException">target is null.</exception>
    /// <exception cref="ArgumentException">target is not in a valid room -or- target is already being protected.</exception>
    public static DeathProtection CreateInstance(Creature target, short lifespan = 40, WorldCoordinate? safePos = null, bool forceRevive = true)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target), "Protected creature cannot be null.");

        if (target.room is null)
            throw new ArgumentException("Cannot protect a target that is not in a valid room.");

        if (HasProtection(target))
            throw new ArgumentException($"{target} is already protected against death.");

        if (lifespan < -1)
        {
            Main.Logger.LogWarning($"Creating DeathProtection with lifespan of {lifespan}! Setting to -1 for indefinite duration.");

            lifespan = -1;
        }

        DeathProtection protection = new(target, lifespan, safePos, forceRevive);

        if (target.dead)
        {
            protection.ReviveTarget();
        }

        _activeInstances.Add(target, protection);
        target.room.AddObject(protection);

        if (Extras.IsOnlineSession)
            RainMeadowAccess.BroadcastProtectionCreation(protection);

        Main.Logger.LogInfo($"Preventing all death from {target} {(lifespan == -1 ? "indefinitely" : $"for {lifespan} ticks")}.");

        return protection;
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

    internal OnlineDeathProtection ToOnlineProtection() => new(ToSnapshot());

    internal ProtectionSnapshot ToSnapshot() => new(this);

    internal static DeathProtection FromSnapshot(ProtectionSnapshot snapshot)
    {
        return new DeathProtection(snapshot.Target, snapshot.Lifespan, snapshot.SafePos, snapshot.ForceRevive)
        {
            SaveCooldown = snapshot.SaveCooldown,
            revivalsLeft = snapshot.RevivalsLeft
        };
    }

    internal static Dictionary<Creature, DeathProtection> GetInstances() => _activeInstances;

    internal static void AddInstance(Creature target, DeathProtection protection) => _activeInstances.Add(target, protection);

    internal static void SetInstances(IDictionary<Creature, DeathProtection> values)
    {
        _activeInstances.Clear();

        foreach (KeyValuePair<Creature, DeathProtection> kvp in values)
        {
            _activeInstances.Add(kvp);
        }
    }

    internal sealed class ProtectionSnapshot
    {
        public Creature Target;

        public WorldCoordinate? SafePos;

        public byte SaveCooldown;

        public byte RevivalsLeft;

        public short Lifespan;

        public bool ForceRevive;

        public ProtectionSnapshot(DeathProtection protection)
        {
            Target = protection.Target;
            SafePos = protection.SafePos;
            SaveCooldown = protection.SaveCooldown;
            RevivalsLeft = protection.revivalsLeft;
            Lifespan = protection.lifespan;
            ForceRevive = protection.forceRevive;
        }

        public ProtectionSnapshot()
        {
            Target = null!;
        }
    }

    private static class RainMeadowAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BroadcastProtectionCreation(DeathProtection protection) =>
            ModRPCManager.BroadcastOnceRPCInLobby(null, MyRPCs.RequestProtection, protection.ToOnlineProtection());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BroadcastProtectionDestruction(DeathProtection protection) =>
            ModRPCManager.BroadcastOnceRPCInLobby(null, MyRPCs.StopProtection, protection.ToOnlineProtection());
    }
}