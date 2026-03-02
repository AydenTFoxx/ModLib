# ModLib | Main Module

> *This document was last updated on `2026-03-01`, and is accurate for ModLib version `0.4.1.0`.*

Contains various utilities and core systems used for interoperability with ModLib and other mods.

## Table of Contents

- [ModLib](#modlib--main-module)
  - [CompatibilityManager](#compatibilitymanager)
    - [Compatibility Overrides](#compatibility-overrides)
  - [Extras](#extras)
  - [Registry](#registry)
  - [ModPlugin](#modplugin)

## CompatibilityManager

ModLib's dedicated helper for determining the presence of other mods at runtime. Besides having full control over which mods are considered "enabled" or not, its strongest feature is the ability to detect a mod's presence as early as during preloading, aka before mods are even loaded into memory.

```cs
using BepInEx;
using ModLib;

[BepInPlugin("example.wawa-mod", "Tequi-wa!", "1.2.3.4")]
public class Plugin : BaseUnityPlugin
{
    // The constructor is the earliest point
    // where a standard mod can execute code at.
    public Plugin()
    {
        // At this point, Rain World's core systems haven't initialized yet;
        // ModManager will not yield any results, even if the given mod is, in fact, enabled.
        if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
        {
            Logger.LogInfo("Rain Meadow was found with ModManager!");
        }
        else if (CompatibilityManager.IsRainMeadowEnabled())
        {
            // This will always execute as long as the Rain Meadow mod is enabled:
            Logger.LogMessage("Rain Meadow was found with CompatibilityManager!");
        }
    }
}
```

When running the above code, assuming the Rain Meadow mod is present, the following will be logged to BepInEx's `LogOutput.log`:

```log
...
[Message: Tequi-wa!] Rain Meadow was found with CompatibilityManager!
...
```

Determining the presence of any given mod can be done with the method `IsModEnabled(string, bool)`:

```cs
// Detects if the mod is enabled; If a cached value is found, it is retrieved. Otherwise, a new cache is made.
Logger.LogDebug(CompatibilityManager.IsModEnabled("example.my-cool-mod")); // -> true

// Sets the cached value to false. Any further checks will determine the mod is NOT enabled (even if it's actually present)
CompatibilityManager.SetModCompatibility("example.my-cool-mod", false);

// Retrieves the cached value (manually set to `false` above); Assumes the mod is not present
Logger.LogDebug(CompatibilityManager.IsModEnabled("example.my-cool-mod")); // -> false

// Overrides the previous cache and queries for the mod directly
Logger.LogDebug(CompatibilityManager.IsModEnabled("example.my-cool-mod", forceQuery: true)); // -> true
```

Alternatively, certain mods have their own dedicated methods for determining their presence. These are as follow:

- *Fake Achievements*: `IsFakeAchievementsEnabled()`;
- *Improved Input Config: Extended*: `IsIICEnabled()`;
- *Rain Meadow*: `IsRainMeadowEnabled()`.

The above mods also have their own dedicated read-only properties in the `Extras` class, which are guaranteed to return accurate results even if the values at the `CompatibilityManager` class are overriden:

```cs
// These properties will always return `true` as long as
// their respective mods are detected during initialization

Logger.LogDebug(Extras.IsFakeAchievementsEnabled); // Fake Achievements
Logger.LogDebug(Extras.IsIICEnabled); // Improved Input Config: Extended
Logger.LogDebug(Extras.IsMeadowEnabled); // Rain Meadow
```

### Compatibility Overrides

While powerful, `CompatibilityManager`'s enhanced mod detection is by default limited to the aforementioned mods; In order to add other mod IDs to be detected at initialization, a special file must be created at a mod's root folder: `CompatibilityMods.txt`

```txt
// The following mod IDs will be checked for during initialization.
// Each line is a separate mod, with multiple IDs being possible variants for identifying the same mod
// Whitespace, empty lines and any text preceded by // are ignored when retrieving IDs

// By default, the first ID is assumed to be the mod's GUID, and is used
// for determining equality with other lists, as well as displaying it to the console

henpemaz_rainmeadow, 3388224007, rainmeadow, rain meadow // Rain Meadow
slime-cubed.devconsole, 2920528044, devconsole, dev console // Dev Console

// Advanced queries can be performed by prefixing the ID with a @
// These queries are guaranteed to find the specified mod, but may have a greater performance impact
// depending on the amount of mods the user has installed

// If you need to absolutely guarantee the presence of a given mod,
// consider making it a dependency of your mod instead

// When specifying advanced queries, only include the GUID of the target mod
@improved-input-config
@ddemile.fake-achievements

@example.my-cool-mod
```

When initializing ModLib, these files are retrieved and read for determining the mods to be queried. Any mod specified will be checked with the same systems used for determining the presence of Rain Meadow and other supported mods.

## Extras

Used as a shortcut for interacting with various ModLib systems and supported third-party mods.

The following static properties can be used to query the current state of the game:

```cs
// Supported mods have dedicated properties for determining their presence
Logger.LogDebug(Extras.IsFakeAchievementsEnabled); // Fake Achievements
Logger.LogDebug(Extras.IsIICEnabled); // Improved Input Config: Extended
Logger.LogDebug(Extras.IsMeadowEnabled); // Rain Meadow
Logger.LogDebug(Extras.DebugMode); // Dev Tools

Logger.LogDebug(Extras.LogUtilsAvailable); // LogUtils (modding library)
Logger.LogDebug(Extras.ModLibReady); // ModLib (modding library)

// The game's state can be determined with various get-only properties:
Logger.LogDebug(Extras.GameSession); // returns the current GameSession the player is in, or `null` if on the main menu
Logger.LogDebug(Extras.InGameSession); // true if `Extras.GameSession` is not null, false otherwise.

Logger.LogDebug(Extras.IsOnlineSession); // true if client is in a Rain Meadow lobby, false otherwise
Logger.LogDebug(Extras.IsMultiplayer); // true if Jolly Co-op is enabled or client is in a Rain Meadow lobby, false otherwise
Logger.LogDebug(Extras.IsHostPlayer); // true if singleplayer/Jolly Co-op or client is the host of a Rain Meadow lobby, false otherwise
```

The following methods are also available for interaction with supported mods:

```cs
// If the Fake Achievements mod is present, custom achievements
// can be granted and/or revoked using the Extras class

// If Fake Achievements is not enabled, these methods do nothing.
Extras.GrantAchievement("mymod.cool_achievement_id");
Extras.RevokeAchievement("mymod.cool_achievement_id");

// If the Rain Meadow mod is present, determining if a given object
// is owned by the client can be performed using the Extras class

// If Rain Meadow is not enabled, these methods do nothing.
PhysicalObject obj = GetPhysicalObject();

if (Extras.IsLocalObject(obj)) // true if not in a Rain Meadow lobby, or obj is owned by this client
{
    // do something with local object...
}
```

Finally, an exception handling method is also available by default:

```cs
static void DangerousThrowingMethod()
{
    // do code...

    if (SomethingWentWrong())
        throw new Exception("An error ocurred!");
}

// safely execute the given delegate in a try/catch block
Extras.WrapAction(DangerousThrowingMethod);

Logger.LogDebug("This code will run even if the above method throws an exception!");
```

## Registry

Certain modules from ModLib require the caller mod to be registered, throwing a `ModNotFoundException` otherwise. In order to register a mod, call the `Registry.RegisterMod(BaseUnityPlugin, Type?, ModLogger?)` method, ideally before all other code from the given mod:

```cs
using BepInEx;
using ModLib;

[BepInPlugin("example.wawa-mod", "Tequi-wa!", "1.2.3.4")]
public class Plugin : BaseUnityPlugin
{
    public Plugin()
    {
        Registry.RegisterMod(this, null, Logger);
    }
}
```

The following arguments may be provided during registration:

- `plugin`: **Required**. The `BaseUnityPlugin` instance or `BepInPlugin` data of the mod being registered, used in various modules for identification and auto-generated names.
- `optionHolder`: *Optional*. A `Type` referring to an [option holder class](./Options/README.md#option-holders). If specified, the mod's REMIX options are automatically added to the `SharedOptions` collection, and may be retrieved and modified as desired from its API.
- `logger`: A `ModLogger` or `ManualLogSource` instance used for logging as the registered mod, used by some modules for logging exceptions. If a `ManualLogSource` instance is provided, it is converted to a `ModLogger` before registration.

Once registered, the mod's data can be retrieved with `Registry.MyMod`:

```cs
Registry.RegisterMod(this);

Logger.LogInfo(Registry.MyMod); // -> [(example.wawa-mod|Tequi-wa!|1.2.3.4); No OptionHolder; ModLib.Logging.FallbackLogger]
```

To unregister a mod, invoke the `Registry.UnregisterMod()` method:

```cs
Register.UnregisterMod();

Logger.LogInfo(Registry.MyMod); // throws ModNotFoundException
```

## ModPlugin

Provides a standard skeleton for a `BaseUnityPlugin` class which is automatically registered to ModLib. Contains methods for adding and removing hooks from the game, loading mod content, and registering the mod's REMIX menu to the game.

Below is an example containing all overridable methods of `ModPlugin`:

```cs
using BepInEx;
using ModLib;

[BepInPlugin("example.wawa-mod", "Tequi-wa!", "1.2.3.4")]
public class Plugin : ModPlugin
{
    public Plugin() : base(new OptionHolder()) // OptionHolder must extend OptionInterface
    {
        Logger.LogDebug(Registry.MyMod); // -> [(example.wawa-mod|Tequi-wa!|1.2.3.4); OptionHolder; ModLib.Logging.FallbackLogger]
    }

    // override this method for behavior which must happen once during your mod's initialization
    public override void OnEnable()
    {
        if (IsModEnabled) return; // set to true when base.OnEnable() runs for the first time

        base.OnEnable();

        Logger.LogInfo($"{Info.Metadata.Name} is enabled!");
    }

    // override this for any behavior which must run before your mod is disabled or the game closes
    public override void OnDisable()
    {
        if (!IsModEnabled) return;

        base.OnDisable();

        Logger.LogInfo($"{Info.Metadata.Name} is disabled!");
    }

    // override this to load resources such as sprites, sound, or interfaces
    protected override void LoadResources()
    {
        base.LoadResources(); // this is where the mod's option interface is registered

        Logger.LogInfo("Loading resources!");
    }

    // override this to apply your mod's hooks to the game
    protected override void ApplyHooks()
    {
        base.ApplyHooks();

        On.Player.Die += PlayerDeathHook;
    }

    // override this to remove your mod's hooks from the game
    // this method is only called if the Rain Reloader mod is present
    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        On.Player.Die -= PlayerDeathHook;
    }

    // Code from SlugTemplate's ExplodeOnDeath feature
    private static void PlayerDeathHook(On.Player.orig_Die orig, Player self)
    {
        bool wasDead = self.dead;

        orig(self);

        if(!wasDead && self.dead)
        {
            // Adapted from ScavengerBomb.Explode
            var room = self.room;
            var pos = self.mainBodyChunk.pos;
            var color = self.ShortCutColor();
            room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
            room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
            room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

            room.ScreenMovement(pos, default, 1.3f);
            room.PlaySound(SoundID.Bomb_Explode, pos);
            room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
        }
    }
}
```
