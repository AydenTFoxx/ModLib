using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ModLib.Collections;

/// <summary>
///     A list of weakly-referenced values, which are removed when the underlying value is garbage-collected.
/// </summary>
/// <typeparam name="T">The type of the elements of this list.</typeparam>
public class WeakList<T> : IList<T>, ICloneable where T : class
{
    /// <summary>
    ///     The internal collection used for tracking <see cref="WeakReference{T}"/> objects.
    /// </summary>
    protected readonly List<WeakReference<T>> values;

    /// <summary>
    ///     Creates a new collection of weak references to a given type.
    /// </summary>
    public WeakList()
    {
        values = [];
    }

    /// <summary>
    ///     Creates a new collection of weak references to a given type with the provided initial capacity.
    /// </summary>
    /// <param name="capacity">The number of elements the collection can initially store.</param>
    public WeakList(int capacity)
    {
        values = new(capacity);
    }

    /// <summary>
    ///     Creates a new collection of weak references to a given type containing elements copied from the provided collection.
    /// </summary>
    /// <param name="collection">The collection whose values will be copied.</param>
    public WeakList(IEnumerable<T> collection)
    {
        values = [];
        values.AddRange(from T item in collection
                        select new WeakReference<T>(item));
    }

    /// <summary>
    ///     Creates a new collection of weak references to a given type containing references copied from the provided collection.
    /// </summary>
    /// <param name="collection">The collection whose values will be copied.</param>
    public WeakList(IEnumerable<WeakReference<T>> collection)
    {
        values = [];
        values.AddRange(collection);
    }

    /// <summary>
    ///     Adds a new weak reference to the <see cref="WeakList{T}"/>.
    /// </summary>
    /// <param name="item">The object to be referenced.</param>
    public void Add(T item) => values.Add(new WeakReference<T>(item));

    /// <summary>
    ///     Removes all items from the <see cref="WeakList{T}"/>.
    /// </summary>
    public void Clear() => values.Clear();

    /// <summary>
    ///     Gets the number of valid references contained in the <see cref="WeakList{T}"/>.
    /// </summary>
    /// <returns>The number of valid references contained in the <see cref="WeakList{T}"/>.</returns>
    public int Count
    {
        get
        {
            Purge();
            return values.Count;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the <see cref="WeakList{T}"/> is read-only. This value always returns <c>false</c>.
    /// </summary>
    /// <returns><c>false</c>.</returns>
    public bool IsReadOnly => false;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Determines whether the <see cref="WeakList{T}"/> contains a specific value.
    /// </summary>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="WeakList{T}"/>; <c>false</c> otherwise.</returns>
    public bool Contains(T item)
    {
        foreach (T element in this)
        {
            if (Equals(element, item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     Copies the elements of the <see cref="WeakList{T}"/> to an Array, starting at a particular Array index.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from <see cref="WeakList{T}"/>. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (T element in this)
        {
            array[arrayIndex++] = element;
        }
    }

    /// <summary>
    ///     Removes the first occurrence of a specific object from the <see cref="WeakList{T}"/>.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="WeakList{T}"/>.</param>
    /// <returns>
    ///     true if item was successfully removed from the <see cref="WeakList{T}"/>; otherwise, false.
    ///     This method also returns false if item is not found in the original <see cref="WeakList{T}"/>.
    /// </returns>
    public bool Remove(T item)
    {
        foreach (WeakReference<T> weakRef in values)
        {
            if (weakRef.TryGetTarget(out T target) && target == item)
            {
                return values.Remove(weakRef);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (!values[i].TryGetTarget(out T element))
            {
                values.RemoveAt(i);
                continue;
            }
            yield return element;
        }
    }

    /// <inheritdoc/>
    object ICloneable.Clone() => Clone();

    /// <summary>
    ///     Creates a new <see cref="WeakList{T}"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="WeakList{T}"/> that is a copy of this instance.</returns>
    public WeakList<T> Clone() => new(values);

    /// <summary>
    ///     Removes all items in the collection whose reference value has been dropped.
    /// </summary>
    public void Purge() => values.RemoveAll(static weakref => !weakref.TryGetTarget(out _));

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

    /// <summary>
    ///     Converts a <see cref="WeakList{T}"/> instance to a non-weak <see cref="List{T}"/> collection.
    /// </summary>
    /// <param name="self">The weak list to be converted.</param>
    public static implicit operator List<T>(WeakList<T> self)
    {
        self.Purge();
        return [.. self];
    }

    /// <summary>
    ///     Converts a <see cref="List{T}"/> instance to a <see cref="WeakList{T}"/> collection.
    /// </summary>
    /// <param name="self">The list to be converted.</param>
    public static explicit operator WeakList<T>(List<T> self)
    {
        self.RemoveAll(static t => t is null);
        return [.. self];
    }
}