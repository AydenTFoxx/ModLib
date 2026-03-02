# ModLib | Logging Module

> [!NOTE]
>
> This document was last updated on `2026-02-28`, and is accurate for ModLib version `0.4.1.0`.

Contains logging utilities for logging to a dedicated, per-mod file, in addition to the default log files provided by the game. If the [LogUtils](https://github.com/TheVileOne/ExpeditionRegionSupport/tree/master/LogUtils) library is present, this module instead acts as a wrapper for its API.

## Table of Contents

- [ModLib.Logging](#modlib--logging-module)
  - [ModLogger](#modlogger)
    - [LogUtilsLogger](#logutilslogger)
    - [FallbackLogger](#fallbacklogger)
    - [Dynamically Creating a ModLogger Instance](#dynamically-creating-a-modlogger-instance)

## ModLogger

The base class for ModLib's representation of a logger instance. Provides roughly the same methods as a `BepInEx.Logging.ManualLogSource`, plus the ability to filter logs based on their `BepInEx.Logging.LogLevel` value.

Every `ModLogger` instance may also contain a "log source", which is usually an instance of another API's logger class used for interoperability. This instance can be retrieved with `ModLogger.GetLogSource()`, and may return `null` in cases where a log source is optional and wasn't provided by the user (e.g. a `FallbackLogger` instantiated with default arguments).

As this is an abstract class, it cannot be directly instantiated or used as a logger; Instead, an inheriting class may be used. As of writing, ModLib provides two logging classes extending `ModLogger`, plus a helper method to automatically create a new instance of the most suitable type at runtime:

### LogUtilsLogger

If the *LogUtils* library is present at runtime, this logging class is preferred over the others. It's a simple wrapper around a `LogUtils.Logger` instance, made compatible with ModLib's `ModLogger` API.

Requires a `LogUtils.Logger` for creating an instance of this logger class. Invoking `LogUtilsLogger.GetLogSource()` returns the wrapped logger instance, and is guaranteed to be a non-null value.

To create a new `LogUtilsLogger` instance:

```cs
ManualLogSource logSource = Logger.CreateLogSource("MyExampleMod");

LogUtilsLogger logger = new(logSource); // logs all requests
LogUtilsLogger filteredLogger = new(logSource, LogLevel.Fatal | LogLevel.Error | LogLevel.Warning); // only log requests from the specified levels.
```

For more details on this class' behavior at runtime, see the [`LogUtils.Logger`](https://github.com/TheVileOne/ExpeditionRegionSupport/blob/master/LogUtils/Logger.cs) class in the *LogUtils* repository.

### FallbackLogger

Used as the default `ModLogger` implementation; Logs all non-filtered requests to `BepInEx/LogOutput.log`, `consoleLog.txt` (or `exceptionLog.txt` for logs of `Error` level or above), and a separate, mod-specific file, located at `RainWorld_Data/StreamingAssets/Logs/{YourModName}.log`.

Requires a `ManualLogSource` for creating an instance of this logger class, unless the caller is [registered to ModLib](../Registry.cs#L73). Invoking `FallbackLogger.GetLogSource()` returns the internal `ManualLogSource` instance, if any was provided at construction.

To create a new `FallbackLogger` instance:

```cs
ManualLogSource logSource = Logger.CreateLogSource("MyExampleMod");

FallbackLogger logger = new(logSource); // logs all requests
FallbackLogger filteredLogger = new(logSource, LogLevel.Fatal | LogLevel.Error | LogLevel.Warning); // only logs requests from the specified levels.
```

Behavior at runtime depends on provided arguments at construction, as well as the game's initialization state:

- If a `ManualLogSource` was provided, log requests are passed to BepInEx and logged in `BepInEx/LogOutput.log`.
- If the game's assembly has already loaded, log requests are additionally logged to `consoleLog.txt`.
- Finally, logs are always logged to `RainWorld_Data/StreamingAssets/Logs/{YourModName}.log`, irrespective of when the log method is called.

### Dynamically Creating a ModLogger Instance

In most cases where fine-grained control is not needed, one can simply use the `LoggingAdapter.CreateLogger(ManualLogSource, LogLevel)` method from the static `LoggingAdapter` class. This method returns an auto-generated `ModLogger` instance using the most suitable API available:

- If *LogUtils* is present, returns a `LogUtilsLogger` containing a wrapped `LogUtils.Logger` instance auto-generated with the provided `ManualLogSource` object;
- Otherwise, returns a `FallbackLogger` containing the provided `ManualLogSource` object.

If the second parameter is specified, the new logger instance will only accept log requests whose level matches the specified criteria:

```cs
ManualLogSource logSource = Logger.CreateLogSource("MyExampleMod");

ModLogger logger = LoggingAdapter.CreateLogger(logSource); // logs any request
ModLogger filteredLogger = LoggingAdapter.CreateLogger(logSource, LogLevel.Fatal | LogLevel.Error | LogLevel.Warning); // only logs requests of level Fatal, Error, or Warning
```
