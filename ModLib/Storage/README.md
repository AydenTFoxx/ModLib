# ModLib | Storage Module

> *This document was last updated on `2026-02-28`, and is accurate for ModLib version `0.4.1.0`.*

Provides a simple API for storing data which persists across game restarts.

## ModData

Represents a data holder for the given mod. Can store any data serializable to JSON, and by default retrieves any previously stored data on construction, and saves it to disk before the game is closed.

```cs
ModData modData = new();

modData.AddData("myKey", "myStringValue");
modData.AddData("myOtherKey", 12345);

Logger.LogDebug(modData.GetData("myKey")); // "myStringValue"
Logger.LogDebug(modData.HasData("someExtraKey")); // false
Logger.LogDebug(modData.TryGetData("myOtherKey", out int data) ? data : 0) // 12345
```
