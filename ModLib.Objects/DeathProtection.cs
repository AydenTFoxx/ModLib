using System;
using System.Collections.Generic;
using System.Linq;
using ModLib.Collections;
using UnityEngine;

namespace ModLib.Objects;

/// <summary>
///     Prevents all death of its target creature while active.
///     Can be configured to either expire after a given delay, or once a given condition is met.
/// </summary>
public class DeathProtection : UpdatableAndDeletable
{
    private static readonly WeakDictionary<Creature, DeathProtection> _activeInstances = [];

    /// <summary>
    ///     The positive condition for removing the protection instance; Always returns <c>true</c>.
    /// </summary>
    /// <remarks>
    ///     When using this predicate, the protection instance is removed as soon as lifetime is depleted.
    /// </remarks>
    public static readonly Predicate<Creature> TrueCondition = static (_) => true;

    /// <summary>
    ///     The negative condition for removing the protection instance; Always returns <c>false</c>.
    /// </summary>
    /// <remarks>
    ///     When using this predicate, the protection instance remains indefinitely active until manually removed.
    /// </remarks>
    public static readonly Predicate<Creature> FalseCondition = static (_) => false;

    /// <summary>
    ///     The default condition for removing the protection instance; Returns <c>true</c> when the target is in a safe position.
    /// </summary>
    /// <remarks>
    ///     When using this predicate, the protection is removed as soon as the target is conscious and on solid ground or a body of water.
    /// </remarks>
    public static readonly Predicate<Creature> SafeLandingCondition = static (crit) =>
        crit.stun == 0 && crit is Player plr ? plr is { canJump: > 0 } or { Submersion: >= 0.5f } : crit is { Submersion: >= 0.5f } || crit.IsTileSolid(crit.mainBodyChunkIndex, 0, -1);

    /// <summary>
    ///     The targeted creature.
    /// </summary>
    public Creature Target { get; }

    /// <summary>
    ///     The sources of damage this protection will prevent against for its Target.
    /// </summary>
    public ProtectionMode ProtectionModes
    {
        get;
        set
        {
            field = value;

            ProtectAgainstViolence = value.HasFlag(ProtectionMode.Violence);
            ProtectAgainstEnvironment = value.HasFlag(ProtectionMode.Environment);
            ProtectAgainstDestruction = value.HasFlag(ProtectionMode.Destruction);
        }
    }

    /// <summary>
    ///     Determines if the current protection will prevent creature-sourced damage or death attempts.
    ///     This value is equivalent to <c>ProtectionModes.HasFlag(DeathProtection.ProtectionMode.Violence)</c>.
    /// </summary>
    public bool ProtectAgainstViolence { get; private set; }

    /// <summary>
    ///     Determines if the current protection will prevent environmental damage or death causes.
    ///     This value is equivalent to <c>ProtectionModes.HasFlag(DeathProtection.ProtectionMode.Environment)</c>.
    /// </summary>
    public bool ProtectAgainstEnvironment { get; private set; }

    /// <summary>
    ///     Determines if the current protection will prevent all kinds of destruction, be it creature or environment-sourced.
    ///     This value is equivalent to <c>ProtectionModes.HasFlag(DeathProtection.ProtectionMode.Destruction)</c>.
    /// </summary>
    public bool ProtectAgainstDestruction { get; private set; }

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
    public int SaveCooldown { get; private set; }

    /// <summary>
    ///     An optional lifespan for the protection. If a condition is also provided, this acts as the "grace" timer before the protection can actually expire.
    /// </summary>
    private int lifespan;

    /// <summary>
    ///     The amount of times the protection will attempt to revive its target creature. If this number reaches zero, the protection is destroyed.
    /// </summary>
    private int revivalsLeft = 10;

    /// <summary>
    ///     An optional condition to determine when the protection should be removed. If omitted, defaults to a countdown using lifespan.
    /// </summary>
    private readonly Predicate<Creature> condition;

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
    private readonly int safeChunkIndex;

    /// <summary>
    ///     The natural immunities Target had prior to being protected, in the following order: Hypothermia, Lava, Tentacle.
    ///     When the protection is removed, their respective fields are restored to their original values.
    /// </summary>
    private readonly bool[] originalImmunities;

    /// <summary>
    ///     Creates a new death protection instance which lasts until the given condition returns <c>true</c>.
    /// </summary>
    /// <param name="target">The creature to protect.</param>
    /// <param name="condition">The condition for removing the protection; When <c>true</c>, the protection is removed.</param>
    /// <param name="lifespan">The amount of time to wait before <paramref name="condition"/> can be tested.</param>
    /// <param name="protectionModes"></param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="forceRevive">If true and <paramref name="target"/> somehow dies while protected, it is revived. This also immediately ends the protection.</param>
    private DeathProtection(Creature target, Predicate<Creature> condition, int lifespan, ProtectionMode protectionModes, WorldCoordinate? safePos, bool forceRevive)
    {
        Target = target;

        ProtectionModes = protectionModes;

        this.condition = condition;
        this.forceRevive = forceRevive;
        this.lifespan = lifespan;

        if (ProtectAgainstEnvironment)
        {
            SafePos = safePos ?? target.abstractCreature.pos;
            Power = Mathf.Clamp(1f * (target.TotalMass / target.bodyChunks.Length), 0.1f, 3f);

            isWaterBreathingCrit = target.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious
                                || target.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly;

            safeChunkIndex = Target is Player ? 1 : Target.mainBodyChunkIndex;
        }

        if (ProtectAgainstDestruction)
        {
            originalImmunities = [target.abstractCreature.HypothermiaImmune, target.abstractCreature.lavaImmune, target.abstractCreature.tentacleImmune];

            ToggleImmunities(target.abstractCreature, true);
        }
        else
        {
            originalImmunities = [];
        }
    }

    /// <inheritdoc/>
    public override void Update(bool eu)
    {
        base.Update(eu);

        if (Target is null)
        {
            Core.Logger.LogWarning($"[DeathProtection] Target was destroyed while being protected! Removing protection object.");
            Destroy(false);
            return;
        }

        if (ProtectAgainstEnvironment)
        {
            Target.rainDeath = 0f;

            if (Target is Player player)
            {
                player.airInLungs = 1f;

                if (ModManager.Watcher)
                {
                    player.rippleDeathTime = 0;
                    player.rippleDeathIntensity = 0f;
                }
            }
            else if (Target is AirBreatherCreature airBreatherCreature)
                airBreatherCreature.lungs = 1f;

            if (ModManager.Watcher)
                Target.repelLocusts = Math.Max(Target.repelLocusts, 10 * Math.Max(lifespan, 2));

            foreach (WormGrass wormGrass in Target.room.updateList.OfType<WormGrass>())
            {
                foreach (WormGrass.WormGrassPatch patch in wormGrass.patches)
                {
                    patch.trackedCreatures.RemoveAll(tc => tc.creature == Target);
                }

                foreach (WormGrass.Worm worm in wormGrass.worms.Where(w => w.focusCreature == Target))
                {
                    worm.focusCreature = null;
                }

                wormGrass.AddNewRepulsiveObject(Target);
            }
        }

        if (ProtectAgainstViolence)
        {
            if (Target is Player player)
                player.playerState.permanentDamageTracking = 0d;

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
        }

        if (Target.room is not null)
        {
            if (Target.room != room)
            {
                Core.Logger.LogInfo($"[DeathProtection] Moving DeathProtection object to Target room.");

                room?.RemoveObject(this);
                Target.room.AddObject(this);
            }

            foreach (RoomCamera camera in Target.room.game.cameras)
            {
                camera.hud?.textPrompt?.gameOverMode = false;
            }

            if (ProtectAgainstDestruction && (SafePos is null || ShouldUpdateSafePos()))
            {
                SafePos = Target.room.GetWorldCoordinate(Target.bodyChunks[safeChunkIndex].pos);
            }
        }
        else if (ProtectAgainstDestruction && this is { Target.inShortcut: false, SafePos: not null })
        {
            Core.Logger.LogWarning($"{Target} not found in a room while being protected! Performing saving throw to prevent destruction.");

            DeathProtectionHooks.TrySaveFromDestruction(Target);
        }

        if (SaveCooldown > 0)
            SaveCooldown--;

        if (Target.dead)
        {
            bool canRevive = this is { forceRevive: true, revivalsLeft: > 0, SaveCooldown: 0 };

            Core.Logger.LogWarning($"{Target} was killed while protected! Will revive? {canRevive}");

            if (canRevive)
                ReviveTarget();
            else
                Destroy();

            return;
        }

        if (lifespan > 0)
        {
            lifespan--;
            return;
        }

        if (condition.Invoke(Target))
            Destroy();
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

            Core.Logger.LogDebug($"{Target} is no longer being protected.");
        }
        else
        {
            if (warnIfNull)
                Core.Logger.LogWarning($"Protection object was destroyed while Target was null! Attempting to remove instance directly.");

            foreach (KeyValuePair<Creature, DeathProtection> kvp in _activeInstances)
            {
                if (kvp.Value == this)
                {
                    Core.Logger.LogInfo($"Removing detached protection instance from: {kvp.Key}");

                    _activeInstances.Remove(kvp.Key);
                    return;
                }
            }

            Core.Logger.LogWarning($"Protection instance not found within active instances; Assumed to be inaccessible or managed by someone else.");
        }
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

        Core.Logger.LogDebug($"Revived {Target}!");
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
    /// <param name="lifespan">The duration of the protection.</param>
    /// <param name="protectionModes"></param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="forceRevive">If true and <paramref name="target"/> somehow dies while protected, it is revived. This also immediately ends the protection.</param>
    /// <exception cref="ArgumentNullException">target is null.</exception>
    /// <exception cref="InvalidOperationException">target is not in a valid room -or- target is already being protected.</exception>
    public static DeathProtection CreateInstance(Creature target, int lifespan, ProtectionMode protectionModes = ProtectionMode.FullInvulnerability, WorldCoordinate? safePos = null, bool forceRevive = true)
    {
        DeathProtection protection = CreateInstanceInternal(target, SafeLandingCondition, lifespan, protectionModes, safePos, forceRevive);

        Core.Logger.LogInfo($"Preventing all death from {target} for {lifespan} ticks.");

        return protection;
    }

    /// <summary>
    ///     Creates a new death protection instance which lasts for the given amount of ticks.
    /// </summary>
    /// <param name="target">The creature to protect.</param>
    /// <param name="condition">The condition for removing the protection; When <c>true</c>, the protection is removed.</param>
    /// <param name="protectionModes"></param>
    /// <param name="safePos">An optional "safe" position, used when the creature is destroyed.</param>
    /// <param name="forceRevive">If true and <paramref name="target"/> somehow dies while protected, it is revived. This also immediately ends the protection.</param>
    /// <exception cref="ArgumentNullException">target is null -or- condition is null.</exception>
    /// <exception cref="InvalidOperationException">target is not in a valid room -or- target is already being protected.</exception>
    public static DeathProtection CreateInstance(Creature target, Predicate<Creature> condition, ProtectionMode protectionModes = ProtectionMode.FullInvulnerability, WorldCoordinate? safePos = null, bool forceRevive = true)
    {
        DeathProtection? protection = CreateInstanceInternal(target, condition, 40, protectionModes, safePos, forceRevive);

        Core.Logger.LogInfo($"Preventing all death from {target} {(condition == FalseCondition ? "indefinitely" : "conditionally")}.");

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

    private static DeathProtection CreateInstanceInternal(Creature target, Predicate<Creature> condition, int lifespan, ProtectionMode protectionModes, WorldCoordinate? safePos, bool forceRevive)
    {
        if (target is null || condition is null)
            throw new ArgumentNullException(target is null ? nameof(target) : nameof(condition));

        if (target.room is null)
            throw new InvalidOperationException("Cannot protect a target that is not in a valid room.");

        if (HasProtection(target))
            throw new InvalidOperationException($"{target} is already protected against death.");

        DeathProtection protection = new(target, condition, lifespan, protectionModes, safePos, forceRevive);

        if (target.dead)
        {
            protection.ReviveTarget();
        }

        _activeInstances.Add(target, protection);

        target.room.AddObject(protection);

        return protection;
    }

    /// <summary>
    ///     Determines what kind of death sources will <see cref="DeathProtection"/> protect its target against.
    /// </summary>
    [Flags]
    public enum ProtectionMode
    {
        /// <summary>
        ///     No source will be prevented; This is the same as having no death protection at all.
        /// </summary>
        None,
        /// <summary>
        ///     Creature-related violence (e.g. lizard bites, all weapons, etc.) will be ignored;
        ///     The target cannot be grabbed by creatures, and becomes immune to Bite and Explosion damage.
        /// </summary>
        Violence,
        /// <summary>
        ///     Environment-related death (e.g. electricity, drowning, worm grass, etc.) will be ignored;
        ///     The target cannot drown, is ignored by worm grass, and becomes immune to lava, hypothermia, and Electricity damage.
        ///     <br/><br/>
        ///     This also prevents death from Ripple Amoeba in the Watcher campaign.
        /// </summary>
        Environment,
        /// <summary>
        ///     Combines the protection values of <see cref="Violence"/> and <see cref="Environment"/>; Has a similar behavior to the `invuln` command from the Dev Console mod.
        ///     This is a composite protection type.
        /// </summary>
        GenericImmunity,
        /// <summary>
        ///     Protects the target against all forms of destruction (death pits, tesla coils, worm grass, vultures flying off-screen, etc.);
        ///     If destroyed, the target will reappear at the last "safe" position it was at, briefly knocking away nearby creatures.
        /// </summary>
        Destruction,
        /// <summary>
        ///     Combines the protection values of <see cref="Violence"/> and <see cref="Destruction"/>;
        ///     Prevents all forms of creature violence and destruction, such as being crushed by a Leviathan's bite.
        ///     This is a composite protection type.
        /// </summary>
        NonEnvironmental,
        /// <summary>
        ///     Combines the protection values of <see cref="Environment"/> and <see cref="Destruction"/>;
        ///     Prevents all forms of environmental harm and destruction, such as worm grass or death pits.
        ///     This is a composite protection type.
        /// </summary>
        NonCreature,
        /// <summary>
        ///     Combines all three protection types: <see cref="Violence"/>, <see cref="Environment"/>, and <see cref="Destruction"/>;
        ///     Prevents all forms of death and destruction from killing the target.
        ///     Unless by exceptional cases or mod intervention, the target cannot die or be destroyed by any means.
        ///     This is a composite protection type.
        /// </summary>
        FullInvulnerability
    }
}