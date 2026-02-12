using System;
using System.Collections.Generic;
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
    /// <summary>
    ///     Safely retrieves all types from all loaded assemblies in the current domain.
    /// </summary>
    /// <returns>A collection containing all loaded types in the current domain.</returns>
    public static IEnumerable<Type> GetAllTypes() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(static assembly => assembly.GetTypesSafely());

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
}
