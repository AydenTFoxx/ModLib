using System;
using System.Runtime.Serialization;

namespace ModLib.Debug;

[Serializable]
public class InvalidCommandException : InvalidOperationException
{
    public InvalidCommandException() { }
    public InvalidCommandException(string message) : base(message) { }
    public InvalidCommandException(string message, Exception inner) : base(message, inner) { }
    protected InvalidCommandException(
        SerializationInfo info,
        StreamingContext context) : base(info, context) { }
}