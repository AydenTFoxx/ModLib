# ModLib | Rain World Modding Made Easier

ModLib is a collection of tools and features made with the goal of streamlining Rain Worlds mod development.
Features are designed to be easy to use and fully compatible with other modding tools (e.g. Improved Input Config: Extended) with minimal work from the developer.

A proper documentation/wiki is still in the works; Nonetheless, the entirety of ModLib's API is documented as doc comments.
To provide IntelliSense for ModLib classes and methods, include `ModLib.xml` in your references alongside the main DLL file.

## Features

At its core, ModLib is no revolutionary solution; Most of its features have been done before (and inspired) by other mods, often in a manual, per use-case approach.
Instead, what ModLib primarily provides is a common API, a collection of time-tested solutions to many common issues most modders will end up facing, sooner or later.

> (TODO: Make greater use of this shared API, simplifying interoperability between mods)

Nonetheless, some features (notably `CompatibilityManager`'s ability to detect enabled mods before the game initializes) are rarely seen elsewhere, and at times exceedingly hard to code to be worth the effort. In such cases, ModLib offers a convenient, ready-made solution for developers who may wish to use such tools in their mods, but cannot/do not want to implement those themselves.

### Logging

ModLib has built-in integration with LogUtils, offering a quick and convenient API for creating logger instances for your mod.
This logger instance automatically targets your mod's BepInEx logger, and logs to a separate file at `StreamingAssets/Logs/YourModName.log`.

When LogUtils is not available, your mod's BepInEx logger is used as a fallback, ensuring logs are never lost regardless of the available tools at runtime.

> (TODO: Revive old `Logger.cs` and adapt it to be a fallback logger instead of solely relying on BepInEx's)

### REMIX

ModLib has a variety of tools for creating and managing options from your mod's REMIX option interface.
Option management, online sync, and even temporary overrides are all handled by ModLib, while requiring as little user input as possible.

> (TODO: Return overrides values to their original values when a temporary option is removed)  
> (TODO? Add a duration for temporary options? Feels more like a user responsibility... Too specific to be made into a general-purpose tool)

- (Optional) Create REMIX interfaces in a familiar fashion with `OptionBuilder`. Chain methods to create simple yet effective interfaces, then convert them to an `OpTab` instance with `Build()`
- At your [mod registering](#usage), include a reference to the class where you implement your REMIX interface.
- To access any given value at runtime, call one of OptionUtils' static methods: `IsOptionEnabled`, `IsOptionValue<T>`, or `GetOptionValue<T>`. You can either pass a reference to the `Configurable<T>` field, or use its ID value directly.
  - Additionally, cosmetic or client-sided options can instead be checked with the variants `IsClientOptionEnabled`, `IsClientOptionValue<T>`, and `GetClientOptionValue<T>`. Note these don't allow you to specify an ID directly, however.

If the Rain Meadow mod is present, option values are automatically synced between the host and clients, ensuring your mod works as expected in an online context. When leaving a lobby, the client's options return to their original values.

> (TODO: Force a refresh of the local `OptionUtils.SharedOptions` instance when no longer online? Seems only useful in a few edge cases, but relevant nonetheless)

Temporary options can also be created and removed with `OptionUtils.SharedOptions.AddTemporaryOption` and `RemoveTemporaryOption`, respectively.
Temporary options do not alter the saved REMIX option values, instead overriding their representation at runtime. After removing them and refreshing the option holder (with `OptionUtils.SharedOptions.RefreshOptions()`), they return to their original values.

### Input Handling

ModLib allows the creation of *immutable* keybinds, using the `Keybind` class. When the Improved Input Config: Extended mod is present, an equivalent `PlayerKeybind` object is automatically registered, allowing players to edit it via that mod's interface instead.

`Keybind` objects are convertible to and from `PlayerKeybind` instances, so any API accepting a `PlayerKeybind` is also compatible with ModLib's `Keybind` class.

> (TODO: Make both conversions implicit, so the two classes are as interchangeable as allowed by C#)

While `Keybind` objects can be queries for input directly, their true power comes from using them alongside the `InputHandler` class (or one of its extension methods): When IIC:E is present, calls with `Keybind` objects are automatically converted to `PlayerKeybind` ones, where the relevant APIs are used instead.

> (TODO? Make `Keybind` methods also inherit this functionality, juust to be sure. If there are two ways to do something, someone will use the less effective one at some point.)

### Rain Meadow Compatibility

Several utilities are available for implementing Rain Meadow compatibility, including the extension methods from `ModRPCManager` (for sending and processing time-limited `RPCEvent`s), and the suite of methods and properties from `MeadowUtils` (for general interaction with online sessions).

ModLib itself uses those same provided APIs to implement automatic syncing of REMIX options.

### And others...

Weak reference collections, compatibility management for implementing interoperability with other mods, automatic error handling of methods (and, in particular, IL hooks); An ever-growing collection of tools for just about any project you could imagine.

## Getting Started

### Installation

To use ModLib in your projects, download the file `ModLib.dll` at `mod/newest/plugins`, then include it as a referenced assembly in your project:

```csproj
<ItemGroup>
    <Reference Include="path/to/ModLib.dll">
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

Optionally, you can also download `ModLib.xml` for IntelliSense support to all public classes and methods, if your editor of choice has such feature.
When building your mod, make sure to also include a copy of `ModLib.dll` on the same folder as your mod's assembly. Including `LogUtils.dll` is strongly recommended, but not required if you do not need a separate file for your mod's logs.

### Usage

To use ModLib's features, you must first register your mod. This can be done in one of two ways:

1. Have your entry point class (the one with a `BepInPlugin` attribute) extend `ModLib.ModPlugin`. This will ensure your mod is registered as soon as it is loaded by BepInEx. Additionally, `ModPlugin` itself offers a general "interface" of virtual methods, which you can override to include your mod's content as well.
2. Alternatively, you can register your mod directly by calling `Registry.RegisterMod`, including a reference to your entry point class, and optionally a reference to your REMIX option interface class and/or a logger instance for usage by your mod. If the last is not provided, ModLib creates a new one and assigns it to your mod during registering.

Once registered, you can access your mod's data with `Registry.MyMod`. If for any reason you want to remove your mod from ModLib's registry, calling `Registry.UnregisterMod()` will discard all saved data tied to your mod. However, you won't be able to use methods which require registry until you register your mod again.

> ![WARN]
> Registering a mod ties the calling *assembly* to the generated data. I.e. it is currently not supported to have more than one `BepInPlugin` registered per mod assembly.

## Licensing

As a project created primarily through the compilation of knowledge from many others far more experienced than me, ModLib is entirely dedicated to the Rain World modding community.
You are free to use ModLib's code, assets, or any other features at no charge or further requirements; To the extent of what is allowed by law, you are free to do with this project as you wish.

Attribution is appreciated, but not required. My primary goal is to make modding as accessible as possible, so more cool things can be done by more new modders :)

## Contributing

If you want to contribute to ModLib in some way, that's greatly appreciated! Pull requests are merged as soon as I can ensure no bugs or regressions are introduced with the latest changes, and issues help a ton with figuring out what pain points of the current API need to be worked on at a greater priority.

If you don't feel like coding (or already got a large-scale project where ModLib would be of little use), but know someone who might find use in it, then consider sharing ModLib with them! Word of mouth is a powerful asset, and an equally appreciated contribution! ^^
