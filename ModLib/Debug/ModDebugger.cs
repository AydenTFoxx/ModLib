using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DevConsole.Commands;
using UnityEngine;

namespace ModLib.Debug;

/// <summary>
///
/// </summary>
public static partial class ModDebugger
{
    internal static void RegisterCommands()
    {
        new CommandBuilder("modlib")
            .Run(MainCommandHandler)
            .AutoComplete(MainAutoCompleteHandler)
            .HideHelp()
            .Register();
    }

    private static void MainCommandHandler(string[] args)
    {
        try
        {
            MainCommandTree.Run(args);
        }
        catch (InvalidCommandException ex)
        {
            WriteToConsole($"Command exception: {ex.Message}{Environment.NewLine}{Environment.NewLine}{ex.StackTrace}", Color.red);
        }
        catch (SyntaxException ex)
        {
            WriteToConsole($"Syntax exception: {ex.Message}{Environment.NewLine}{Environment.NewLine}{ex.StackTrace}", Color.red);

            if (ex.Node is not null)
                WriteToConsole($"{Environment.NewLine}{ex.Node.GetHelp()}");
        }
        catch (Exception ex)
        {
            WriteToConsole($"An error occurred while running this command.{Environment.NewLine}{Environment.NewLine}Exception: {ex}", Color.red);
        }
    }

    private static IEnumerable<string> MainAutoCompleteHandler(string[] args)
    {
        CommandNode node = MainCommandTree;
        int i = 0;

        if (args.Length is not 0)
        {
            while (i < args.Length)
            {
                CommandNode newNode = node.GetChild(args[i]);

                if (newNode is null)
                    break;

                node = newNode;
                i++;
            }
        }

        if (node.HasChildren)
            return node.Children.Select(static child => child.Name);
        else if (node.Arguments.Length > 0)
        {
            string? currentArg = node.Arguments.Select(static arg => arg.Insert(0, "help-")).ElementAtOrDefault(args.Length - i);

            return currentArg is not null
                ? [currentArg]
                : node.Arguments.Last().StartsWith("..")
                    ? [node.Arguments.Last().Insert(0, "help-")]
                    : [];
        }
        else return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AssertInGame()
    {
        if (UnityEngine.Object.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is not RainWorldGame)
        {
            WriteToConsole("This command can only run while in-game!");
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AssertOnlineSession()
    {
        if (!Extras.IsOnlineSession)
        {
            WriteToConsole("This command can only run while in a Rain Meadow lobby!");
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ListElements<T>(IEnumerable<T> values, string prefix = "- ") =>
        values.Any() ? $"{Environment.NewLine}{prefix}{string.Join($"{Environment.NewLine}{prefix}", values)}" : " None.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ListElements<TKey, TValue>(IDictionary<TKey, TValue> keyValuePairs, string prefix = "- ") =>
        keyValuePairs.Count > 0 ? $"{Environment.NewLine}{prefix}{string.Join($"{Environment.NewLine}{prefix}", keyValuePairs)}" : " None.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToConsole(string message) => DevConsole.GameConsole.WriteLine(message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToConsole(string message, Color color) => DevConsole.GameConsole.WriteLine(message, color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToConsoleThreaded(string message) => DevConsole.GameConsole.WriteLineThreaded(message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToConsoleThreaded(string message, Color color) => DevConsole.GameConsole.WriteLineThreaded(message, color);
}