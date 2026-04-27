namespace MeloongCore;

/// <summary>
/// 线程安全的 <see cref="List{T}"/> 实现。
/// 在 <see cref="SynchronizedCollection{T}"/> 的基础上，当调用枚举器时返回一个临时副本，以避免列表在枚举过程中被修改导致异常。
/// </summary>
public class ConcurrentList<T> : SynchronizedCollection<T>, IEnumerable, IEnumerable<T> {

    // 构造函数
    public ConcurrentList() : base() { }
    public ConcurrentList(IEnumerable<T> data) : base(new object(), data) { }
    public static implicit operator ConcurrentList<T>(List<T> data) => new(data);
    public static implicit operator List<T>(ConcurrentList<T> data) => new(data);

    // 对枚举器进行覆写，返回一个临时副本
    public new IEnumerator<T> GetEnumerator() {
        lock (SyncRoot) {
            return Items.ToList().GetEnumerator();
        }
    }
    IEnumerator IEnumerable.GetEnumerator() {
        lock (SyncRoot) {
            return Items.ToList().GetEnumerator();
        }
    }

}