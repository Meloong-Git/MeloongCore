namespace MeloongCore;

/// <summary>
/// 使用粗粒度锁的线程安全 <see cref="List{T}"/> 实现。
/// 通常建议使用 <see cref="ConcurrentBag{T}"/> 或其他线程安全集合，它们具有更好的性能。
/// </summary>
public class ConcurrentList<T> : IList<T>, IList {

    private readonly List<T> _items = [];

    // 构造函数
    public ConcurrentList() { }
    public ConcurrentList(IEnumerable<T> data) => _items = new(data);
    public static implicit operator ConcurrentList<T>(List<T> data) => new(data);
    public static implicit operator List<T>(ConcurrentList<T> data) { lock (data.SyncRoot) { return new(data._items); } }

    // 枚举器
    /// <summary>
    /// 返回此列表的副本。
    /// </summary>
    public IEnumerator<T> GetEnumerator() { lock (SyncRoot) return _items.ToList().GetEnumerator(); }
    /// <summary>
    /// 返回此列表的副本。
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() { lock (SyncRoot) return _items.ToList().GetEnumerator(); }

    // 成员
    public int Count { get { lock (SyncRoot) { return _items.Count; } } }
    public bool IsReadOnly => false;
    bool IList.IsFixedSize => false;
    bool ICollection.IsSynchronized => true;
    public readonly object SyncRoot = new();
    object ICollection.SyncRoot => SyncRoot;

    public T this[int index] {
        get { lock (SyncRoot) { return _items[index]; } }
        set { lock (SyncRoot) { _items[index] = value; } }
    }
    object? IList.this[int index] {
        get => this[index];
        set { lock (SyncRoot) { _items[index] = (T) value!; } }
    }

    public void Add(T item) { lock (SyncRoot) { _items.Add(item); } }
    int IList.Add(object? value) { lock (SyncRoot) { _items.Add((T) value!); return _items.Count - 1; } }

    public void Insert(int index, T item) { lock (SyncRoot) { _items.Insert(index, item); } }
    void IList.Insert(int index, object? value) { lock (SyncRoot) { _items.Insert(index, (T) value!); } }

    public bool Remove(T item) { lock (SyncRoot) { return _items.Remove(item); } }
    void IList.Remove(object? value) { lock (SyncRoot) { if (value is T t) _items.Remove(t); } }

    public void RemoveAt(int index) { lock (SyncRoot) { _items.RemoveAt(index); } }
    public void Clear() { lock (SyncRoot) { _items.Clear(); } }

    public bool Contains(T item) { lock (SyncRoot) { return _items.Contains(item); } }
    bool IList.Contains(object? value) { lock (SyncRoot) { return value is T t && _items.Contains(t); } }

    public int IndexOf(T item) { lock (SyncRoot) { return _items.IndexOf(item); } }
    int IList.IndexOf(object? value) { lock (SyncRoot) { return value is T t ? _items.IndexOf(t) : -1; } }

    public void CopyTo(T[] array, int arrayIndex) { lock (SyncRoot) { _items.CopyTo(array, arrayIndex); } }
    void ICollection.CopyTo(Array array, int index) { lock (SyncRoot) { ((ICollection) _items).CopyTo(array, index); } }

}