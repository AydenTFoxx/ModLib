using System;
using System.Runtime.Serialization;

namespace ModLib.Debug;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[Serializable]
public class SyntaxException : ArgumentException
{
    public CommandNode? Node { get; }

    public SyntaxException() { }
    public SyntaxException(string message) : base(message) { }
    public SyntaxException(string message, Exception inner) : base(message, inner) { }
    public SyntaxException(string message, CommandNode node) : base(message) { Node = node; }
    public SyntaxException(string message, CommandNode node, Exception inner) : base(message, inner) { Node = node; }
    protected SyntaxException(
        SerializationInfo info,
        StreamingContext context) : base(info, context) { }
}