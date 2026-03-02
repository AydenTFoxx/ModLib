using DevConsole;
using ModLib.Debug;
using UnityEngine;

namespace ModLib.Objects;

internal static class ModDebuggerExtension
{
    public static void RegisterCommands()
    {
        Main.Logger.LogDebug("Registering extension commands for ModLib!");

        ModDebugger.MainCommandTree.AddChildren(
            new CommandNode("protection", null,
                new CommandNode("list", null, static _ => ModDebugger.WriteToConsole($"Active death protections:{ModDebugger.ListElements(DeathProtection.Instances)}")),
                new CommandNode("add", null, static args =>
                {
                    if (!ModDebugger.AssertInGame() || !ModDebugger.ParseInt32(args[0], out int creatureId, argName: "ID")) return;

                    Creature? creature = ModDebugger.GetCreatureById(creatureId)?.realizedCreature;

                    if (creature is null)
                        ModDebugger.WriteToConsole($"Could not find a realized creature with ID {creatureId}.", Color.red);
                    else if (DeathProtection.TryGetProtection(creature, out DeathProtection protection))
                        ModDebugger.WriteToConsole($"{creature} is already protected.", Color.red);
                    else
                    {
                        ushort duration = args.Length > 1 && ModDebugger.ParseUInt16(args[1], out ushort value, silent: true) ? value : ushort.MinValue;
                        bool isPersistent = ModDebugger.ValidateBoolean(in args, 2);

                        DeathProtection.CreateInstance(creature, duration, null, isPersistent);

                        ModDebugger.WriteToConsole($"Started the protection of {creature} {(duration > 0 ? $"for {duration} ticks" : "indefinitely")}.");
                    }
                }, ["creatureId: Int32", "duration: UInt16 = 0", "isPersistent: Boolean = False"]),
                new CommandNode("remove", null, static args =>
                {
                    if (!ModDebugger.AssertInGame() || !ModDebugger.ParseInt32(args[0], out int creatureId, argName: "ID")) return;

                    Creature? creature = ModDebugger.GetCreatureById(creatureId)?.realizedCreature;

                    if (creature is null)
                        ModDebugger.WriteToConsole($"Could not find a realized creature with ID {creatureId}.", Color.red);
                    else if (!DeathProtection.TryGetProtection(creature, out DeathProtection protection))
                        ModDebugger.WriteToConsole($"{creature} has no death protection.", Color.red);
                    else
                    {
                        protection.Destroy();

                        ModDebugger.WriteToConsole($"Stopped protection of {creature}.");
                    }
                }, ["creatureId: Int32"])),
            new CommandNode("revive", null,
                new CommandNode("creature", null, static args =>
                {
                    if (!ModDebugger.AssertInGame() || !ModDebugger.ParseInt32(args[0], out int creatureId, argName: "ID")) return;

                    Creature? creature = ModDebugger.GetCreatureById(creatureId)?.realizedCreature;

                    if (creature is null)
                        ModDebugger.WriteToConsole($"Could not find a realized creature with ID {creatureId}.", Color.red);
                    else if (!RevivalHelper.ReviveCreature(creature, forceRevive: true))
                        ModDebugger.WriteToConsole($"Failed to revive {creature}.", Color.red);
                    else
                        ModDebugger.WriteToConsole($"Successfully revived {creature}.");
                }, ["creatureId: Int32"]),
                new CommandNode("oracle", null, static args =>
                {
                    if (!ModDebugger.AssertInGame() || !ModDebugger.ParseInt32(args[0], out int oracleId, argName: "ID")) return;

                    PhysicalObject? obj = ModDebugger.GetObjectById(oracleId)?.realizedObject;

                    if (obj is null)
                        ModDebugger.WriteToConsole($"Could not find an object with ID {oracleId}.", Color.red);
                    if (obj is not Oracle oracle)
                        ModDebugger.WriteToConsole($"{obj} is not an Oracle.", Color.red);
                    else if (!RevivalHelper.ReviveOracle(oracle, forceRevive: true))
                        ModDebugger.WriteToConsole($"Failed to revive oracle {RevivalHelper.GetOracleName(oracle.ID)}.", Color.red);
                    else
                        ModDebugger.WriteToConsole($"Successfully revived oracle {RevivalHelper.GetOracleName(oracle.ID)}.");
                }, ["objectId: Int32"])
            ),
            new CommandNode("meltlights", null,
                new CommandNode("create", null, static args =>
                {
                    if (!ModDebugger.AssertInGame()) return;

                    float speed = args.Length > 0 && ModDebugger.ParseSingle(args[0], out float v1, 0f, silent: true) ? v1 : 1f;
                    float strength = args.Length > 1 && ModDebugger.ParseSingle(args[1], out float v2, 0f, silent: true) ? v2 : 1f;

                    if (GameConsole.TargetPos.Room is null)
                        ModDebugger.WriteToConsole("Cannot create a new FadingMeltLights object in the current target position.", Color.red);
                    else if (GameConsole.TargetPos.Room.realizedRoom is null)
                        ModDebugger.WriteToConsole("Target room is not realized.", Color.red);
                    else
                    {
                        GameConsole.TargetPos.Room.realizedRoom.AddObject(new FadingMeltLights(speed, strength));

                        ModDebugger.WriteToConsole($"Created new FadingMeltLights object at {GameConsole.TargetPos.Room.name}.");
                    }
                }, ["speed: Single = 1f", "strength: Single = 1f"])
            )
        );
    }
}