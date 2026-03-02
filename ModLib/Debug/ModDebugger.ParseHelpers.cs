using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ModLib.Debug;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static partial class ModDebugger
{
    private const int MAX_CAST_RECURSION = 20;

    private const NumberStyles UnsignedNumberStyles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
    private const NumberStyles SignedNumberStyles = UnsignedNumberStyles | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowParentheses;

    public static object CastFromString(string value, int r = 0)
    {
        if (value.StartsWith("[", StringComparison.OrdinalIgnoreCase) && value.EndsWith("]", StringComparison.OrdinalIgnoreCase))
        {
            if (r > MAX_CAST_RECURSION)
                throw new OverflowException("Too many nested objects.");

            string[] contents = value.Substring(1, value.Length - 2).Split([','], StringSplitOptions.RemoveEmptyEntries);

            return contents.Length is 0
                ? []
                : new object[] { contents.Select(str => CastFromString(str.Trim(), ++r)) };
        }

        return bool.TryParse(value, out bool b)
            ? b
            : byte.TryParse(value, UnsignedNumberStyles, CultureInfo.InvariantCulture, out byte y)
            ? y
            : sbyte.TryParse(value, SignedNumberStyles, CultureInfo.InvariantCulture, out sbyte sy)
            ? sy
            : short.TryParse(value, SignedNumberStyles, CultureInfo.InvariantCulture, out short s)
            ? s
            : ushort.TryParse(value, UnsignedNumberStyles, CultureInfo.InvariantCulture, out ushort us)
            ? us
            : int.TryParse(value, SignedNumberStyles | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int i)
            ? i
            : uint.TryParse(value, UnsignedNumberStyles | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out uint ui)
            ? ui
            : long.TryParse(value, SignedNumberStyles | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long L)
            ? L
            : ulong.TryParse(value, UnsignedNumberStyles | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out ulong uL)
            ? uL
            : float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
            ? f
            : double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            ? d
            : decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal m)
            ? m
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out DateTime dt)
            ? dt
            : value;
    }

    public static AbstractCreature? GetCreatureById(int id) =>
        RWCustom.Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game
            ? game.world.abstractRooms.SelectMany(static ar => ar.creatures).FirstOrDefault(ac => ac.ID.number == id)
            : null;

    public static AbstractPhysicalObject? GetObjectById(int id) =>
        RWCustom.Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game
            ? game.world.abstractRooms.SelectMany(static ar => ar.entities).OfType<AbstractPhysicalObject>().FirstOrDefault(apo => apo.ID.number == id)
            : null;

    public static bool ParseBoolean(string value, out bool result, bool silent = false)
    {
        if (!bool.TryParse(value, out result))
        {
            if (!silent)
                WriteToConsole($"The specified boolean is not valid.", Color.red);
            return false;
        }
        return true;
    }

    public static bool ParseInt32(string value, out int result, int min = int.MinValue, int max = int.MaxValue, bool silent = false, string argName = "number")
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result) || result < min || result > max)
        {
            if (!silent)
                WriteToConsole($"The specified {argName} is not valid; Must be a value between {min} and {max}.", Color.red);
            return false;
        }
        return true;
    }

    public static bool ParseUInt16(string value, out ushort result, ushort min = ushort.MinValue, ushort max = ushort.MaxValue, bool silent = false, string argName = "number")
    {
        if (!ushort.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result) || result < min || result > max)
        {
            if (!silent)
                WriteToConsole($"The specified {argName} is not valid; Must be a value between {min} and {max}.", Color.red);
            return false;
        }
        return true;
    }

    public static bool ParseSingle(string value, out float result, float min = float.MinValue, float max = float.MaxValue, bool silent = false, string argName = "float")
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) || result < min || result > max)
        {
            if (!silent)
                WriteToConsole($"The specified {argName} is not valid; Must be a value between {min} and {max}.", Color.red);
            return false;
        }
        return true;
    }

    public static bool ValidateBoolean(in string[] args, int index, bool @default = false)
    {
        return args.Length > index
            ? bool.TryParse(args[index], out bool result)
                ? result
                : throw new SyntaxException($"Expected boolean at index {index}, but got: {args[index]}")
            : @default;
    }
}