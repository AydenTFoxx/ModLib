using System.Collections.Generic;

namespace ModLib.Extensions;

internal static class CollectionExtensions
{
    /// <summary>
    ///     Adds the specified key-value pair to the dictionary.
    /// </summary>
    /// <param name="self">The dictionary itself.</param>
    /// <param name="keyValuePair">The key and value pair to be added.</param>
    public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> self, KeyValuePair<TKey, TValue> keyValuePair) => self.Add(keyValuePair.Key, keyValuePair.Value);
}