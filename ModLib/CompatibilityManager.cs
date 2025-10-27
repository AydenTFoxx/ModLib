using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ModLib;

/// <summary>
///     Simple helper for determining the presence of other mods and ensure mod compatibility.
/// </summary>
public static class CompatibilityManager
{
    private static readonly Dictionary<string, bool> ManagedMods = [];

    /// <summary>
    ///     Clears the internal dictionary of cached mods.
    /// </summary>
    public static void Clear() => ManagedMods.Clear();

    /// <summary>
    ///     Determines if a given mod is currently enabled.
    /// </summary>
    /// <param name="modID">The ID of the mod to check for.</param>
    /// <returns><c>true</c> if the given mod was found to be enabled, <c>false</c> otherwise.</returns>
    public static bool IsModEnabled(string modID) =>
        ManagedMods.TryGetValue(modID, out bool value)
            ? value
            : ModManager.ActiveMods.Any(mod => mod.id == modID);

    /// <summary>
    ///     Determines if either Improved Input Config or Improved Input Config: Extended are enabled.
    /// </summary>
    /// <returns><c>true</c> if one of these mods is enabled, <c>false</c> otherwise.</returns>
    public static bool IsIICEnabled() => IsModEnabled("improved-input-config");

    /// <summary>
    ///     Determines if the Rain Meadow mod is enabled.
    /// </summary>
    /// <returns><c>true</c> if the mod is enabled, <c>false</c> otherwise.</returns>
    public static bool IsRainMeadowEnabled() => IsModEnabled("henpemaz_rainmeadow");

    /// <summary>
    ///     Overrides the configured compatibility features for a given mod.
    /// </summary>
    /// <param name="modID">The identifier of the mod.</param>
    /// <param name="enable">Whether or not compatibility with the given mod should be enabled.</param>
    public static void SetModCompatibility(string modID, bool enable) => ManagedMods[modID] = enable;

    internal static void Initialize(IList<string> compatibilityPaths) => ConfigLoader.Initialize(compatibilityPaths);

    private static class ConfigLoader
    {
        private static readonly string PathToLocalMods = Path.Combine(Application.streamingAssetsPath, "mods");

        private static bool _initialized;

        /// <summary>
        ///     Initializes the <see cref="CompatibilityManager"/> with the provided list of paths to config files.
        /// </summary>
        /// <param name="compatibilityPaths">The list of paths to config files for mod IDs.</param>
        public static void Initialize(IList<string> compatibilityPaths)
        {
            if (_initialized) return;

            List<string[]> userModIDs = [];
            bool queryModInfo = false;

            foreach (string filePath in compatibilityPaths)
            {
                userModIDs.AddRange(ReadCompatibilityFile(filePath, ref queryModInfo));
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

                    configuredModIDs.Add([.. currentModIDs.Union(modIDs)]);
                }
                else
                {
                    configuredModIDs.Add(modIDs);
                }
            }

            CheckModCompats(configuredModIDs, queryModInfo);

            _initialized = true;
        }

        /// <summary>
        ///     Queries the client's list of enabled mods for toggling compatibility features.
        /// </summary>
        /// <param name="supportedModIDs">The list of mod IDs to be queried.</param>
        /// <param name="queryModInfo">
        ///     If true, performs a deep search where every mod's manifest is queried for the given IDs.
        ///     This should be avoided if possible, as it is considerably more expensive than just checking for directory names.
        /// </param>
        private static void CheckModCompats(IEnumerable<string[]> supportedModIDs, bool queryModInfo)
        {
            Core.Logger.LogDebug("Checking compatibility mods...");

            try
            {
                StreamReader reader = new(Path.Combine(Application.streamingAssetsPath, "enabledMods.txt"));

                while (!reader.EndOfStream)
                {
                    string modID = reader.ReadLine();
                    string? pathToMod = queryModInfo ? null : "?";

                    if (modID.StartsWith("[WORKSHOP]"))
                    {
                        pathToMod ??= modID.Replace("[WORKSHOP]", "");

                        modID = modID.Split('\\').Last();
                    }
                    else
                    {
                        pathToMod ??= Path.Combine(PathToLocalMods, modID);
                    }

                    if (queryModInfo)
                    {
                        string pathToModInfo = Path.Combine(pathToMod, "modinfo.json");

                        string? trueModID = GetModGuid(pathToModInfo);
                        if (!string.IsNullOrWhiteSpace(trueModID))
                        {
                            modID = trueModID!;
                        }
                    }

                    foreach (string[] supportedIDs in supportedModIDs)
                    {
                        if (supportedIDs.Contains(modID))
                        {
                            modID = supportedIDs[0];

                            ManagedMods.Add(modID, true);

                            Core.Logger.LogInfo($"Added compatibility layer for: {modID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger.LogError($"Failed to read enabled mods file! {ex}");
            }
        }

        private static string? GetModGuid(string pathToJson)
        {
            using StreamReader reader = File.OpenText(pathToJson);

            object result = Json.Parser.Parse(reader.ReadToEnd());

            return (string?)(result as IDictionary<string, object>)?["id"];
        }

        private static List<string[]> ReadCompatibilityFile(string path, ref bool queryModInfo)
        {
            using StreamReader reader = File.OpenText(path);

            List<string[]> modIDs = [];

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                if (line.StartsWith("@"))
                {
                    queryModInfo = true;
                }

                modIDs.Add([.. line.Split([",", "//"], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim())]);
            }

            return modIDs;
        }

        private sealed class ModIDEqualityComparer : IEqualityComparer<string[]>
        {
            public bool Equals(string[] x, string[] y) =>
                x[0].Equals(y[0], StringComparison.OrdinalIgnoreCase) || x.Intersect(y).Count() > 0;

            public int GetHashCode(string[] obj) =>
                obj is null ? throw new ArgumentNullException(nameof(obj)) : obj.GetHashCode();
        }
    }
}