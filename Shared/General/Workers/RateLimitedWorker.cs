namespace MeloongCore;

public enum RateLimitMode {
    /// <summary>Throttle：立即执行一次，然后一段时间内合并为一次尾随执行。</summary>
    ImmediateThenMerge,
    /// <summary>Debounce (leading)：立即执行一次，然后忽略一段时间内的后续调用。</summary>
    ImmediateThenIgnore,
    /// <summary>Debounce (trailing)：不立即执行，直到一段时间内不再有任何新调用后再执行一次。</summary>
    WaitUnillNoNewInvoke,
}

/// <summary>
/// 对高频调用进行限制。
/// 详见：<see href="https://aldaviva.com/portfolio.html#ratelimiting" />
/// </summary>
public class RateLimitedWorker(Action workload, double minimalIntervalMs, RateLimitMode mode) : IDisposable {

    private readonly RateLimitedAction action = mode switch {
        RateLimitMode.ImmediateThenMerge => Throttler.Throttle(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: true, trailing: true),
        RateLimitMode.WaitUnillNoNewInvoke => Debouncer.Debounce(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: false, trailing: true),
        _ => Debouncer.Debounce(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: true, trailing: false)
    };

    public void Start() {
        if (!disposed) action.Invoke();
    }

    private bool disposed = false;
    public virtual void Dispose() {
        if (disposed) return;
        disposed = true;
        action.Dispose();
    }

}
