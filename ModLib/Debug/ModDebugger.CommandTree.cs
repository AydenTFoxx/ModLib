using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using ModLib.Extensions;
using ModLib.Input;
using ModLib.Loader;
using ModLib.Logging;
using ModLib.Meadow;
using ModLib.Options;
using ModLib.Storage;
using RainMeadow;
using UnityEngine;

namespace ModLib.Debug;

public static partial class ModDebugger
{
    public static readonly CommandNode MainCommandTree = new("modlib", $"Base interactive command for managing ModLib's various modules.{Environment.NewLine}{Environment.NewLine}To invoke a particular command, type `modlib <CommandName>` plus any arguments.",
        new CommandNode("compats", "Determines or \"overrides\" the presence of specified mods.",
            new CommandNode("list", "Lists all currently active presence overrides.", static _ => WriteToConsole($"Compatibility layers:{ListElements(CompatibilityManager.ManagedMods)}")),
            new CommandNode("get", "Retrieves the current presence of a given mod.", static args => WriteToConsole($"Is mod [{args[0]}] enabled? {CompatibilityManager.IsModEnabled_NoCache(args[0])} | Overriden by CompatibilityManager? {CompatibilityManager.ManagedMods.ContainsKey(args[0])}"), ["modId: String"]),
            new CommandNode("set", "Overrides the presence of a given mod to a specified value.", static args =>
            {
                if (!ParseBoolean(args[1], out bool value)) return;

                CompatibilityManager.SetModCompatibility(args[0], value);

                switch (args[0])
                {
                    case CompatibilityManager.FAKE_ACHIEVEMENTS_ID:
                        Extras.IsFakeAchievementsEnabled = value;
                        break;
                    case CompatibilityManager.IMPROVED_INPUT_ID:
                        Extras.IsIICEnabled = value;
                        break;
                    case CompatibilityManager.RAIN_MEADOW_ID:
                        Extras.IsMeadowEnabled = value;
                        break;
                    default:
                        break;
                }

                WriteToConsole($"Set compatibility layer of {args[0]} to {value}.");
            }, ["modId: String", "flag: Boolean"])),
        new CommandNode("data", "Manages the ModData stored by a specified mod.",
            new CommandNode("list", "Lists all data stored by the specified mod.", static args =>
            {
                ModData? data = ModData.StoredInstances.Find(md => md.ModID == args[0]);

                WriteToConsole(data is null ? $"Mod [{args[0]}] has no stored data." : $"Data for [{args[0]}] is:{{{ListElements(data.Data)}}}");
            }, ["modId: String"]),
            new CommandNode("get", "Retrieves the given key from the specified mod's stored data.", static args =>
            {
                ModData? data = ModData.StoredInstances.Find(md => md.ModID == args[0]);

                if (data is null)
                    WriteToConsole($"No data was found for mod [{args[0]}].", Color.red);
                else if (!data.TryGetData(args[1], out object value))
                    WriteToConsole($"Mod data for [{args[0]}] has no key \"{args[1]}\".", Color.red);
                else
                    WriteToConsole($"\"{args[1]}\": {value}");
            }, ["modId: String", "key: String"]),
            new CommandNode("add", "Adds a new key to the given mod's stored data with the provided value.", static args =>
            {
                ModData? data = ModData.StoredInstances.Find(md => md.ModID == args[0]);

                if (data is null)
                    WriteToConsole($"No data was found for mod [{args[0]}].", Color.red);
                else if (data.HasData(args[1]))
                    WriteToConsole($"Mod data for [{args[0]}] already has key \"{args[1]}\".");
                else
                {
                    data.AddData(args[1], CastFromString(args[2]));

                    WriteToConsole($"Added key \"{args[1]}\" to mod [{args[0]}] with value {args[2]}.");
                }
            }, ["modId: String", "key: String", "data: Object"]),
            new CommandNode("set", "Sets a given key in the mod's stored data to the provided value.", static args =>
            {
                ModData? data = ModData.StoredInstances.Find(md => md.ModID == args[0]);

                if (data is null)
                    WriteToConsole($"No data was found for mod [{args[0]}].", Color.red);
                else
                {
                    data.SetData(args[1], CastFromString(args[2]));

                    WriteToConsole($"Set key \"{args[1]}\" from mod [{args[0]}] to {args[2]}.");
                }
            }, ["modId: String", "key: String", "data: Object"]),
            new CommandNode("clear", "Clears all stored data of the specified mod.", static args =>
            {
                ModData? data = ModData.StoredInstances.Find(md => md.ModID == args[0]);

                if (data is null)
                    WriteToConsole($"No data was found for mod [{args[0]}].", Color.red);
                else
                {
                    data.ClearData();

                    WriteToConsole($"Cleared all data for mod [{args[0]}].");
                }
            }, ["modId: String"])),
        new CommandNode("echo", "Prints a given message to the console and ModLib's log file.", static args =>
        {
            string message = string.Join(" ", args);

            if (!string.IsNullOrWhiteSpace(message))
            {
                Core.Logger.LogMessage(message);
                WriteToConsole(message);
            }
        }, [".. message: String"]),
        new CommandNode("entrypoint", "Manages ModLib's main entrypoint class and its extensions.",
            new CommandNode("list", "Lists all currently loaded extension entrypoints.", static _ => WriteToConsole($"Loaded extension entrypoints:{ListElements(Entrypoint.LoadedExtensions.Select(static e => $"{e} ({e.Metadata.Name}; v{e.Metadata.Version})"))}")),
            new CommandNode("initialize", "Initializes ModLib's entrypoint class. Does nothing if ModLib was properly initialized.", static _ =>
            {
                if (!Entrypoint.IsInitialized)
                    Entrypoint.Initialize([], true);
                else
                    WriteToConsole("Entrypoint class is already initialized.", Color.red);
            })),
        new CommandNode("help", "Displays detailed help about a given command or subcommand, or all commands if none is specified.", static args =>
        {
            if (MainCommandTree is null) // impossible but makes the IDE happy
                WriteToConsole("Failed to retrieve command tree.", Color.red);
            else if (MainCommandTree.Children.Count is 0) // see above comment
                WriteToConsole("Main command tree has no children.");
            else if (args.Length > 0 && !args[0].Equals(MainCommandTree.Name, StringComparison.OrdinalIgnoreCase))
            {
                CommandNode? node;
                int i = 0;

                do
                {
                    node = MainCommandTree.GetChild(args[i]);
                    i++;
                } while (i < args.Length);

                if (node is null)
                    WriteToConsole($"Could not find command \"{string.Join(" ", args)}\".", Color.red);
                else
                    WriteToConsole($"* {node.Description}{Environment.NewLine}{Environment.NewLine}{node.GetHelp(showHelpNote: false)}");
            }
            else
                WriteToConsole($"Full syntax: {MainCommandTree.Name} [subcommand] [args?]+{Environment.NewLine}{Environment.NewLine}Valid subcommands:{ListElements(MainCommandTree.Children.Select(static node => node.GetInvocationSyntax()), "  ... ")}{Environment.NewLine}{Environment.NewLine}For more information about a subcommand, see `{MainCommandTree.Name} help <name>`.");
        }, [".. commandName: String"]),
        new CommandNode("keybind", "Manage keybinds registered by ModLib.",
            new CommandNode("list", "Lists all keybinds registered by ModLib.", static _ => WriteToConsole($"Keybinds:{(Core.InputModuleActivated ? ListElements(Keybind.Keybinds) : " None.")}")),
            new CommandNode("add", "Registers a new keybind with the specified arguments. If IIC:E is present, a PlayerKeybind is also registered to their API with this same data.", static args => Keybind.Register(args[0], args[1], (KeyCode)int.Parse(args[2]), (KeyCode)int.Parse(args[3]), (KeyCode)int.Parse(args[4])), ["id: String", "name: String", "keyboardKey: Int32", "gamepadKey: Int32", "xboxKey: Int32"]),
            new CommandNode("trigger", "Triggers a specified keybind as if a player had pressed it for a single frame. Does nothing if IIC:E is enabled.", static args =>
            {
                if (!AssertInGame() || !ParseInt32(args[1], out int playerNumber, 0, Keybind.MaxPlayers - 1, argName: "player index")) return;

                Keybind? keybind = Keybind.Get(args[1]);

                if (keybind is null)
                    WriteToConsole("No keybind was found with the given ID.", Color.red);
                else
                    keybind.ForceInput(playerNumber, true);
            }, ["keybindId: String", "playerIndex: Int32"])),
        new CommandNode("options", "Manage the collection of options shared by all mods registered to ModLib.",
            new CommandNode("list", "Lists all options in the shared collection.", static _ => WriteToConsole($"Shared options: [{SharedOptions.FormatOptions()}]")),
            new CommandNode("get", "Retrieves a given option from the shared collection by its key.", static args =>
            {
                if (!SharedOptions.MyOptions.TryGetValue(args[0], out ConfigurableBase value))
                    WriteToConsole($"No data was found with key \"{args[0]}\".", Color.red);
                else
                {
                    bool isOverride = SharedOptions.IsOverriden(args[0]);
                    bool isEphemeral = SharedOptions.IsEphemeral(args[0]);

                    WriteToConsole($"\"{args[0]}\": {value}{(isOverride || isEphemeral ? $"({(isOverride ? "Overriden" : "")}{(isOverride && isEphemeral ? "; " : "")}{(isEphemeral ? "Ephemeral" : "")})" : "")}");
                }
            }, ["key: String"]),
            new CommandNode("set", $"Sets a given key in the shared collection to the provided value.{Environment.NewLine}If set to ephemeral, the new value is removed on the next refresh/new cycle.", static args =>
            {
                SharedOptions.SetOption(args[0], CastFromString(args[1]), ValidateBoolean(in args, 2));

                WriteToConsole($"Set option \"{args[0]}\" to {SharedOptions.MyOptions[args[0]]}.");
            }, ["key: String", "value: Object", "isEphemeral: Boolean = False"])),
        new CommandNode("registry", "Manages the collection of mods registered to ModLib.",
            new CommandNode("get", "Retrieves all registered data for a given mod.", static args =>
            {
                Assembly? assembly = args[0].Equals(Core.MOD_GUID, StringComparison.OrdinalIgnoreCase)
                    ? Core.MyAssembly
                    : CompatibilityManager.LoadedPlugins.FirstOrDefault(p => p.Info.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.GetType().Assembly
                        ?? Entrypoint.LoadedExtensions.FirstOrDefault(e => e.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.GetType().Assembly;

                if (assembly is null)
                    WriteToConsole($"No mod could be found with GUID \"{args[0]}\".", Color.red);
                else if (!Registry.TryGetMod(assembly, out Registry.ModEntry? metadata))
                    WriteToConsole($"Mod [{args[0]}] is not registered to ModLib.", Color.red);
                else
                    WriteToConsole(metadata.ToString());
            }, ["modId: String"]),
            new CommandNode("add", "Registers a new mod to ModLib; Data is retrieved automatically from on the mod's entrypoint class.", static args =>
            {
                BepInPlugin? plugin = args[0].Equals(Core.MOD_GUID, StringComparison.OrdinalIgnoreCase)
                    ? Core.PluginData
                    : CompatibilityManager.LoadedPlugins.FirstOrDefault(p => p.Info.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.Info.Metadata
                        ?? Entrypoint.LoadedExtensions.FirstOrDefault(e => e.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.Metadata;

                Assembly? assembly = plugin?.GetType().Assembly;

                if (plugin is null || assembly is null)
                    WriteToConsole($"No mod could be found with GUID \"{args[0]}\".", Color.red);
                else if (Registry.TryGetMod(assembly, out _))
                    WriteToConsole($"Mod [{args[0]}] is already registered to ModLib.", Color.red);
                else
                {
                    Registry.RegisterAssembly(assembly, plugin, null, LoggingAdapter.CreateLogger(BepInEx.Logging.Logger.Sources.FirstOrDefault(s => s.SourceName == plugin.Name) as ManualLogSource ?? BepInEx.Logging.Logger.CreateLogSource(plugin.Name)));

                    WriteToConsole($"Successfully registered mod [{args[0]}] to ModLib.");
                }
            }, ["modId: String"]),
            new CommandNode("remove", "Removes all registered data from the given mod. Note if the specified mod later attempts to use a module or class which requires registration, it WILL throw an exception!", static args =>
            {
                Assembly? assembly = args[0].Equals(Core.MOD_GUID, StringComparison.OrdinalIgnoreCase)
                    ? Core.MyAssembly
                    : CompatibilityManager.LoadedPlugins.FirstOrDefault(p => p.Info.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.GetType().Assembly
                        ?? Entrypoint.LoadedExtensions.FirstOrDefault(e => e.Metadata.GUID.Equals(args[0], StringComparison.OrdinalIgnoreCase))?.GetType().Assembly;

                if (assembly is null)
                    WriteToConsole($"No mod could be found with GUID \"{args[0]}\".", Color.red);
                else if (!Registry.TryGetMod(assembly, out Registry.ModEntry? metadata))
                    WriteToConsole($"Mod [{args[0]}] is not registered to ModLib.", Color.red);
                else
                {
                    Registry.UnregisterAssembly(assembly);

                    WriteToConsole($"Removed mod [{args[0]}] from ModLib's registry.");
                }
            }, ["modId: String"])),
        new CommandNode("rpc", "Manage RPC events with a sprinkle of reflection. All operations require the Rain Meadow mod to be enabled.",
            new CommandNode("list", "Lists all outgoing RPC events managed by ModLib.", static args =>
            {
                if (!AssertOnlineSession()) return;

                RainMeadowAccess.ListRPCImpl();
            }),
            new CommandNode("invoke", "Invokes a RPC-marked method by its name.", static args =>
            {
                if (!AssertOnlineSession()) return;

                if (!ushort.TryParse(args[1], NumberStyles.Integer, CultureInfo.CurrentCulture, out ushort playerNumber))
                    WriteToConsole("The specified ID is not valid.", Color.red);
                else if (!RainMeadowAccess.InvokeRPCImpl(args[0], playerNumber, [.. args.Skip(2).Select(CastFromString)]))
                    WriteToConsole($"Failed to send RPC to player #{playerNumber}.", Color.red);
                else
                    WriteToConsole($"Successfully sent RPC to player #{playerNumber}.");
            }, ["methodName: String", "targetPlayer: UInt16", ".. args: Object"]),
            new CommandNode("abort", "Aborts a ModLib-managed RPC event; The specified event will resolve with a result of type `GenericResult.Error`.", static args =>
            {
                if (!AssertOnlineSession()) return;

                RainMeadowAccess.AbortRPCImpl(args[0]);
            }, ["index: Int32"])));

    private static class RainMeadowAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InvokeRPCImpl(string methodName, ushort targetPlayer, object[] args)
        {
            MethodInfo? method = null;

            _ = AssemblyExtensions.GetAllTypes().LastOrDefault(t => (method = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)) is not null);

            if (method is null) return false;

            OnlinePlayer? onlinePlayer = OnlineManager.lobby.PlayerFromId(targetPlayer);

            if (onlinePlayer is null) return false;

            Type delegateType = Expression.GetDelegateType([.. method.GetParameters().Select(static p => p.ParameterType), method.ReturnType]);

            onlinePlayer.SendRPCEvent(method.DeclaringType.GetMethod(method.Name).CreateDelegate(delegateType), args);

            return true;
        }

        internal static void AbortRPCImpl(string arg)
        {
            if (!ParseInt32(arg, out int index, 0, ModRPCManager._activeRPCs.Count - 1, argName: "index")) return;

            ModRPCManager._activeRPCs[index].Abort();

            WriteToConsole($"RPC #{index} was interrupted.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ListRPCImpl() => WriteToConsole($"Pending RPCs:{ListElements(ModRPCManager._activeRPCs)}");
    }
}