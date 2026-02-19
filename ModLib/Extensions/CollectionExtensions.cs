using System.Collections.Generic;

namespace ModLib.Extensions;

internal static class CollectionExtensions
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> self)
    {
        /// <summary>
        ///     Adds the specified key-value pair to the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The key and value pair to be added.</param>
        public void Add(KeyValuePair<TKey, TValue> keyValuePair) =>
            self.Add(keyValuePair.Key, keyValuePair.Value);
    }
}