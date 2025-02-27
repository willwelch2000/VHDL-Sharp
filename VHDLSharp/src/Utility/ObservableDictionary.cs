using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp.Utility;

/// <summary>
/// Dictionary that raises event when it is changed
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> backendDictionary = [];

    private ICollection<KeyValuePair<TKey, TValue>> BackendDictionaryAsCollection => backendDictionary;

    private NotifyCollectionChangedEventHandler? collectionChanged;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add
        {
            collectionChanged -= value; // remove if already present
            collectionChanged += value;
        }
        remove => collectionChanged -= value;
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys => backendDictionary.Keys;

    /// <inheritdoc/>
    public ICollection<TValue> Values => backendDictionary.Values;

    /// <inheritdoc/>
    public int Count => backendDictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public virtual TValue this[TKey key]
    {
        get => backendDictionary[key];
        set
        {
            if (backendDictionary.TryGetValue(key, out TValue? oldValue))
            {
                backendDictionary[key] = value;
                collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, oldValue)));
            }
            else
                Add(key, value);
        }
    }

    /// <inheritdoc/>
    public virtual void Add(TKey key, TValue value)
    {
        backendDictionary.Add(key, value);
        collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => backendDictionary.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        backendDictionary.TryGetValue(key, out TValue? oldValue);
        bool removed = backendDictionary.Remove(key);
        if (removed)
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, oldValue ?? throw new("Old value null"))));
        return removed;
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        backendDictionary.TryGetValue(key, out value);

    /// <inheritdoc/>
    public void Clear()
    {
        backendDictionary.Clear();
        collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item) => backendDictionary.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => BackendDictionaryAsCollection.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        bool removed = backendDictionary.Remove(item.Key);
        if (removed)
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        return removed;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => backendDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        IEnumerable enumerable = backendDictionary;
        return enumerable.GetEnumerator();
    }
}