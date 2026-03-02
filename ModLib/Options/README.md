# ModLib | Options Module

> *This document was last updated on `2026-03-01`, and is accurate for ModLib version `0.4.1.0`.*

Contains the primary API for setting and retrieving mod options from registered mods, as well as a simple helper for building REMIX menus.

## Table of Contents

- [ModLib.Options](#modlib--options-module)
  - [SharedOptions](#sharedoptions)
    - [Ephemeral Values](#ephemeral-values)
    - [Option Holders](#option-holders)
      - [The ClientOption Attribute](#the-clientoption-attribute)
  - [OptionBuilder](#optionbuilder)

## SharedOptions

Provides the main API for setting and retrieving mod options. Values are retrieved from option holders provided during mod registration, and are automatically synced with other clients in a *Rain Meadow* lobby.

Data can be retrieved and compared using `Configurable<T>` instances, or their respective `string` keys:

```cs
Configurable<bool> myConfigurable = new(null, "myConfigurableKey", true, null);

Logger.LogDebug(SharedOptions.GetOptionValue<bool>(myConfigurable)); // -> true
Logger.LogDebug(SharedOptions.IsOptionEnabled(myConfigurable.Key)); // -> true
Logger.LogDebug(SharedOptions.IsOptionValue("myConfigurableKey", false)); // -> false
```

Stored data may also be set, removed, and/or overriden at any point, without affecting the original values stored in the mod's options file:

```cs
SharedOptions.SetOption("myNewOption", "wawa");

Logger.LogDebug(SharedOptions.GetOptionValue<string>("myNewOption")); // -> "wawa"
Logger.LogDebug(SharedOptions.IsOverriden("myNewOption")); // -> false

SharedOptions.SetOption("myNewOption", "omiyo"); // overrides previous value

Logger.LogDebug(SharedOptions.GetOptionValue<string>("myNewOption")); // -> "omiyo"
Logger.LogDebug(SharedOptions.IsOverriden("myNewOption")); // -> true

SharedOptions.RemoveOption("myNewOption", tempOnly: true); // remove only overrides, keep base value

Logger.LogDebug(SharedOptions.GetOptionValue<string>("myNewOption")); // -> "wawa"
Logger.LogDebug(SharedOptions.IsOverriden("myNewOption")); // -> false

SharedOptions.RemoveOption("myNewOption"); // remove base value and overrides

Logger.LogDebug(SharedOptions.GetOptionValue<string>("myNewOption")); // -> null (option was fully removed)
```

Do note that option overrides must be of the same type as their base value; Attempting to set an option to a different value type will throw an `ArgumentException` at runtime:

```cs
SharedOptions.SetOption("myOption", 1234); // myOption is initialized as System.Int32
SharedOptions.SetOption("myOption", false); // throws System.ArgumentException (expected Int32, got Boolean)
```

In order to change the type of an option at runtime, it must first be removed with `SharedOptions.RemoveOption(string)`, then re-added with the desired type using `SharedOptions.SetOption<T>(string, T)`.

> [!NOTE]
>
> Avoid changing the type of registered options at runtime, as it may inadvertently break other mods whom may not be expecting that change!

### Ephemeral Values

When setting a new value or overriding an existing one, it can be optionally set as *ephemeral*. Ephemeral values are prefixed by a `!`, and are removed whenever the `SharedOptions` collection is refreshed, be it from a new cycle start or exiting to the main menu screen.

When an ephemeral value is removed,one of two behaviors may occur:

- If it was an override, the original value is restored.
- Otherwise, the key is removed from the options collection.

### Option Holders

When registering an assembly to ModLib, a `Type` argument may be optionally provided to determine that assembly's *option holder* class, typically one inheriting `OptionInterface`. If such class is provided, ModLib will search its members for any `public static` field of type `Configurable<T>`, then load its values to `SharedOptions`. Whenever a refresh occurs, any non-overriden values are automatically updated to the values of their respective configurables.

The following is an example of a typical option holder class:

```cs
public class OptionHolder : OptionInterface
{
    public static Configurable<bool> MyBooleanOption;
    public static Configurable<string> MyStringOption;
    public static Configurable<int> MyInt32Option;

    public OptionHolder()
    {
        MyBooleanOption = config.Bind("my_boolean", true);
        MyStringOption = config.Bind("my_string", "naw'ag hanla gaenma", new ConfigAcceptableList<string>("naw'ag hanla gaenma", "sanbaga hanla gaenma", "sahi"));
        MyInt32Option = config.Bind("my_int32", 123456, new ConfigAcceptableRange<int>(1, 100));
    }
}
```

When registered to ModLib, the above class would produce the following auto-generated options:

```cs
- my_boolean: true
- my_string: "naw'ag hanla gaenma"
- my_int32: 123456
```

#### The ClientOption Attribute

If a given option is meant to represent a client-side feature, the `ClientOptionAttribute` can be added before it to prevent it from being synced in a *Rain Meadow* lobby:

```cs
[ClientOption]
Configurable<Color> MyCustomColor; // This will not be synced in Rain Meadow
```

When syncing the clients' `SharedOptions` collection, any options with the above attribute are also ignored for sync.

## OptionBuilder

A simple helper for building basic REMIX menus, with a focus on creating various option-related components, like check boxes, lists, and buttons.

```cs

public class OptionHolder : OptionInterface
{
    public static Configurable<bool> MyBooleanOption;
    public static Configurable<string> MyStringOption;
    public static Configurable<int> MyInt32Option;

    public OptionHolder()
    {
        // ...
    }

    public override void Initialize()
    {
        base.Initialize();

        Tabs = [
            // constructor takes an OptionInterface name, plus the name of the new tab
            new OptionBuilder(this, "Main")
                .AddCheckBoxOption("My Boolean", MyBooleanOption) // Automatically adds a label alongside the check box
                .AddComboBoxOption("My Strings", MyStringOption) // same as above; choose from a predefined set of strings
                .AddSliderOption("My Int", MyInt32Option) // a slider between min and max
                .Build() // returns the generated OpTab object
        ];
    }
}
```
