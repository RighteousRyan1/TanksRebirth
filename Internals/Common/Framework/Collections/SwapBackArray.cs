using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TanksRebirth.Internals.Common.Framework.Collections;
public class SwapBackArray<T>(int capacity = 256) : IEnumerable<T> {
    T[] _items = new T[capacity];
    public int Count { get; private set; } = 0;
    public int Capacity => _items.Length;

    public Action<int, T>? OnSwapBack;

    public void Add(T item) {
        if (Count >= _items.Length)
            Resize(_items.Length * 2);
        _items[Count++] = item;
    }
    public int AddWithIndex(T item) {
        if (Count >= _items.Length)
            Resize(_items.Length * 2);
        _items[Count] = item;
        return Count++;
    }

    public void RemoveAt(int index) {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        // swap with last element
        int last = Count - 1;
        _items[index] = _items[Count - 1];
        _items[last] = default!;

        if (index != last)
            OnSwapBack?.Invoke(index, _items[index]);

        Count--;
    }

    public T this[int index] {
        get {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
        set {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _items[index] = value;
        }
    }

    public void Clear() {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    private void Resize(int newSize) {
        Array.Resize(ref _items, newSize);
    }

    public IEnumerator<T> GetEnumerator() {
        for (int i = 0; i < Count; i++)
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
