using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ModLib.Debug;

public sealed class CommandNode : IEquatable<CommandNode>
{
    private readonly List<CommandNode> _children = [];

    private readonly int _minArgsRequired;
    private readonly bool _enforceMaxArgsLength = true;

    public string Name { get; }
    public string Description
    {
        get
        {
            field ??= IsRoot
                ? HasChildren
                    ? "The base for all commands. Cannot be used on its own."
                    : $"Invokes a simple command. {(_enforceMaxArgsLength ? "Requires" : "May require")} {(Arguments.Length == _minArgsRequired ? $"{Arguments.Length} arguments" : $"up to {Arguments.Length} arguments, {_minArgsRequired} of which are required")}.{(_enforceMaxArgsLength ? $" Throws a SyntaxException if the amount of arguments is not {(Arguments.Length == _minArgsRequired ? "the exact value specified" : "within the specified range")}." : "")}"
                : HasChildren
                    ? $"Subcommand of {Parent}. Cannot be used on its own."
                    : $"Invokes a subcommand of {Parent}. {(_enforceMaxArgsLength ? "Requires" : "May require")} {(Arguments.Length == _minArgsRequired ? $"{Arguments.Length} arguments" : $"up to {Arguments.Length} arguments, {_minArgsRequired} of which are required")}.{(_enforceMaxArgsLength ? $" Throws a SyntaxException if the amount of arguments is not {(Arguments.Length == _minArgsRequired ? "the exact value specified" : "within the specified range")}." : "")}";

            return field;
        }
    }

    public Action<string[]>? Command { get; }
    public string[] Arguments { get; }

    public CommandNode? Parent { get; private set; }

    public ReadOnlyCollection<CommandNode> Children => _children.AsReadOnly();

    [MemberNotNullWhen(false, nameof(Parent))]
    public bool IsRoot => Parent is null;
    public bool HasChildren => _children.Count > 0;

    [MemberNotNullWhen(true, nameof(Parent))]
    public bool IsLeaf => this is { Parent: not null, _children.Count: 0 };

    public int Depth => Parent is null ? 0 : Parent.Depth + 1;

    public CommandNode TopLevelParent => Parent is null ? this : Parent.TopLevelParent;

    public CommandNode[] Ancestors => Parent is null ? [] : [.. Parent.Ancestors, Parent];
    public CommandNode[] Descendants => [.. _children.SelectMany(static child => child.Flatten())];

    public CommandNode(string name, string? description, params CommandNode[] children)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "Name cannot be null.");

        Name = name;
        Description = description;

        Arguments = [];

        AddChildren(children);
    }

    public CommandNode(string name, string? description, Action<string[]> command, string[]? argsNames = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "Name cannot be null.");

        Name = name;
        Description = description;

        Command = command;
        Arguments = argsNames ?? [];

        if (argsNames is not null)
        {
            int firstOptionalIndex = Array.IndexOf(argsNames, argsNames.FirstOrDefault(static s => s.Contains('=')));

            _minArgsRequired = firstOptionalIndex > -1
                ? Math.Max(firstOptionalIndex - 1, 0)
                : argsNames.Length;

            string paramArg = argsNames.FirstOrDefault(static arg => arg.StartsWith(".."));

            if (!string.IsNullOrEmpty(paramArg))
            {
                _enforceMaxArgsLength = false;

                if (Array.IndexOf(argsNames, paramArg) != argsNames.Length - 1)
                    throw new ArgumentException("A params parameter must be the last parameter in a parameter list.", nameof(argsNames));

                if (paramArg.Contains('='))
                    throw new ArgumentException("Cannot specify a default value for a parameter collection.", nameof(argsNames));

                if (firstOptionalIndex is -1)
                    _minArgsRequired = Math.Max(_minArgsRequired - 1, 0);
            }
        }
    }

    public void AddChild(CommandNode node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node), "Child node cannot be null.");

        _children.Add(node);
        _children.Sort(static (x, y) => x.Name.CompareTo(y.Name));

        node.Parent = this;
    }

    public void AddChildren(params CommandNode[] nodes)
    {
        foreach (CommandNode node in nodes)
            AddChild(node);
    }

    public CommandNode GetChild(string name) => _children.Find(child => child.Name == name);

    public bool RemoveChild(string name) => RemoveChild(GetChild(name));

    public bool RemoveChild(CommandNode node)
    {
        if (node is not null && _children.Remove(node))
        {
            _children.Sort(static (x, y) => x.Name.CompareTo(y.Name));

            node.Parent = null;
            return true;
        }
        return false;
    }

    public void Traverse(Action<CommandNode> action)
    {
        action.Invoke(this);

        for (int i = _children.Count - 1; i >= 0; i--)
            _children[i].Traverse(action);
    }

    public List<CommandNode> Flatten() => [this, .. _children.SelectMany(static child => child.Flatten())];

    public string GetHelp(bool showHelpNote = true) => $"Usage: {(IsRoot ? "" : $"{string.Join(" ", Ancestors.Select(static node => node.Name))} ")}{GetInvocationSyntax()}{(!IsRoot && showHelpNote ? $"{Environment.NewLine}For more details, see `{TopLevelParent.Name} help {(Ancestors.Length > 1 ? $"{string.Join(" ", Ancestors.Except([TopLevelParent]).Select(static node => node.Name))} " : "")}{Name}`" : "")}";

    public string GetInvocationSyntax(bool showArgs = true) => $"{Name}{(HasChildren ? $" [{string.Join(IsRoot || showArgs ? " | " : "|", _children.Select(d => d.GetInvocationSyntax(showArgs)))}]" : Arguments.Length is not 0 ? (showArgs ? $" [{string.Join("] [", Arguments)}]" : "..") : null)}";

    public void Run(string[] args)
    {
        if (HasChildren)
        {
            if (args.Length is 0)
                throw new SyntaxException("No subcommand was specified.", this);

            (_children.SingleOrDefault(c => c.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidCommandException($"Could not find subcommand with name \"{args[0].ToLowerInvariant()}\".")).Run([.. args.Skip(1)]);
        }
        else
        {
            if (args.Length < _minArgsRequired || (_enforceMaxArgsLength && args.Length > Arguments.Length))
                throw new SyntaxException($"Expected {(_minArgsRequired != Arguments.Length ? $"{_minArgsRequired} to {Arguments.Length}" : _minArgsRequired)} argument{(_minArgsRequired != 1 ? "s" : "")}, but got {args.Length}.", this);

            if (Command is null)
                throw new InvalidCommandException("Child node has no registered command or children.");

            Command.Invoke(args);
        }
    }

    public bool Equals(CommandNode other) =>
        other is not null
        && other.Name == Name
        && other.Parent == Parent
        && other._children == _children
        && other.Command == Command
        && other.Arguments == Arguments;

    public override bool Equals(object obj) => obj is CommandNode other && Equals(other);

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => $"Node \"{Name}\" -> {Parent?.Name ?? "ROOT"}; {(HasChildren ? $"{_children.Count} children" : $"Command: {Command} ({Arguments.Length} arguments)")}";

    public static bool operator ==(CommandNode? x, CommandNode? y)
    {
        return x is null
            ? y is null
            : y is not null && x.Equals(y);
    }

    public static bool operator !=(CommandNode? x, CommandNode? y)
    {
        return x is null
            ? y is not null
            : y is null || !x.Equals(y);
    }
}