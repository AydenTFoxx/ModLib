using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using ModLib.Extensions;

namespace ModLib.Loader;

/// <summary>
///     Specifies that a given class is the entrypoint for a ModLib extension assembly. This class cannot be inherited.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModLibExtensionAttribute : Attribute
{
    internal static readonly List<EntrypointData> loadedExtensions = [];

    internal static void LoadAllEntrypoints()
    {
        IEnumerable<Type> types = AssemblyExtensions.GetAllTypes().Where(static t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<ModLibExtensionAttribute>(false) is not null);

        foreach (Type type in types)
        {
            try
            {
                BepInPlugin? metadata = (BepInPlugin)type.GetProperty("Metadata", BindingFlags.Public | BindingFlags.Static, null, typeof(BepInPlugin), [], null).GetValue(null);

                if (metadata is null) continue;

                MethodInfo? initMethod = type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static, null, [], null);
                MethodInfo? disableMethod = type.GetMethod("Disable", BindingFlags.Public | BindingFlags.Static, null, [], null);

                if (initMethod is null && disableMethod is null) continue;

                Core.Logger.LogInfo($"Initializing extension assembly: {ParseMetadata(metadata)} ({type.Assembly})");

                Registry.RegisterAssembly(type.Assembly, metadata, null, null);

                initMethod?.Invoke(null, []);

                loadedExtensions.Add(new EntrypointData(type.AssemblyQualifiedName, metadata, initMethod, disableMethod));
            }
            catch (Exception ex)
            {
                Core.Logger.LogError($"Failed to initialize extension assembly: {type.AssemblyQualifiedName}!");
                Core.Logger.LogError($"Exception: {ex}");
            }
        }
    }

    internal static void UnloadAllEntrypoints()
    {
        for (int i = loadedExtensions.Count - 1; i >= 0; i--)
        {
            EntrypointData entrypoint = loadedExtensions[i];

            try
            {
                Core.Logger.LogInfo($"Unloading extension assembly: {ParseMetadata(entrypoint.Metadata)}");

                entrypoint.Disable();
            }
            catch (Exception ex)
            {
                Core.Logger.LogError($"Failed to disable extension assembly: {entrypoint.AssemblyQualifiedName}");
                Core.Logger.LogError($"Exception: {ex}");
            }

            loadedExtensions.RemoveAt(i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ParseMetadata(BepInPlugin metadata) => $"[{metadata.Name} | {metadata.GUID} | {metadata.Version}]";

    internal sealed record EntrypointData
    {
        private readonly MethodInfo? _initMethod;
        private readonly MethodInfo? _disableMethod;

        public string AssemblyQualifiedName { get; }
        public BepInPlugin Metadata { get; }

        public EntrypointData(string assemblyQualifiedName, BepInPlugin metadata, MethodInfo? initMethod, MethodInfo? disableMethod)
        {
            AssemblyQualifiedName = assemblyQualifiedName;
            Metadata = metadata;

            _initMethod = initMethod;
            _disableMethod = disableMethod;
        }

        public void Initialize() => _initMethod?.Invoke(null, []);

        public void Disable() => _disableMethod?.Invoke(null, []);
    }
}