# ModLib | Collections Namespace

> *This document was last updated on `2026-02-28`, and is accurate for ModLib version `0.4.1.0`.*

Contains simple generic collections for storing object references without extending the lifetime of said objects.

## Table of Contents

- [ModLib.Collections](#modlib--collections-namespace)
  - [WeakDictionary{TKey, TValue}](#weakdictionarytkey-tvalue)
  - [WeakList{T}](#weaklistt)

## WeakDictionary{TKey, TValue}

Analogous to `Dictionary<TKey, TValue>`, implements `IDictionary<TKey, TValue>`, `ICloneable`, and `IDisposable`; Requires `TKey` to be a reference type.

Allows storing references to objects (as key or value, or both) without preventing those from being collected by the garbage collector, making it suitable for storing references to game objects like `Creature` or `PhysicalObject`. When a key or value in the dictionary is collected, its corresponding pair is automatically removed when accessing the dictionary.

```cs
WeakDictionary<Creature, int> myDict = []; // Weak dictionary can be initialized like a regular collection

Creature myCreature = GetMyCreature();
int myInt = 10;

myDict.Add(myCreature, myInt);

Logger.Log(myDict[myCreature]); // -> 10
Logger.Log(myDict.Count); // -> 1
Logger.Log(myDict.FirstOrDefault(kvp => kvp.Value == myInt)?.Key ?? "None"); // -> MyCreature ...

...

myCreature.Destroy(); // referenced creature will be garbage-collected

...

// myCreature and myInt were removed (myCreature was garbage-collected)
Logger.Log(myDict.Count); // -> 0
Logger.Log(myDict.FirstOrDefault(kvp => kvp.Value == myInt)?.Key ?? "None"); // -> None
```

## WeakList{T}

Analogous to `List<T>`, implements `IList<T>, ICloneable`; Requires `T` to be a reference type.

As with the above, allows storing references to objects without preventing their garbage collection, making it appropriate for storing game objects (`Creature`, `Room`, `PhysicalObject`, etc.). When a key in the list is collected, it is automatically removed when accessing the list's values.

```cs
WeakList<Creature> myList = []; // Weak list can be initialized like a regular collection

myList.Add(GetMyCreature()); // adds one element to the list
myList.AddRange(GetThreeRandomCreatures()); // adds multiple elements to the list

Logger.Log(myList[0]); // MyCreature ...
Logger.Log(myList.Count); // 4

...

// referenced objects will be garbage-collected
foreach (var obj in myList)
{
    obj.Destroy();
}

...

// list is now empty (all elements were garbage-collected)
Logger.Log(myList.Count); // -> 0
```
