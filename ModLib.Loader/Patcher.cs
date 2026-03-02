using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using BepInEx.MultiFolderLoader;
using Mono.Cecil;
using AssemblyCandidate = (System.Version Version, string Path);

// Credits to LogUtils by Fluffball (@TheVileOne) for original LogUtils.VersionLoader code

namespace ModLib.Loader;

public static class Patcher
{
    internal static ManualLogSource LogSource = Logger.CreateLogSource("ModLib.Loader");

    private static List<string> CompatibilityPaths = [];
    private static bool _loadedAssembly;

    public static IEnumerable<string> TargetDLLs => GetDLLs();

    public static IEnumerable<string> GetDLLs()
    {
        yield return "BepInEx.MultiFolderLoader.dll";

        LogSource.LogMessage($"Looking for ModLib assemblies...");

        AssemblyCandidate target = AssemblyUtils.FindLatestAssembly(GetSearchPaths(true), "ModLib.dll");

        if (!string.IsNullOrEmpty(target.Path))
        {
            LogSource.LogMessage($"Loading latest ModLib DLL: {AssemblyUtils.FormatCandidate(target, true)}");

            Assembly.LoadFrom(target.Path);

            _loadedAssembly = true;
        }
        else
        {
            LogSource.LogInfo("No ModLib assembly found.");
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
                    LogSource.LogInfo($"Found CM config file: {AssemblyUtils.GetModName(compatFilePath, true)}");

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
        if (!_loadedAssembly) return;

        try
        {
            ModLibAccess.TryLoadModLib(CompatibilityPaths);
        }
        catch (Exception ex)
        {
            LogSource.LogError($"Failed to initialize ModLib entrypoint: {ex} (Init Phase: #0)");
        }

        CompatibilityPaths.Clear();
        CompatibilityPaths = null!;

        Logger.Sources.Remove(LogSource);
        LogSource = null!;
    }

    private static class ModLibAccess
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TryLoadModLib(List<string> compatibilityPaths) =>
            Entrypoint.Initialize(compatibilityPaths, false);
    }
}