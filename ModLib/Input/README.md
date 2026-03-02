# ModLib | Input Namespace

> *This document was last updated on `2026-02-28`, and is accurate for ModLib version `0.4.1.0`.*

Contains various classes and static helpers for managing and retrieving player input. Has full compatibility with the [Improved Input Config: Extended](https://steamcommunity.com/workshop/filedetails/?id=3458119961) (IIC:E) mod, which is preferred over ModLib's own API if present.

## Table of Contents

- [ModLib.Input](#modlib--input-namespace)
  - [Keybind](#keybind)
    - [Registering a Keybind](#registering-a-keybind)
    - [Retrieving a Keybind](#retrieving-a-keybind)
    - [Using a Keybind](#using-a-keybind)
      - [Detect on every frame](#detect-on-every-frame)
      - [Detect only the first frame](#detect-only-the-first-frame)
      - [Detect even if incapacitated](#detect-even-if-incapacitated)
    - [Converting a Keybind](#converting-a-keybind)
  - [InputHandler](#inputhandler)
    - [Detecting Player Input](#detecting-player-input)
      - [Detecting input when incapacitated](#detecting-input-when-incapacitated)
    - [Detecting Non-Player Input](#detecting-non-player-input)

## Keybind

The most basic representation of a user-defined keybind. Can be used with ModLib's `InputHandler` class to detect player input, or converted to a `PlayerKeybind` and used in IIC:E's own API for the same purpose.

### Registering a Keybind

To create a new keybind, use the static methods `Keybind.Register(string, string, KeyCode, KeyCode, KeyCode)` or `Keybind.Register(string, KeyCode, KeyCode, KeyCode)`. These methods returns the newly registered keybind, or an existing one if a keybind of the same ID was already registered beforehand.

```cs
Keybind myKeybind = Keybind.Register("example.my_keybind", "My Cool Keybind", KeyCode.A, KeyCode.Joystick1Button1, KeyCode.Joystick1Button9);

Keybind myOtherKeybind = Keybind.Register("My Other Keybind", KeyCode.B, KeyCode.Joystick1Button2);

Keybind myEmptyKeybind = Keybind.Register("My Empty Keybind");

Logger.Log(myKeybind);
Logger.Log(myOtherKeybind);
Logger.Log(myEmptyKeybind);
```

When executed, the above code produces the following result:

```txt
My Cool Keybind (example.my_keybind) [A|Joystick1Button1|Joystick1Button9]
My Other Keybind (example.myotherkeybind) [B|Joystick1Button2]
My Empty Keybind (example.myemptykeybind) [None|None]
```

### Retrieving a Keybind

To retrieve a registered `Keybind` instance, use the `Keybind.Get(string)` method:

```cs
Keybind keybind = Keybind.Get("example.my_keybind");

Logger.Log(keybind);
```

When executed, the above code produces the following result:

```txt
My Cool Keybind (example.my_keybind) [A|Joystick1Button1|Joystick1Button9]
```

If no keybind is found with the specified ID, the method returns `null`:

```cs
Keybind keybind = Keybind.Get("example.unregistered-keybind");

Logger.Log(keybind); // Does not print anything to the console (keybind is null)
```

### Using a Keybind

> [!NOTE]
>
> These methods are not intended to be directly called by consumers. Instead, prefer using the extension methods `InputHandler.IsKeyDown(this Player, Keybind, bool)` and `InputHandler.WasKeyJustPressed(this Player, Keybind, bool)`, respectively.

#### Detect on every frame

To determine if a given keybind is being pressed:

```cs
Player player = GetMyPlayer();

if (myKeybind.IsDown(player.playerState.playerNumber))
{
    Logger.Log($"{myKeybind.Name} was pressed!");
}
```

Assuming the keybind is pressed for a while and released, the above code prints the following to the console:

```txt
My Cool Keybind was pressed!
My Cool Keybind was pressed!
My Cool Keybind was pressed!
My Cool Keybind was pressed!
My Cool Keybind was pressed!
... (repeats every frame while myKeybind is down)
```

#### Detect only the first frame

To detect only the first frame where a keybind is pressed:

```cs
Player player = GetMyPlayer();

if (myKeybind.WasJustPressed(player.playerState.playerNumber, player))
{
    Logger.Log($"{myKeybind.Name} was just pressed!");
}
```

Assuming the keybind is pressed for the exact same amount of time, the above code prints the following to the console:

```txt
My Cool Keybind was just pressed!
```

#### Detect even if incapacitated

If the specified player is stunned, dead, or has the map open, all keybinds' input will be ignored. To receive input even in those scenarios, pass a `null` reference for the second argument instead of a `Player` instance:

```cs
player.Die();

if (myKeybind.WasJustPressed(player.playerState.playerNumber, null))
{
    Logger.Log("This is fine.");
}
```

When running the above code and pressing the respective keybind, the following is printed to the console:

```txt
This is fine.
```

### Converting a Keybind

Both conversions to and from an `ImprovedInput.PlayerKeybind` are implicitly performed when passing one as an argument where a method requires another. If the other API does not already have a keybind of the same ID, a new one is registered using the converter's data as its arguments.

The following examples illustrate conversions between `Keybind` and `PlayerKeybind` objects:

```cs
static void PrintKeybind(Keybind keybind)
{
    Logger.Log($"Keybind is: {keybind}");
}

static void PrintPlayerKeybind(PlayerKeybind keybind)
{
    Logger.Log($"PlayerKeybind is: {keybind}");
}

static void PrintAnyKeybind(Keybind keybind)
{
    Logger.Log($"{keybind.Name} is a cool keybind from ModLib!");
}

static void PrintAnyKeybind(PlayerKeybind keybind)
{
    Logger.Log($"{keybind.Name} is a cool keybind from IIC:E!");
}

Keybind keybind = Keybind.Register("example.modlib_keybind", "My ModLib Keybind");
PlayerKeybind playerKeybind = PlayerKeybind.Register("example.iic_keybind", "Example Mod", "My IIC:E Keybind");

PrintKeybind(playerKeybind); // Implicit conversion to ModLib's Keybind object (registers a new keybind with the same data)

PrintPlayerKeybind(keybind); // Implicit conversion to IIC:E's PlayerKeybind object (registers a new keybind with the same data)

PrintAnyKeybind(playerKeybind); // No conversion performed (invokes the overload accepting a PlayerKeybind)
PrintAnyKeybind((Keybind)playerKeybind); // Explicit conversion to ModLib's Keybind object (invokes the overload accepting a Keybind)
```

When executed, the above code produces the following result:

```txt
Keybind is: My IIC:E Keybind (example.iic_keybind) [None|None]
PlayerKeybind is: example.modlib_keybind
My IIC:E Keybind is a cool keybind from IIC:E!
My IIC:E Keybind is a cool keybind from ModLib!
```

## InputHandler

Designed for performing safety checks and bridging compatibility between IIC:E and ModLib's input APIs, the static class `InputHandler` ensures input is retrieved as conveniently and accurately as possible in all occasions.

Methods from this class primarily accept `Keybind` objects, but [IIC:E keybinds may also be used for the same purpose.](#converting-a-keybind)

### Detecting Player Input

To detect input for a given player:

```cs
Player player = GetMyPlayer();
Keybind myKeybind = Keybind.Get("example.my_keybind");

if (player.IsKeyDown(myKeybind)) // detect every frame
{
    Logger.Log($"Player is pressing {myKeybind.Name}!");
}

if (player.WasKeyJustPressed(myKeybind)) // detect only the first frame
{
    Logger.Log($"Player has just pressed {myKeybind.Name}!");
}
```

When executed, the above code produces the following result:

```txt
Player is pressing My Cool Keybind!
Player has just pressed My Cool Keybind!
Player is pressing My Cool Keybind!
Player is pressing My Cool Keybind!
Player is pressing My Cool Keybind!
Player is pressing My Cool Keybind!
Player is pressing My Cool Keybind!
...
```

If the player is a NPC (i.e. a Slugpup, except if controlled by the player in Safari Mode), dead, stunned, using the map or in a cutscene, the above methods will always return `false`.

#### Detecting input when incapacitated

To detect input from the given player even in the above conditions (save for Slugpups), both methods accept an optional parameter of type `bool`, which can be set to `true` to bypass these limitations:

```cs
player.Stun(1000);

if (player.IsKeyDown(myKeybind, true)) // ignores stun/death/map/cutscene input-blocking
{
    Logger.Log("I'm still kickin'!");
}
```

When executed, the above code produces the following result:

```txt
I'm still kickin'!
I'm still kickin'!
I'm still kickin'!
I'm still kickin'!
...
```

### Detecting Non-Player Input

> [!WARNING]
>
> This feature is experimental and somewhat untested; Use at your own risk.

If the *Improved Input Config: Extended* mod is enabled, receiving input from non-`Player` instances is also possible, with a little bit of setup. This is especially useful for when the targeted player is not playing as a Slugcat, or simply to listen for input when a `Player` instance is not available.

To enable custom input detection for a given player index, call the method `InputHandler.AddInputListener(int)`, with the first argument as the index of the player whose input will be read:

```cs
InputHandler.AddInputListener(playerIndex);

// Example: (Receives custom input from the first four players, perfect for Jolly Co-op compatibility)

InputHandler.AddInputListener(0); // Listen for custom input from the first player
InputHandler.AddInputListener(1); // Listen for custom input from the second player
InputHandler.AddInputListener(2); // Listen for custom input from the third player
InputHandler.AddInputListener(3); // Listen for custom input from the fourth player
```

To retrieve input for a given player index and keybind, use the static methods `InputHandler.IsKeyDown(int, Keybind, bool)` and `InputHandler.WasKeyJustPressed(int, Keybind, bool)`:

```cs
Keybind myKeybind = Keybind.Register("My Cool Keybind");

if (InputHandler.WasKeyJustPressed(0, myKeybind)) // detect input from first player once
{
    Logger.Log("I have escaped my mortal flesh.");
}

if (InputHandler.IsKeyDown(0, myKeybind))  // detect input from first player every tick
{
    Logger.Log($"I am pressing {myKeybind.Name}!");
}
```

When executed, the above code produces the following result:

```txt
I have escaped my mortal flesh.
I am pressing My Cool Keybind!
I am pressing My Cool Keybind!
I am pressing My Cool Keybind!
I am pressing My Cool Keybind!
...
```

Input retrieved this way is always considered to be "raw" (i.e. ignores whether the target could receive inputs or not).
