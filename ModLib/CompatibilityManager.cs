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
    ///     Queries the client's list of enabled mods for toggling compatibility features.
    /// </summary>
    /// <param name="supportedModIDs">The list of mod IDs to be queried.</param>
    internal static void CheckModCompats(IEnumerable<string[]> supportedModIDs)
    {
        Core.Logger.LogDebug("Checking compatibility mods...");

        try
        {
            StreamReader reader = new(Path.Combine(Application.streamingAssetsPath, "enabledMods.txt"));

            while (!reader.EndOfStream)
            {
                string modID = reader.ReadLine();

                if (modID.StartsWith("[WORKSHOP]"))
                    modID = modID.Split('\\').Last();

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
}