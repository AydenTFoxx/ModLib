using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.MultiFolderLoader;
using Mono.Cecil;
using AssemblyCandidate = (System.Version Version, string Path);

// Credits to LogUtils by Fluffball (@TheVileOne) for original LogUtils.VersionLoader code

namespace ModLib.Loader;

public static class Patcher
{
    internal static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModLib.Loader");

    private static bool loadedAssembly;
    private static readonly List<string> CompatibilityPaths = [];

    public static IEnumerable<string> TargetDLLs => GetDLLs();

    public static IEnumerable<string> GetDLLs()
    {
        yield return "BepInEx.MultiFolderLoader.dll";

        Logger.LogMessage($"Looking for ModLib assemblies...");

        AssemblyCandidate target = AssemblyUtils.FindLatestAssembly(GetSearchPaths(true), "ModLib.dll");

        if (target.Path != null)
        {
            Logger.LogMessage($"Loading latest ModLib DLL: {AssemblyUtils.FormatCandidate(target, true)}");

            Assembly.LoadFrom(target.Path);

            AssemblyCandidate targetObjects = AssemblyUtils.FindLatestAssembly(GetSearchPaths(false), "ModLib.Objects.dll");

            if (targetObjects.Path != null)
            {
                Logger.LogMessage($"Loading latest ModLib.Objects DLL: {AssemblyUtils.FormatCandidate(targetObjects, true)}");

                Assembly.LoadFrom(targetObjects.Path);
            }

            loadedAssembly = true;
        }
        else
        {
            Logger.LogInfo("No ModLib assembly found.");
        }
    }

    private static IEnumerable<string> GetSearchPaths(bool doCompatFileCheck = true)
    {
        foreach (Mod mod in ModManager.Mods)
        {
            // Retrieve the mod's CompatibilityMods.txt file, if there is any
            if (doCompatFileCheck)
            {
                string? compatFilePath = Directory.GetFiles(mod.ModDir, "CompatibilityMods.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (compatFilePath is not null)
                {
                    Logger.LogInfo($"Found CM config file: {AssemblyUtils.GetModName(compatFilePath, true)}");

                    CompatibilityPaths.Add(compatFilePath);
                }
            }

            yield return mod.PluginsPath;

            //Check the mod's root directory only if we did not find results in the current plugin directory
            if (!AssemblyUtils.LastFoundAssembly.HasPath(mod.PluginsPath))
            {
                yield return mod.ModDir;
            }
        }
    }

    public static void Initialize()
    {
    }

    public static void Patch(AssemblyDefinition _)
    {
    }

    public static void Finish()
    {
        if (!loadedAssembly) return;

        try
        {
            ModLibAccess.TryLoadModLib(CompatibilityPaths);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize ModLib entrypoint: {ex}");
        }

        CompatibilityPaths.Clear();

        BepInEx.Logging.Logger.Sources.Remove(Logger);
    }

    private static class ModLibAccess
    {
        public static void TryLoadModLib(List<string> compatibilityPaths) =>
            Entrypoint.Initialize(compatibilityPaths);
    }
}