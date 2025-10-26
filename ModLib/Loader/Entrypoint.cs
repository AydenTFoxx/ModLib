using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace ModLib.Loader;

/// <summary>
///     ModLib's entrypoint class, used by its loader to intialize critical systems as early as possible.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Entrypoint
{
    /// <summary>
    ///     Determines if ModLib's assembly was successfully loaded by the loader entrypoint.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Initialized { get; private set; }

    /// <summary>
    ///     Forces ModLib's internal systems to initialize as soon as possible.
    ///     Allows some cool things to be done before other ModLib-dependant mods are loaded.
    /// </summary>
    /// <remarks>
    ///     Unless directly working with ModLib's loading process, you should never have the need to call this method.
    /// </remarks>
    /// <param name="compatibilityPaths">The list of paths to <c>CompatibilityMods.txt</c> files.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Initialize(List<string> compatibilityPaths)
    {
        if (Initialized) return;

        List<string[]> userModIDs = [];

        foreach (string filePath in compatibilityPaths)
        {
            userModIDs.AddRange(ReadCompatibilityFile(filePath));
        }

        HashSet<string[]> configuredModIDs = new(
            [
                ["henpemaz_rainmeadow", "3388224007"],  // Rain Meadow
                ["improved-input-config", "3458119961"] // Improved Input Config: Extended
            ],
            new ModIDEqualityComparer()
        );

        foreach (string[] modIDs in userModIDs)
        {
            if (configuredModIDs.TryGetValue(modIDs, out string[] currentModIDs))
            {
                configuredModIDs.Remove(currentModIDs);

                configuredModIDs.Add([.. modIDs.Union(currentModIDs)]);
            }
            else
            {
                configuredModIDs.Add(modIDs);
            }
        }

        CompatibilityManager.CheckModCompats(configuredModIDs);

        Initialized = true;
    }

    private static List<string[]> ReadCompatibilityFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new(stream);

        List<string[]> modIDs = [];

        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

            modIDs.Add([.. line.Split(',').Select(s => s.Trim())]);
        }

        return modIDs;
    }

    private class ModIDEqualityComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            return x[0].Equals(y[0], StringComparison.OrdinalIgnoreCase) || x.Intersect(y).Count() > 0;
        }

        public int GetHashCode(string[] obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.GetHashCode();
        }
    }
}