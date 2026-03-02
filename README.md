# ModLib | Rain World Modding Made Easier

ModLib is a collection of tools and features made with the goal of streamlining Rain World mod development.
Features are designed to be easy to use, powerful when needed, and fully compatible with popular mods (e.g. *Improved Input Config: Extended*, *Rain Meadow*, etc.) with as little setup as possible from the developer's end.

A work-in-progress, automatically-generated documentation is available in [this repository's site](https://aydentfoxx.github.io/ModLib/api/ModLib.html).

## Features

- [Weak collections](./ModLib/Collections/) for safely storing reference types without preventing their garbage collection.
- [Input handling](./ModLib/Input/) with various utilities for registering and retrieving custom keybinds; Provides full support for the *Improved Input Config: Extended* mod if present, but also works decently well without it.
- [Logging classes](./ModLib/Logging/) offering full integration with [LogUtils](https://github.com/TheVileOne/ExpeditionRegionSupport/tree/master/LogUtils), a powerful logging library tailored for Rain World. If the former is not present, ModLib also has its own (admittedly simpler) implementation for [logging to a dedicated, mod-specific file](./ModLib/Logging/FallbackLogger.cs).
- [Meadow compatibility](./ModLib/Meadow/) helpers for ensuring compatibility with the [Rain Meadow](https://github.com/henpemaz/Rain-Meadow) mod, including various static helpers and methods for retrieving online information, factory methods for sending time-limited RPCs, and a [`SerializableDictionary<TOnlineKey, TOnlineValue>`](./ModLib/Meadow/SerializableDictionary.cs) with the ability to map all of its values to equivalent local counterparts (e.g. `OnlineCreature` -> `Creature`, `RoomSession` -> `Room`).
- [Settings utilities](./ModLib/Options/) like [`OptionBuilder`](./ModLib/Options/OptionBuilder.cs) for building simple mod setting pages, or [`OptionUtils`](./ModLib/Options/OptionUtils.cs) for retrieving and overriding a mod's settings dynamically without affecting the user's configured values.
- A simple yet effective [mod data storage](./ModLib/Storage/ModData.cs) class for storing data which is automatically saved and retrieved by ModLib.
- A comprehensive [compatibility helper](./ModLib/CompatibilityManager.cs) capable of detecting any given mod's presence before the game even begins loading, configurable via developer-provided text files, and more.

## Getting Started

### Installation

To use ModLib in your projects, download `ModLib.dll` and (optionally) `ModLib.pdb`, then include it as a referenced assembly in your project:

```xml
<ItemGroup>
    <Reference Include="path/to/ModLib.dll">
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

You may also download `ModLib.xml` for IntelliSense support to all public classes and methods, if your editor of choice has such feature.

When building your mod, make sure to include a copy of `ModLib.dll` (and if present, `ModLib.pdb`) on the same folder as `YourModName.dll`.

### Usage

Before using most of ModLib's features, you must first register your mod. This can be done in one of two ways:

- Have your "Plugin" class (the one inheriting `BaseUnityPlugin`) extend `ModPlugin` instead. This will ensure your mod is registered as soon as it is loaded by BepInEx. Additionally, `ModPlugin` itself offers a variety of virtual methods for registering your mod's hooks and content, which you may override as needed for your mod.

```cs
using BepInEx;
using ModLib;
[BepInPlugin("example.mod", "My Cool Mod", "1.0.0.0")]
public class Plugin : ModPlugin
{
    // your code goes here...
}
```

- Alternatively, register your mod directly by calling `Registry.RegisterMod(BaseUnityPlugin, Type?, ModLogger?)`, including a reference to your entry point class, and optionally a reference to your REMIX option interface class and/or a logger instance for usage by your mod. If the last is not provided, ModLib will create a new one and assign it to your mod during registration.

```cs
...
public void OnEnable()
{
    Registry.RegisterMod(this, null);
}
...
```

> [!WARNING]
>
> Several features require a registered mod in order to be accessed; Attempting to use them from an unregistered mod will throw a `ModNotFoundException` at runtime!
>
> It is also worth noting that mod registration is performed *per assembly*, meaning ALL `BaseUnityPlugin` instances in the same assembly share the same registry entry. Attempting to register more than one will throw an `InvalidOperationException` at runtime.

For usage of specific modules, refer to their respective documentation:

- [Main](./ModLib/README.md)
- [Collections](./ModLib/Collections/README.md)
- [Input](./ModLib/Input/README.md)
- [Loading Pipeline](./ModLib/Loader/README.md)
- [Logging](./ModLib/Logging/README.md)
- [Meadow](./ModLib/Meadow/README.md)
- [Options](./ModLib/Options/README.md)
- [Storage](./ModLib/Storage/README.md)

If you need more help with anything ModLib, ping `@yannahtfoxx` on the Rain World server (or send a DM if you prefer, but do mention ModLib in your message!)

## License

As a project created primarily through the compilation of knowledge from many others far more experienced than me, ModLib is entirely dedicated to the Rain World modding community.

**ModLib is licensed under the CC0 license**; In other words, you are free to use its code and features as you wish, at no charge or additional requirements. For all intents and purposes, the code is dedicated to the public domain.

Attribution is highly appreciated, however! The more people hear about ModLib, the more it can grow and the cooler it can become! :D

## Contributing

Pull Requests are very welcome and appreciated! This is a passion project funded entirely by my free time and energy, so any help goes a long way in keeping this project alive.
