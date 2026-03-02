using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ModLib.Meadow;
using ModLib.Objects.Meadow;
using ModLib.Options;
using MoreSlugcats;
using RainMeadow;
using RWCustom;

namespace ModLib.Objects;

/// <summary>
///     Helper methods for reviving creatures and Oracles.
/// </summary>
public static class RevivalHelper
{
    private const int REVIVAL_STUN = 80;

    /// <summary>
    ///     Creates a new Halcyon Pearl for Five Pebbles.
    /// </summary>
    /// <param name="oracle">The oracle who will own the object.</param>
    /// <returns>A new HalcyonPearl instance, or <c>null</c> if an error occurs during realization.</returns>
    public static HalcyonPearl? CreateHalcyonPearl(Oracle oracle)
    {
        if (!ModManager.MSC) return null;

        AbstractPhysicalObject abstractPearl = new(
            oracle.abstractPhysicalObject.world,
            MoreSlugcatsEnums.AbstractObjectType.HalcyonPearl,
            null,
            oracle.abstractPhysicalObject.pos,
            oracle.room.game.GetNewID());

        abstractPearl.Realize();

        if (abstractPearl.realizedObject is not HalcyonPearl halcyonPearl)
        {
            abstractPearl.realizedObject?.Destroy();
            abstractPearl.Destroy();

            Main.Logger.LogWarning("Failed to realize halcyon pearl, destroying created objects.");

            return null;
        }

        return halcyonPearl;
    }

    /// <summary>
    ///     Creates and grants a new Neuron Fly for the given Oracle (usually Looks to the Moon).
    /// </summary>
    /// <param name="oracle">The Oracle this neuron is being created for.</param>
    /// <returns>The newly created oracle swarmer, or <c>null</c> if an error ocurred during realization.</returns>
    public static OracleSwarmer? CreateOracleSwarmer(Oracle oracle)
    {
        AbstractPhysicalObject.AbstractObjectType swarmerType = oracle.ID == Oracle.OracleID.SL
            ? AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer
            : AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer;

        AbstractPhysicalObject abstractSwarmer = new(
            oracle.room.world,
            swarmerType,
            null,
            oracle.abstractPhysicalObject.pos,
            oracle.room.world.game.GetNewID()
        );

        abstractSwarmer.Realize();

        if (abstractSwarmer.realizedObject is not OracleSwarmer realizedSwarmer)
        {
            Main.Logger.LogWarning($"Failed to realize OracleSwarmer for {oracle}! Destroying abstract object.");

            abstractSwarmer.Destroy();
            abstractSwarmer.realizedObject?.Destroy();
            return null;
        }

        return realizedSwarmer;
    }

    /// <summary>
    ///     Obtains an Oracle's "full name" based on its ID.
    /// </summary>
    /// <param name="oracleID">The oracle ID to be tested.</param>
    /// <returns>The Oracle's name (e.g. <c>Five Pebbles</c>), or <c>Unknown Iterator</c> if a name couldn't be determined.</returns>
    public static string GetOracleName(Oracle.OracleID oracleID)
    {
        return oracleID == Oracle.OracleID.SS || (ModManager.MSC && (oracleID == MoreSlugcatsEnums.OracleID.CL || oracleID == MoreSlugcatsEnums.OracleID.SS_Cutscene))
            ? "Five Pebbles"
            : oracleID == Oracle.OracleID.SL || (ModManager.MSC && (oracleID == MoreSlugcatsEnums.OracleID.DM || oracleID == MoreSlugcatsEnums.OracleID.SL_Cutscene))
                ? "Looks to the Moon"
                : ModManager.MSC && (oracleID == MoreSlugcatsEnums.OracleID.ST || oracleID == MoreSlugcatsEnums.OracleID.ST_Cutscene)
                    ? "Sliver of Straw"
                    : $"Unknown Iterator";
    }

    /// <summary>
    ///     Revives a creature with full health and a brief stun duration.
    /// </summary>
    /// <param name="target">The creature to be revived.</param>
    /// <param name="forceRevive">If true, bypasses living checks and revives the creature even if it was considered "alive".</param>
    public static bool ReviveCreature(Creature? target, bool forceRevive = false)
    {
        if (target is null || (!target.dead && !forceRevive)) return false;

        if (Extras.IsLocalObject(target))
        {
            AbstractCreature abstractCreature = target.abstractCreature;

            if (abstractCreature.state is HealthState healthState)
            {
                healthState.alive = true;
                healthState.health = 1f;
            }

            target.dead = false;
            target.killTag = null;
            target.killTagCounter = 0;

            abstractCreature.abstractAI?.SetDestination(abstractCreature.pos);

            if (target is Player player)
            {
                if (player.playerState is not null)
                {
                    player.playerState.alive = true;
                    player.playerState.permaDead = false;
                    player.playerState.permanentDamageTracking = 0d;
                }

                player.airInLungs = 0.1f;
                player.exhausted = true;
                player.aerobicLevel = 1f;

                player.room?.game.cameras[0].hud?.textPrompt?.gameOverMode = false;
            }

            target.Stun(REVIVAL_STUN);

            Main.Logger.LogInfo($"{target} was revived!");
        }
        else
        {
            RainMeadowAccess.RequestCreatureRevival(target);
        }

        return true;
    }

    /// <summary>
    ///     Revives an Oracle, restoring any missing objects not found in the room. (e.g. Neuron Flies or the Halcyon Pearl)
    /// </summary>
    /// <remarks>
    ///     Custom Oracle revival can be enabled by setting the option <c>modlib.preview</c> to <c>true</c>.
    ///     Notice this feature is currently untested and highly prone to bugs.
    /// </remarks>
    /// <param name="oracle">The Oracle to be revived.</param>
    /// <param name="forceRevive">If true, bypasses living checks and revives the oracle even if it was considered "alive" or invalid for revival.</param>
    public static bool ReviveOracle(Oracle? oracle, bool forceRevive = false)
    {
        if (oracle is null || (!CanReviveOracle(oracle) && !forceRevive)) return false;

        if (Extras.IsLocalObject(oracle))
        {
            StoryGameSession storySession = oracle.room.game.GetStorySession;

            if (oracle.ID.value is "SS" or "SS_Cutscene" or "CL")
            {
                Custom.Log("Revive! Five Pebbles");

                if (ModManager.MSC && oracle.oracleBehavior is CLOracleBehavior pebblesBehavior)
                {
                    pebblesBehavior.Pain();

                    pebblesBehavior.halcyon = oracle.room.updateList.FirstOrDefault(static uad => uad is HalcyonPearl) as HalcyonPearl ?? CreateHalcyonPearl(oracle);
                }

                if (Extras.IsHostPlayer)
                {
                    if (ModManager.MSC)
                        storySession.saveState.miscWorldSaveData.halcyonStolen = (oracle.oracleBehavior as CLOracleBehavior)?.halcyon is null;

                    storySession.saveState.deathPersistentSaveData.ripPebbles = false;
                }
                else
                {
                    RainMeadowAccess.RequestRevivePebbles(oracle);
                }
            }
            else if (oracle.ID.value is "SL" or "SL_Cutscene" or "DM")
            {
                Custom.Log("Revive! Looks to the Moon");

                int neuronsLeft = ModManager.MSC && (storySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet || storySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint) ? 7 : 5;

                for (int i = 0; i < neuronsLeft; i++)
                {
                    oracle.mySwarmers.Add(CreateOracleSwarmer(oracle));

                    oracle.glowers++;
                }

                if (oracle.oracleBehavior is SLOracleBehavior moonBehavior)
                {
                    moonBehavior.State.neuronsLeft = neuronsLeft;

                    moonBehavior.Pain();
                }

                if (Extras.IsLocalObject(oracle))
                    storySession.saveState.deathPersistentSaveData.ripMoon = false;
                else
                    RainMeadowAccess.RequestReviveMoon(oracle);
            }
            else
            {
                Main.Logger.LogWarning($"Reviving unknown oracle: {oracle.ID}; Custom Oracle revival is not supported and may result in undefined behavior!");
            }

            oracle.stun = REVIVAL_STUN;
            oracle.health = 1f;

            Main.Logger.LogInfo($"{GetOracleName(oracle.ID)} ({oracle.ID}) was revived!");
        }
        else
        {
            RainMeadowAccess.RequestOracleRevival(oracle);
        }

        return true;
    }

    /// <summary>
    ///     Removes a given creature from the world's <c>respawnCreatures</c> list, preventing it from respawning the next cycle.
    /// </summary>
    /// <param name="creature">The creature to be removed.</param>
    public static void RemoveFromRespawnList(Creature creature)
    {
        if (Extras.IsHostPlayer)
        {
            EntityID ID = creature.abstractCreature.ID;

            if (creature.abstractCreature.state.alive && ID.spawner >= 0 && creature.room?.game.session is StoryGameSession storySession)
            {
                storySession.saveState.respawnCreatures.Remove(ID.spawner);

                Main.Logger.LogDebug($"Removed {creature} ({ID.spawner}) from the respawns list.");
            }
        }
        else
        {
            RainMeadowAccess.RequestRemoveFromRespawnList(creature);
        }
    }

    /// <summary>
    ///     Determines if the given Oracle can be revived.
    /// </summary>
    /// <param name="oracle">The oracle for revival.</param>
    /// <returns><c>true</c> if the given oracle can be revived, <c>false</c> otherwise.</returns>
    private static bool CanReviveOracle(Oracle oracle)
    {
        return oracle.room?.game.session is StoryGameSession storyGame
            && (oracle.ID == Oracle.OracleID.SS || (ModManager.MSC && (oracle.ID == MoreSlugcatsEnums.OracleID.CL || oracle.ID == MoreSlugcatsEnums.OracleID.SS_Cutscene))
                ? storyGame.saveState.deathPersistentSaveData.ripPebbles
                : oracle.ID == Oracle.OracleID.SL || (ModManager.MSC && (oracle.ID == MoreSlugcatsEnums.OracleID.DM || oracle.ID == MoreSlugcatsEnums.OracleID.SL_Cutscene))
                    ? storyGame.saveState.deathPersistentSaveData.ripMoon || oracle.oracleBehavior is SLOracleBehavior { State.neuronsLeft: 0 }
                    : (!ModManager.MSC || (oracle.ID != MoreSlugcatsEnums.OracleID.ST && oracle.ID != MoreSlugcatsEnums.OracleID.ST_Cutscene)) && SharedOptions.IsOptionEnabled("modlib.preview") && !oracle.Alive);
    }

    private static class RainMeadowAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestCreatureRevival(Creature creature)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            onlineCreature.owner.SendRPCEvent(MyRPCs.RequestCreatureRevival, onlineCreature);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestOracleRevival(Oracle oracle)
        {
            OnlinePhysicalObject? onlineObject = oracle.abstractPhysicalObject.GetOnlineObject() ?? throw new ArgumentException($"Could not retrieve online representation of {oracle}.", nameof(oracle));

            onlineObject.owner.SendRPCEvent(MyRPCs.RequestOracleRevival, onlineObject);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestRevivePebbles(Oracle oracle)
        {
            OnlinePhysicalObject? onlineObject = oracle.abstractPhysicalObject.GetOnlineObject() ?? throw new ArgumentException($"Could not retrieve online representation of {oracle}.", nameof(oracle));

            OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.RequestRevivePebbles, onlineObject);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestReviveMoon(Oracle oracle)
        {
            OnlinePhysicalObject? onlineObject = oracle.abstractPhysicalObject.GetOnlineObject() ?? throw new ArgumentException($"Could not retrieve online representation of {oracle}.", nameof(oracle));

            OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.RequestReviveMoon, onlineObject);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RequestRemoveFromRespawnList(Creature creature)
        {
            OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature() ?? throw new ArgumentException($"Could not retrieve online representation of {creature}.", nameof(creature));

            OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.RequestRemoveFromRespawnList, onlineCreature);
        }
    }
}