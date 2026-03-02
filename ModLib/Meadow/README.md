# ModLib | Meadow Module

> *This document was last updated on `2026-03-01`, and is accurate for ModLib version `0.4.1.0`.*

Contains various classes for interaction and compatibility with the [Rain Meadow](https://github.com/henpemaz/Rain-Meadow/tree/main) mod.

> [!WARNING]
>
> If the *Rain Meadow* mod is not present, accessing any class from this module WILL throw a `System.TypeLoadException` at runtime.
>
> For a list of safe methods for optionally calling members of this module without throwing an exception, see the [safe encapsulation techniques](#safe-encapsulation-techniques) section below.

## Table of Contents

- [ModLib.Meadow](#modlib--meadow-module)
  - [MeadowUtils](#meadowutils)
  - [ModRPCManager](#modrpcmanager)
  - [SerializableDictionary{TOnlineKey, TOnlineValue}](#serializabledictionarytonlinekey-tonlinevalue)
  - [Safe Encapsulation Techniques](#safe-encapsulation-techniques)
    - [Preventing Code Execution](#preventing-code-execution)
      - [Using the Extras class (Recommended)](#using-the-extras-class-recommended)
      - [Using the CompatibilityManager class](#using-the-compatibilitymanager-class)
      - [Querying for Rain Meadow Directly](#querying-for-rain-meadow-directly)
    - [Prevention and Handling of Exceptions](#prevention-and-handling-of-exceptions)
      - [Wrapping the Code in a Try/Catch](#wrapping-the-code-in-a-trycatch)
      - [Wrapping the Code with Extras class](#wrapping-the-code-with-extras-class)
      - [Moving the Code to a Separate Class](#moving-the-code-to-a-separate-class)

## MeadowUtils

Provides various helpers for querying and interacting with an online context (i.e. a Rain Meadow lobby).Also contains events for joining an online game session (not the lobby itself), as well as for when a player joins a lobby the current client is already in.

For details about this class and its members, see its [API documentation](https://aydentfoxx.github.io/ModLib/api/ModLib.Meadow.MeadowUtils.html).

## ModRPCManager

Contains a handful of methods for safely sending RPCs to players, which are automatically aborted if they exceed a given time limit.

```cs
OnlinePlayer onlinePlayer = GetOnlinePlayer();

// Sends an RPC to a player directl; Automatically aborted after 30s unless processed by the receiver.
onlinePlayer.SendRPCEvent(MyRPCEvent, "My argument", 123, false);

OnlineCreature onlineCreature = GetOnlineCreature();

// same as BroadcastRPCInRoom, except it's not repeated if called multiple times
onlineCreature.BroadcastOnceRPCInRoom(MyOtherRPCEvent);

// Broadcast to all players in the current lobby except the caller of the method
ModRPCManager.BroadcastOnceRPCInLobby(MyLobbyRPC, "Wawa!");
```

## SerializableDictionary{TOnlineKey, TOnlineValue}

Represents a serializable `Dictionary<TKey, TValue>` instance, allowing it to be used for passing dictionary-like structures in RPCs or similar.

```cs
[RPCMethod]
static void MyRPCMethod(RPCEvent rpcEvent, SerializableDictionary<OnlineCreature, OnlinePlayer> data)
{
    Logger.LogDebug($"Number of elements in received data: {data.Count}");
}

SerializableDictionary<OnlineCreature, OnlinePlayer> playerAvatars = [];

OnlineCreature myCreature = GetMyOnlineCreature();
OnlinePlayer onlinePlayer = GetOnlinePlayer();

playerAvatars.Add(myCreature, onlinePlayer);

Logger.LogDebug(playerAvatars[myCreature]); // -> "1:MyPlayerName"

onlinePlayer.SendRPCEvent(MyRPC, playerAvatars);
```

On the receiving player's end, the following is printed to console:

```txt
Number of elements in received data: 1
```

The methods used for serializing the key and value types are automatically retrieved when calling `CustomSerialize(Serializer)`; The specified types must have a serialization method registered to Rain Meadow, or an `InvalidOperationException` will be thrown at runtime:

```cs
SerializableDictionary<RainWorldGame, OnlinePlayer> gameSessions = [];

// Throws an InvalidOperationException (RainWorldGame has no serialization method)
gameSessions.CustomSerialize();
```

## Safe Encapsulation Techniques

Accessing any members of the `ModLib.Meadow` namespace when the *Rain Meadow* mod is not enabled will throw a `System.TypeLoadException` at runtime. In order to prevent this, there are numerous techniques which can be used to safely access Meadow code, and if it is not present, to handle exceptions gracefully.

Notice the below is *not* an exhaustive list; Other methods may also be used, as long as ALL Meadow-related code is not accessed if the respective mod is not present.

### Preventing Code Execution

The following methods may be used to determine Rain Meadow's presence, with varying degrees of accuracy:

#### Using the Extras class (Recommended)

The `Extras.IsMeadowEnabled` property can be used to safely determine if the Rain Meadow mod is present:

```cs
if (Extras.IsMeadowEnabled)
{
    AccessMeadowExclusiveContent();
}
```

This property will accurately return `true` whenever Rain Meadow is enabled, even before the game itself initializes:

```cs
using BepInEx;

[BepInPlugin("example.mod", "Example Mod", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    public Plugin()
    {
        if (Extras.IsMeadowEnabled)
        {
            // This will execute as long as Rain Meadow is enabled in the game's REMIX menu
            Logger.LogInfo("Rain Meadow is enabled!");
        }
    }
}
```

Alternatively, the `Extras.IsOnlineSession` property can be used to safely determine that the player is currently in a Rain Meadow *lobby*, also determining if the mod is present beforehand to avoid throwing exceptions:

```cs
if (Extras.IsOnlineSession) // Equivalent to `Extras.IsMeadowEnabled && MeadowUtils.IsOnline`
{
    Logger.LogInfo("Player is in a Rain Meadow lobby!");
}
```

#### Using the CompatibilityManager class

> [!TIP]
>
> Prefer using the `Extras` class if possible; It's more accurate, easier to use, and provides various other properties for Rain Meadow compatibility.

The `CompatibilityManager` class may also be used to determine if Rain Meadow is present:

```cs
bool isMeadowEnabled = CompatibilityManager.IsModEnabled("henpemaz_rainmeadow", true);
```

Alternatively, the shortcut method `IsRainMeadowEnabled()` may be used instead:

```cs
// Equivalent to the above method call, but shorter
bool isMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
```

Both methods will correctly return `true` if the Rain Meadow mod is enabled, irrespective of when or where they are called.

#### Querying for Rain Meadow Directly

> [!WARNING]
>
> This method has some limitations and may not return the correct value at all times.

Rain Meadow's presence can be determined directly with the following method:

```cs
// Warning! This code has limitations; See below for details.
bool isMeadowEnabled = ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow");
```

This should return `true` as long as Rain Meadow is enabled and `ModManager` has initialized.

> [!WARNING]
>
> Querying for Rain Meadow directly may not always yield the expected value, depending on when the above method is called:
>
> ```cs
> using BepInEx;
>
> [BepInPlugin("example.mod", "Example Mod", "1.0.0.0")]
> public class Plugin : BaseUnityPlugin
> {
>     public Plugin()
>     {
>         if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
>         {
>             // This code will never execute:
>             Logger.LogInfo("Found Rain Meadow during .ctor!");
>         }
>     }
>
>     public void OnEnable()
>     {
>         if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
>         {
>             // This code will also not execute:
>             Logger.LogInfo("Found Rain Meadow during OnEnable!");
>         }
>
>         On.RainWorld.PostModsInit += PostModsInitHook;
>     }
>
>     private static void PostModsInitHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
>     {
>         orig.Invoke(self);
>
>         if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
>         {
>             // This code will always execute if the Rain Meadow mod is enabled
>             Logger.LogInfo("Found Rain Meadow during PostModsInit!");
>         }
>     }
> }
> ```
>
> Only the last `if` block is guaranteed to run when Rain Meadow is enabled; If detecting Rain Meadow's presence before that is necessary, use the `Extras.IsMeadowEnabled` property instead.

### Prevention and Handling of Exceptions

As discussed before, attempting to use any Meadow-related code when the Rain Meadow mod is not present will throw a `TypeLoadException` at runtime. There are many ways to circumvent that, each with their own pros and cons:

#### Wrapping the Code in a Try/Catch

This is the most straightforward and acceptable solution; Wrapping the code in a `try` block, then handling exceptions with `catch` will prevent all other code from being affected by thrown exceptions:

```cs
try
{
    AccessMeadowExclusiveCode();
}
catch
{
    DoSomethingIfFailed();
}
```

In the above example, `AccessMeadowExclusiveCode()` will first attempt to run, and if it fails, `DoSomethingIfFailed()` will execute instead.

Pros:

- Built-in support, has minimal overhead
- Exceptions can be handled individually as needed

Cons:

- Having several `try`/`catch` blocks in the same method might make the code less readable.
- Does not prevent a `TypeLoadException` from being thrown if the `try` block directly contains Rain Meadow-specific code.

#### Wrapping the Code with Extras class

Ideal for exception-handling entire methods, although less flexible than other options. Internally uses a `try`/`catch` block for exception handling, automatically logging any thrown exception(s) with the caller mod's registered logger, if any.

```cs
Extras.WrapAction(() => { // passing a lambda expression to be executed
    bool value = DoSomething();

    if (value)
        throw new Exception("An error occurred!");
});

Extras.WrapAction(DoSomethingOrThrow); // passing a method delegate

// This code will still be executed, even if the above methods throw an exception
Logger.LogDebug("This code is fine.");
```

Pros:

- Easiest to setup, exception handling is done by ModLib
- Can receive entire methods as its argument, and will safely handle exceptions of their entire code
- Significantly cleaner code, especially when wrapping method delegates directly

Cons:

- Has a slightly bigger overhead compared to directly using `try`/`catch`
- Cannot customize how exceptions are handled; If an exception is thrown, it will always log to the caller's logger and exit the method
- If using lambda expressions, a `TypeLoadException` can still be thrown if its code contains Rain Meadow-specific code (with an even more enigmatic stack trace than before)

#### Moving the Code to a Separate Class

Separate all Meadow-related code into its own class, then check for Meadow's presence before calling the dedicated class' methods. This technique is widely used within ModLib itself, and ensures no `TypeLoadException`s are thrown when handling third-party code.

```cs
using BepInEx;
using System.Runtime.CompilerServices;

[BepInPlugin("example.mod", "Example Mod", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    public void OnEnable()
    {
        if (Extras.IsMeadowEnabled)
        {
            RainMeadowAccess.AccessMeadowCode();
        }
    }

    private static class RainMeadowAccess
    {
        public static void AccessMeadowCode()
        {
            try
            {
                // Do something
            }
            catch
            {
                // Handle exceptions...
            }
        }
    }
}
```

Pros:

- Provides complete encapsulation; No `TypeLoadException` is thrown as long as the separate class is not accessed if Rain Meadow is not present
- Can be used with previous methods to ensure Rain Meadow code is only executed if the mod is enabled, and that exceptions thrown by accessing its methods do not affect surrounding code.

Cons:

- Represents additional clutter; Code is harder to read.
- Decentralization of responsibility might introduce repetition in the codebase.
