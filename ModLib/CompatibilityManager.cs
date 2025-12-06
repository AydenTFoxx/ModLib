using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;

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

    internal sealed class ConfigLoader(ManualLogSource logger) : IDisposable
    {
        private static readonly string PathToLocalMods = Path.Combine(Core.StreamingAssetsPath, "mods");

        private List<string> AdvancedSearchIDs = [];
        private bool disposedValue;

        /// <summary>
        ///     Initializes the <see cref="CompatibilityManager"/> with the provided list of paths to config files.
        /// </summary>
        /// <param name="compatibilityPaths">The list of paths to config files for mod IDs.</param>
        public void Initialize(IList<string> compatibilityPaths)
        {
            List<string[]> userModIDs = [];

            foreach (string filePath in compatibilityPaths)
            {
                logger.LogDebug($"Reading file: {filePath}");

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

                    configuredModIDs.Add([.. currentModIDs.Union(modIDs)]);
                }
                else
                {
                    configuredModIDs.Add(modIDs);
                }
            }

            CheckModCompats(configuredModIDs);
        }

        /// <summary>
        ///     Queries the client's list of enabled mods for toggling compatibility features.
        /// </summary>
        /// <param name="supportedModIDs">The list of mod IDs to be queried.</param>
        private void CheckModCompats(IEnumerable<string[]> supportedModIDs)
        {
            logger.LogDebug("Checking compatibility mods...");

            try
            {
                using StreamReader reader = new(Path.Combine(Core.StreamingAssetsPath, "enabledMods.txt"));

                while (!reader.EndOfStream)
                {
                    bool queryModInfo = AdvancedSearchIDs.Count > 0;

                    string modID = reader.ReadLine();
                    string? pathToMod = queryModInfo ? null : "?";

                    if (modID.StartsWith("[WORKSHOP]"))
                    {
                        pathToMod ??= modID.Replace("[WORKSHOP]", "");

                        modID = modID.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();
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
                            modID = trueModID;
                        }

                        if (AdvancedSearchIDs.Contains(trueModID))
                        {
                            logger.LogDebug($"Found {trueModID} with advanced query.");

                            AdvancedSearchIDs.Remove(trueModID);
                        }
                    }

                    foreach (string[] supportedIDs in supportedModIDs)
                    {
                        if (supportedIDs.Contains(modID))
                        {
                            modID = supportedIDs[0];

                            ManagedMods.Add(modID, true);

                            logger.LogInfo($"Added compatibility layer for: {modID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to read enabled mods file! {ex}");
            }
        }

        private List<string[]> ReadCompatibilityFile(string path)
        {
            using StreamReader reader = File.OpenText(path);

            List<string[]> modIDs = [];

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                line = line.Split(["//"], StringSplitOptions.None)[0];

                if (line.StartsWith("@"))
                {
                    line = line.Remove(0, 1);

                    AdvancedSearchIDs.Add(line);
                }

                modIDs.Add([.. line.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim())]);
            }

            return modIDs;
        }

        private static string GetModGuid(string pathToJson)
        {
            using StreamReader reader = File.OpenText(pathToJson);

            return Json.Parser.Parse(reader.ReadToEnd()) is Dictionary<string, object> jsonObject
                ? (string)jsonObject["id"]
                : string.Empty;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AdvancedSearchIDs?.Clear();
                    AdvancedSearchIDs = null!;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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