namespace MeloongCore;

public interface IWorker {
    /// <summary>在工作线程运行工作负载。</summary>
    void Start(CancellationToken cancellationToken = default, ProgressProvider? progressOverride = null);
    /// <summary>取消当前运行。</summary>
    void Cancel();
    /// <summary>若当前正在运行中，等待其完成。若未在运行中，则立即返回。</summary>
    /// <returns>若未超时则返回 true。</returns>
    bool WaitIfRunning(int millisecondsTimeout = -1, CancellationToken cancellationToken = default);
    /// <summary>若当前正在运行中，异步等待其完成。若未在运行中，则立即返回。</summary>
    /// <returns>若未超时则返回 true。</returns>
    Task<bool> WaitIfRunningAsync(int millisecondsTimeout = -1, CancellationToken cancellationToken = default);

    /// <summary>从空闲状态进入运行状态时触发。</summary>
    event Action? Started;
    /// <summary>运行结束并进入空闲状态时触发。</summary>
    event Action? Stopped;
    /// <summary>工作负载成功完成时触发。</summary>
    public event Action? Succeeded;
    /// <summary>工作负载执行失败时触发，参数为发生的异常。</summary>
    public event Action<Exception>? Failed;
    /// <summary>运行被取消时触发。</summary>
    event Action? Canceled;

    /// <summary>当前是否正在运行。</summary>
    bool Running { get; }
    /// <summary>是否曾经有过未被取消且未失败的运行。</summary>
    bool HasSucceeded { get; }
    /// <summary>标识上次进入空闲时，并非因为取消或失败，而是正常运行到结束。</summary>
    bool LastSucceeded { get; }
    /// <summary>当前工作负载的执行进度。</summary>
    ProgressProvider Progress { get; }
}

/// <summary>
/// 可合并和重启的工作器。
/// <para/> 在工作负载运行期间多次调用 <see cref="Start(CancellationToken)"/> 会取消当前负载并使用最新令牌重新执行；多次调用也只重新执行一次。
/// </summary>
public abstract class RedoableWorkerBase<TOut>(Func<CancellationToken?, ProgressProvider?, TOut> workload, 
    ProgressProvider? progress = null, [CallerMemberName] string creatorMemberName = "") : IWorker {

    // ============================================ 状态与事件 ============================================

    /// <inheritdoc/>
    public bool Running { get { lock (this) return running; } }
    private bool running;
    /// <inheritdoc/>
    public event Action? Started;
    /// <inheritdoc/>
    public event Action? Stopped;

    /// <inheritdoc/>
    public bool HasSucceeded { get { lock (this) return hasSucceeded; } }
    private bool hasSucceeded;
    /// <inheritdoc/>
    public event Action? Succeeded;
    /// <inheritdoc/>
    public event Action<Exception>? Failed;
    /// <inheritdoc/>
    public event Action? Canceled;

    /// <inheritdoc/>
    public bool LastSucceeded { get { lock (this) return lastSucceeded; } }
    private bool lastSucceeded;

    /// <summary>上次未被取消且未失败的运行中，工作负载的返回值。</summary>
    public TOut LastResult {
        get {
            lock (this) {
                if (typeof(TOut) == typeof(NoType)) throw new InvalidOperationException("该 Worker 没有返回值。");
                if (!hasSucceeded) throw new InvalidOperationException("从未成功完成过。");
                return lastResult == null ? default! : (TOut) lastResult;
            }
        }
    }
    private object? lastResult;

    /// <inheritdoc/>
    public ProgressProvider Progress { get; private set; } = progress ?? new ProgressProvider();

    private bool pendingRedo;

    // =========================================== 运行与取消 ===========================================

    private CancellationTokenSource? realCts;
    private CancellationToken lastToken;
    private readonly ManualResetEventSlim idleEvent = new(initialState: true);

    /// <summary>在工作线程运行工作负载。若当前已在运行，则取消当前负载并使用最新令牌重启。</summary>
    public void Start(CancellationToken cancellationToken = default, ProgressProvider? progressOverride = null) {
        // 接取运行状态
        lock (this) {
            lastToken = cancellationToken;
            if (progressOverride != null) Progress = progressOverride;
            if (running) {
                Logger.Info($"{creatorMemberName}：接到重启请求");
                pendingRedo = true;
                realCts?.Cancel();
                return;
            }
            running = true; idleEvent.Reset();
            realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
        }
        var th = new Thread(_Start) { IsBackground = true, Name = $"W/{creatorMemberName}" };
        th.Start();
    }
    private void _Start() {
        Logger.Info($"{creatorMemberName}：运行开始");
        Started?.Invoke();
        while (true) {
            try {
                realCts!.Token.ThrowIfCancellationRequested();
                Progress?.Reset();
                TOut result = workload(realCts.Token, Progress); // 实际的执行
                realCts.Token.ThrowIfCancellationRequested();
                lock (this) {
                    realCts?.Dispose();
                    if (pendingRedo) { // 接取重启请求
                        pendingRedo = false;
                        realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
                        continue;
                    }
                    realCts = null;
                    lastSucceeded = true;
                    lastResult = result;
                    hasSucceeded = true;
                    running = false; idleEvent.Set();
                    Progress?.Finish();
                }
                Logger.Info($"{creatorMemberName}：运行成功");
                Succeeded?.Invoke();
            } catch (Exception ex) {
                if (!ex.IsCanceled())
                    Logger.Log(ex, $"{creatorMemberName}：运行失败{(pendingRedo ? "，但即将重启，或可忽略" : "")}", 
                        pendingRedo ? LogLevel.Warn : LogLevel.Error, LogBehavior.ToastIfDebug);
                lock (this) {
                    realCts?.Dispose();
                    if (pendingRedo) { // 接取重启请求
                        Logger.Info($"{creatorMemberName}：重启开始");
                        pendingRedo = false;
                        realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
                        continue;
                    }
                    realCts = null;
                    lastSucceeded = false;
                    running = false; idleEvent.Set();
                    Progress?.Skip();
                }
                if (ex.IsCanceled()) {
                    Logger.Info($"{creatorMemberName}：运行已取消");
                    Canceled?.Invoke();
                } else {
                    Failed?.Invoke(ex);
                }
            }
            break;
        }
        Stopped?.Invoke();
    }

    /// <inheritdoc/>
    public void Cancel() {
        lock (this) {
            pendingRedo = false;
            Logger.Info($"{creatorMemberName}：接到取消请求");
            realCts?.Cancel();
        }
    }

    /// <inheritdoc/>
    public bool WaitIfRunning(int millisecondsTimeout = -1, CancellationToken cancellationToken = default)
        => idleEvent.Wait(millisecondsTimeout, cancellationToken);
    /// <inheritdoc/>
    public async Task<bool> WaitIfRunningAsync(int millisecondsTimeout = -1, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitHandle = ThreadPool.RegisterWaitForSingleObject(idleEvent.WaitHandle,
            (_, timedOut) => completionSource.TrySetResult(!timedOut), null, millisecondsTimeout, executeOnlyOnce: true);
        using var cancellationRegistration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        try {
            return await completionSource.Task.NoCapture();
        } finally {
            waitHandle.Unregister(null);
        }
    }

}

/// <inheritdoc />
public class RedoableWorker<TOut> : RedoableWorkerBase<TOut> {
    public RedoableWorker(Func<CancellationToken?, ProgressProvider?, TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base(workload, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Func<CancellationToken?, TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, _) => workload(c), creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Func<TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base((_, _) => workload(), creatorMemberName: creatorMemberName) { }
}

/// <inheritdoc />
public class RedoableWorker : RedoableWorkerBase<NoType?> {
    public RedoableWorker(Action<CancellationToken?, ProgressProvider?> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, p) => { workload(c, p); return null; }, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Action<CancellationToken?> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, _) => { workload(c); return null; }, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Action workload, [CallerMemberName] string creatorMemberName = "")
        : base((_, _) => { workload(); return null; }, creatorMemberName: creatorMemberName) { }
}

/// <summary>指示没有该类型，用于泛型参数。</summary>
public class NoType { }
