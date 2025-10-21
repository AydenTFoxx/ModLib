using System;
using System.Collections.Generic;

namespace ModLib.Collections;

/// <summary>
///     A list of weakly-referenced values, which are removed when the underlying value is GC'ed.
/// </summary>
/// <typeparam name="T">The type of the elements of this list.</typeparam>
public class WeakList<T> : WeakCollection<T>, IList<T> where T : class
{
    /// <inheritdoc/>
    public T this[int index]
    {
        get => values[index].TryGetTarget(out T target) ? target : null!;
        set => values[index] = new WeakReference<T>(value);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        foreach (WeakReference<T> weakRef in values)
        {
            if (weakRef.TryGetTarget(out T target) && target == item)
            {
                return values.IndexOf(weakRef);
            }
        }

        return -1;
    }

    /// <inheritdoc/>
    public void Insert(int index, T item) => values.Insert(index, new WeakReference<T>(item));

    /// <inheritdoc/>
    public void RemoveAt(int index) => values.RemoveAt(index);
}