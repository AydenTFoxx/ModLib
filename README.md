# Yannah's Template Mod

A not-so-basic skeleton of a Rain World mod, with extensible utilities for mod compatibility, error handling, and more.

For a version of this using SlimeCubed's [SlugTemplate](https://github.com/SlimeCubed/SlugTemplate), see the [template-slugbase](https://github.com/AydenTFoxx/Martyr/tree/template-slugbase/README.md) branch.

## Features

- Custom [Logger](src/Logger.cs) class for logging messages outside of Rain World's log files
- Wrapper methods for handling error-prone code without crashes
- Automatic syncing of "server-sided" REMIX options
- [Weak collection classes](src/Utils/Generics) for storing weak references to game objects
- [Meadow helpers](src/Utils/Meadow) for syncing settings and custom events with other players
- Helper classes for handling REMIX options:
  - [A REMIX menu page builder](src/Utils/Options/OptionBuilder.cs) for quickly creating simple menus
  - A [handler of current REMIX options](src/Utils/Options/OptionBuilder.cs), which stores the actual settings used by the mod, with an [utility class for retrieving its values](src/Utils/Options/OptionUtils.cs) by reference
- A [compatibility manager](src/Utils/CompatibilityManager.cs) for handling "compatibility layers" with other mods
  - Includes a method for querying the user's `enabledMods.txt` file directly, useful for detecting a mod's presence during `OnEnable` or earlier
- An [input handler](src/Utils/InputHandler.cs) class for registering keybinds and retrieving keypress events, with [automatic support](src/Utils/InputHandler.cs) for *Improved Input Config: Extended*
- And more...

## Usage

Use this template on GitHub or [download the code](https://github.com/AydenTFoxx/Martyr/archive/refs/heads/master.zip), whichever is easiest.

Links:

- [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/Mod_Directories) for `modinfo.json` documentation.
