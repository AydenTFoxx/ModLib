# ModLib | Rain World Modding Made Easier

ModLib is a collection of tools and features made with the goal of streamlining Rain Worlds mod development.
Features are designed to be easy to use and fully compatible with other popular mods (e.g. Improved Input Config: Extended), requiring zero setup from the developer.

A work-in-progress, automatically-generated documentation is available in [this repository's site](https://aydentfoxx.github.io/ModLib/api/ModLib.html).

## Features

- [Weak collections](./ModLib/Collections/) for safely storing reference types without preventing their garbage collection.
- [Input handling](./ModLib/Input/) with various utilities for registering and retrieving custom keybinds; Provides full support for the *Improved Input Config: Extended* mod if present, but also works decently well without it.
- [Logging classes](./ModLib/Logging/) offering full integration with [LogUtils](https://github.com/TheVileOne/ExpeditionRegionSupport/tree/master/LogUtils), a powerful logging library tailored for Rain World. If the former is not present, ModLib also has its own (admittedly simpler) implementation for [logging to a dedicated, mod-specific file](./ModLib/Logging/FallbackLogger.cs).
- [Meadow compatibility](./ModLib/Meadow/) helpers for ensuring compatibility with the [Rain Meadow](https://github.com/henpemaz/Rain-Meadow) mod, including various static helpers and methods for retrieving online information, factory methods for sending time-limited RPCs, and a [`SerializableDictionary<TKey, TValue>`](./ModLib/Meadow/SerializableDictionary.cs) with the ability to map all of its values to equivalent local counterparts (e.g. `OnlineCreature` -> `Creature`, `RoomSession` -> `Room`).
- [Settings utilities](./ModLib/Options/) like [`OptionBuilder`](./ModLib/Options/OptionBuilder.cs) for building simple mod setting pages, or [`OptionUtils`](./ModLib/Options/OptionUtils.cs) for retrieving and overridin a mod's settings dynamically without affecting the user's configured values.
- A simple yet effective [mod persistent data](./ModLib/Storage/ModPersistentSaveData.cs) class for storing data which is automatically saved and retrieved by ModLib.
- A comprehensive [compatibility helper](./ModLib/CompatibilityManager.cs) capable of detecting any given mod's presence before the game even begins loading, with the ability to configure detected mods via a dedicated configuration file, and more.

## Getting Started

### Installation

To use ModLib in your projects, download `ModLib.dll` and (optionally) `ModLib.pdb`, then include it as a referenced assembly in your project:

```csproj
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

1. Have your "Plugin" class (the one inheriting `BaseUnityPlugin`) extend `ModPlugin` instead. This will ensure your mod is registered as soon as it is loaded by BepInEx. Additionally, `ModPlugin` itself offers a variety of virtual methods for registering your mod's hooks and content, which you may override as needed for your mod.

> ```cs
> using BepInEx;
> using ModLib;
>
> [BepInPlugin("example.mod", "My Cool Mod", "1.0.0.0")]
> public class Plugin : ModPlugin
> {
>     // your code goes here...
> }
> ```

2. Register your mod directly by calling `Registry.RegisterMod(BaseUnityPlugin, Type?, ModLogger?)`, including a reference to your entry point class, and optionally a reference to your REMIX option interface class and/or a logger instance for usage by your mod. If the last is not provided, ModLib will create a new one and assign it to your mod during registration.

> ```cs
> ...
> public void OnEnable()
> {
>     Registry.RegisterMod(this, null);
> }
> ...
> ```

> [!WARNING]
> Several features require a registered mod in order to be accessed; Attempting to use them from an unregistered mod will throw a `ModNotFoundException` at runtime!
>
> It is also worth noting that mod registration is performed *per assembly*, meaning two `BaseUnityPlugin` instances in the same assembly share the same registry entry. Attempting to register both will throw an `InvalidOperationException`.

For usage of specific features, see their related classes.

If you need help with anything ModLib-related, ping @yannahtfoxx in the Rain World server (or send a DM if you prefer, but do mention you need help with ModLib in your message!)

## License

As a project created primarily through the compilation of knowledge from many others far more experienced than me, ModLib is entirely dedicated to the Rain World modding community.
You are free to use ModLib's code, assets, or any other features at no charge or further requirements; To the full extent of applicable law, you are free to do with this project as you wish.

Attribution is appreciated, but not required. My primary goal is to make modding as accessible as possible, so more cool things can be done by new modders :)

## Contributing

Issues and Pull Requests are very welcome and appreciated! This is a passion project funded entirely by my free time and energy, so any help is highly appreciated.

Other than technical help, word of mouth is also appreciated; If you like ModLib, let the world know about it too! The more people I can help with this, the better :P
