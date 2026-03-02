using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ModLib.Extensions;

/// <summary>
///     Utility and extension methods for handling assemblies.
/// </summary>
/// <remarks>
///     Credit goes to LogUtils by Fluffball (@TheVileOne) for the original LogUtils.Helpers.AssemblyUtils code.
/// </remarks>
internal static class AssemblyExtensions
{
    private static Type[] LoadedTypes
    {
        get
        {
            field ??= [.. AppDomain.CurrentDomain.GetAssemblies().SelectMany(static assembly => assembly.GetTypesSafely())];
            return field;
        }
        set => field = value;
    }

    /// <summary>
    ///     Safely retrieves all types from all loaded assemblies in the current domain.
    /// </summary>
    /// <returns>A collection containing all loaded types in the current domain.</returns>
    public static IEnumerable<Type> GetAllTypes(bool cacheResults = true) =>
        cacheResults
            ? LoadedTypes
            : [.. AppDomain.CurrentDomain.GetAssemblies().SelectMany(static assembly => assembly.GetTypesSafely())];

    /// <summary>
    ///     Retrieves all types from a given assembly while safely handling thrown exceptions.
    /// </summary>
    /// <param name="assembly">The assembly whose types will be retrieved.</param>
    /// <returns>A collection containing all loaded types of the given assembly.</returns>
    public static IEnumerable<Type> GetTypesSafely(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static t => t is not null);
        }
    }

    /// <summary>
    ///     Get the first calling assembly that is not the executing assembly via a stack trace.
    /// </summary>
    /// <remarks>
    ///     Credit for this code goes to WilliamCruisoring and LogUtils.
    /// </remarks>
    /// <returns>The first assembly in the stack trace that isn't ModLib, or <c>null</c> if none is found.</returns>
    public static Assembly? GetCallingAssembly()
    {
        Assembly myAssembly = Core.MyAssembly;

        StackFrame[] frames = new StackTrace().GetFrames();
        for (int i = 0; i < frames.Length; i++)
        {
            Assembly callerAssembly = frames[i].GetMethod().DeclaringType.Assembly;

            if (callerAssembly != myAssembly)
                return callerAssembly;
        }

        return null;
    }
}