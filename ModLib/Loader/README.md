# ModLib | Loading Pipeline

> *This document was last updated on `2026-02-28`, and is accurate for ModLib version `0.4.1.0`.*

ModLib's initialization process is fairly complex, composed of multiple classes interacting with a central `Entrypoint`, which orchestrates the entire loading and unloading processes.

Understanding ModLib's initialization is entirely optional, but allows for a greater understanding of the library's capabilities and limitations, as well as allowing the possibility of adding new features to it through [extension assemblies](./README-ExtensionAssemblies.md).

> [!WARNING]
>
> This documentation is highly technical, and details in depth internal systems of ModLib. These are NOT intended as, nor considered part of its public API.

## Table of Contents

- [ModLib Loading Pipeline](#modlib--loading-pipeline)
  - [Initialization Phases](#initialization-phases)
    - [Phase 0](#phase-0)
    - [Phase 1](#phase-1)
    - [Phase 2](#phase-2)
    - [Phase 3](#phase-3)
  - [Deactivation Process](#deactivation-process)

## Initialization Phases

For simplicity and organization's sake, ModLib's initialization is generally defined through multiple "phases":

### Phase 0

If ModLib has run at least once in a given device, a preloading patcher (`ModLib.Loader.dll`) should have been deployed to `Rain World/BepInEx/patchers/`. If the patcher could not be added or was manually removed, this phase is skipped.

If present, the patcher performs the following operations before any mods are loaded:

- Query for `CompatibilityMods.txt` files in the root folder of each enabled mod. If found, the full path to the file is stored for later reference.
- Query for ModLib assemblies in all enabled mods' plugins folders. If one or more are found, the latest assembly is loaded by the patcher.
- Query for ModLib.Objects assemblies in all enabled mods' plugins folders. If one or more are found, the latest assembly is loaded by the patcher.

Once all preloading patchers finish their initialization, the patcher performs the following operations:

- Attempt to initialize ModLib by calling `Loader.Entrypoint.Initialize(IList<string>, bool, string)` from ModLib's assembly, passing the list of paths to `CompatibilityMods.txt` files (if any was found) as the first argument, `false` for its second argument, and a compiler-provided path to the current file as the third argument.

If no errors occur during any of the above steps, Phase 0 finishes successfully.

### Phase 1

Initialized by calling the `Initialize(IList<string>, bool string)` method from the `Loader.Entrypoint` class, this phase is responsible for initializing static classes, loading [extension assemblies](./README-ExtensionAssemblies.md) and preparing ModLib's internal `Core` systems for initialization.

There are usually two ways by which this method is usually called:

- In the `Finalize()` method of the preloader patcher (`Loader.Patcher` from the `ModLib.Loader` assembly);
- At the static constructor of the following classes:
  - `Extras`
  - `Registry`
  - `Options.OptionUtils`

If the *Dev Console* mod is enabled, the `Loader.Entrypoint` class may also be initialized manually with the following command: `modlib entrypoint initialize`

When initialized, the `Loader.Entrypoint` class performs the following operations:

- Query for and instantiate [extension entrypoints](./README-ExtensionAssemblies.md) loaded in the current domain. The target type must implement the `Loader.IExtensionEntrypoint` interface and have a parameterless constructor. If an entrypoint throws an exception during its initialization, it is skipped.
- Create a new instance of the `CompatibilityManager.ConfigLoader` class, then initialize it using the provided paths to compatibility override files. Configured mod IDs are queried for, and if found, marked as "present" in the manager. By default, the following IDs are included:
  - `henpemaz_rainmeadow` (Rain Meadow)
  - `improved-input-config` (Improved Input Config: Extended)
  - `ddemile.fake_achievements` (Fake Achievements)
  - `slime-cubed.devconsole` (Dev Console)
- Initialize the `Extras` static class, setting its properties' values by querying `CompatibilityManager`'s specialized methods.
- If the caller of the initialization method was the preloader patcher, add a hook to BepInEx's chainloader initialization to initialize Phase 2. Otherwise, start Phase 2 immediately.

### Phase 2

Initialized by calling the `CoreInitialize()` method from the `Loader.Entrypoint` class, this phase is responsible for initializing entrypoint types, ModLib's internal `Core` systems, and checking and/or deploying the preloading patcher assembly to its appropriate folder.

This phase can only be initialized by either of two methods:

- Invoked by an IL hook within the `BepInEx.MultiFolderLoader.ChainloaderHandler.PostFindPluginTypes` method (early initialization);
- Invoked directly by the `ModLib.Loader.Entrypoint.Initialize(IList<string>, bool, string)` method (late initialization).

When invoked by either sources, the `CoreInitialize()` method performs the following operations:

- For each loaded extension entrypoint, invoke its `OnEnable()` method. If it throws an exception, the entrypoint is skipped.
- Initialize the internal static `Core` class by invoking its `Initialize()` method, which performs the following operations:
  - Attempt to read ModLib's stored JSON data, if any.
  - Set internal options `modlib.debug` and `modlib.preview` to their default values:
    - If Dev Tools was detected last time ModLib was enabled, `modlib.debug` is `true`; Otherwise, it is always `false`
    - `modlib.preview` is always `false`
  - Store the value of `modlib.debug` to `Extras.DebugMode`
  - If *LogUtils* was found to be enabled during Phase 1, switch ModLib's logging implementation to use a `LogUtils.Logger` instance.
  - If Rain Meadow is present, add ModLib's Meadow compatibility hooks to the game. Otherwise, add their non-Meadow equivalents from the `Core` class, if any.
  - Add the rest of the required hooks for the functionality of certain ModLib systems.
  - Set the `Loader.Entrypoint.Disable()` method to be invoked when the game is closing.
  - Register ModLib's main assembly to its `Registry` class, using the data provided by the `Core` class.
- If the above did not throw any exceptions, mark the `Loader.Entrypoint` class as fully initialized.
- Initialize the internal static `Core+PatchLoader` nested class by invoking its `Initialize()` method, which performs the following operations:
  - Query for ModLib's preloading patcher assembly in BepInEx's `backup` and `patchers` folders. If both are found, delete the file at the `backup` folder and exit the method. In regular scenarios, this exclusively occurs right after updating the preloading patcher assembly.
  - Otherwise, query for the preloading patcher assembly in the `patchers` folder. Then:
    - If it is found and its version is equals to or greater than the embedded assembly's, exit the method.
    - If it is found and its version is lower than the embedded assembly's, remove the assembly's name from BepInEx's whitelist, which will move it from the `patchers` folder to the `backup` folder the next time the game initializes.
    - Otherwise (if the file is not found), deploy the new assembly to the `patchers` folder, then add its name to BepInEx's whitelist file (which will prevent it from being moved again)

### Phase 3

Initialized as a hook to `RainWorld.PostModsInit`, this phase is responsible for correcting cached values and initializing ModLib's commands for Dev Console.

When initialized, this phase performs the following operations:

- Update ModLib's data with its current version and whether Dev Tools is enabled (via `ModManager.DevTools`).
- Refresh the option `modlib.debug` with the new value, and set it again to `Extras.DebugMode`.
- If a supported mod was not detected during previous initialization phases, query for its presence again. The following mods are tested for:
  - *Improved Input Config: Extended*
  - *Rain Meadow*
  - *Fake Achievements*
- If the *Dev Console* mod is enabled, register the `modlib` debugging command.
- For each logging filter created with unspecified settings, refresh its configuration with the updated `modlib.debug` value.

Once all operations are complete, ModLib is considered ready for usage, and can be freely accessed by consumer mods. If supported mods were previously detected, their respective compatibility systems should also be available for usage by now.

## Deactivation Process

Unlike its initialization process, ModLib's deactivation is far more straightforward. Invoked when the game is about to close, the method `Disable()` from the `Loader.Entrypoint` class handles deactivation of all ModLib systems.

When invoked, the `Disable()` method performs the following operations:

- For each loaded extension entrypoint, invoke its `OnDisable()` method. If it throws an exception, the entrypoint is skipped.
- Invoke the `Disable()` method from the internal static `Core` class, which performs the following operations:
  - If the *Rain Reloader*[^rainreloader] mod is present, remove all compatibility overrides set with `CompatibilityManager`, as well as all hooks from ModLib
  - Write ModLib's data structure to its JSON file
  - If any mod had a ModData instance configured for auto-saving, store its data in its respective file.
- If the *Rain Reloader*[^rainreloader] mod is present, remove the IL hook used to initialize ModLib, if any.
- Mark the `Loader.Entrypoint` class as non-initialized again.
- Clear all loaded entrypoint extensions.

[^rainreloader]: ModLib is currently incompatible with the *Rain Reloader* mod, so in practice these lines are never run.
