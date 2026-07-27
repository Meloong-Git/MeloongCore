namespace MeloongCore;

/// <summary>
/// 提供一个追踪任务进度的对象，它所提供的供用户查看的进度必定单调递增。
/// 支持重试、拆分子项进度、记录部分进度被跳过。
/// </summary>
public class ProgressProvider {

    /// <summary>
    /// 当原始进度可能被改变时触发。
    /// </summary>
    public event Action<(double actual, double skiped)>? ProgressChanged;
    private event Action? ChildProgressChanged;
    private void InvokeProgressChanged() {
        ProgressChanged?.Invoke(GetRaw());
        ChildProgressChanged?.Invoke();
    }

    // ===================================== 主项进度 =====================================

    private (double actual, double skiped, double splited) progressParts = (0, 0, 0);
    private double progressSum => progressParts.actual + progressParts.skiped + progressParts.splited;

    private bool _Set(double value, bool skiped) {
        value = value.Clamp(0, 1);
        double delta = value - progressSum;
        if (delta < 0) { // 等比减少
            progressParts = (progressParts.actual * value / progressSum,
                              progressParts.skiped * value / progressSum,
                              progressParts.splited * value / progressSum);
        } else if (skiped) {
            progressParts.skiped += delta;
        } else {
            progressParts.actual += delta;
        }
        return delta != 0;
    }

    /// <summary>
    /// 将当前进度设置为指定值。
    /// 若指定了 <paramref name="skiped"/>，这段进度的增量值将被计为跳过：当前观测进度不会增加，后续观测进度将增加地更快。
    /// </summary>
    public void Set(double value, bool skiped = false, ChildrenAction action = ChildrenAction.Finished) {
        bool changed;
        lock (this) {
            changed = _Set(value, skiped);
            changed |= AssumeChildrenAs(action);
        }
        if (changed) InvokeProgressChanged();
    }
    /// <summary>
    /// 将当前进度增加指定值。
    /// 若指定了 <paramref name="skiped"/>，这段进度的增量值将被计为跳过：当前观测进度不会增加，后续观测进度将增加地更快。
    /// 若指定了 <paramref name="markChildrenAsFinished"/>，当前所有未完成的子项将被视为已完成。
    /// </summary>
    public void Add(double value, bool skiped = false, ChildrenAction action = ChildrenAction.Finished) {
        bool changed;
        lock (this) {
            changed = _Set(value + progressSum, skiped);
            changed |= AssumeChildrenAs(action);
        }
        if (changed) InvokeProgressChanged();
    }
    /// <summary>完成当前项的剩余进度。</summary>
    public void Finish(ChildrenAction action = ChildrenAction.Finished) => Set(1, skiped: false, action: action);
    /// <summary>跳过当前项的剩余进度。</summary>
    public void Skip(ChildrenAction action = ChildrenAction.Skiped) => Set(1, skiped: true, action: action);
    /// <summary>
    /// 重置当前进度，并清除所有子项。
    /// </summary>
    public void Reset() {
        bool changed;
        lock (this) {
            changed = progressSum > 0;
            progressParts = (0, 0, 0);
            childrens.ForEach(c => c.child.ChildProgressChanged -= InvokeProgressChanged);
            childrens.Clear();
        }
        if (changed) InvokeProgressChanged();
    }

    // ===================================== 子项进度 =====================================

    private readonly List<(double percentage, ProgressProvider child)> childrens = [];

    /// <summary>
    /// 拆分一个子项：当该子项完成时，进度将从当前值增加至 <paramref name="value"/>。
    /// </summary>
    public ProgressProvider SplitTo(double value) {
        lock (this) return SplitBy(value - progressSum).First();
    }
    /// <summary>
    /// 拆分多个子项，每个子项都占据对应指定的进度量。
    /// <para/>例如，调用 <c>SplitBy(0.2, 0.3)</c> 将返回两个子项，分别占据总进度的 20%、30%。
    /// <para/>若总进度量 > 1，子项的进度量将相对缩小。
    /// </summary>
    public List<ProgressProvider> SplitBy(params double[] percentages) {
        lock (this) {
            if (percentages.Any(p => p <= 0)) throw new ArgumentException("子项进度必须为正数且");
            progressParts.splited += (percentages.Sum() + progressSum).Clamp(0, 1) - progressSum;
            return percentages.Select(percentage => {
                var sub = new ProgressProvider();
                sub.ChildProgressChanged += InvokeProgressChanged;
                childrens.Add((percentage, sub));
                return sub;
            }).ToList();
        }
    }

    public enum ChildrenAction {
        /// <summary>不做任何处理。</summary>
        None,
        /// <summary>将未完成的子项的剩余进度视为已完成。</summary>
        Finished,
        /// <summary>将未完成的子项的剩余进度视为已跳过。</summary>
        Skiped
    }
    /// <summary>
    /// 修改所有未完成的子项。
    /// </summary>
    /// <returns>是否修改了任意进度不到 1 的子项。</returns>
    private bool AssumeChildrenAs(ChildrenAction action) {
        if (action == ChildrenAction.None) return false;
        lock (this) {
            bool changed = false;
            if (childrens.Any(c => c.percentage > 0)) { // 将子项的进度加入主项
                var mult = progressParts.splited / childrens.Sum(c => c.percentage);
                childrens.ForEach(c => {
                    var (subActual, subSkiped) = c.child.GetRaw();
                    progressParts.actual += mult * c.percentage * (action == ChildrenAction.Finished ? (1 - subSkiped) : subActual);
                    progressParts.skiped += mult * c.percentage * (action == ChildrenAction.Skiped ? (1 - subActual) : subSkiped);
                    c.child.ChildProgressChanged -= InvokeProgressChanged;
                    if (subActual + subSkiped < 0.9999999) changed = true;
                });
            }
            progressParts.splited = 0;
            childrens.Clear();
            return changed;
        }
    }

    // ===================================== 观测 =====================================

    /// <summary>
    /// 获取包含子项进度的实际总进度值。
    /// </summary>
    public (double actual, double skiped) GetRaw() {
        lock (this) {
            (double actual, double skiped) current = (progressParts.actual, progressParts.skiped);
            if (childrens.Any(c => c.percentage > 0)) { // 将子项的进度加入主项
                var mult = progressParts.splited / childrens.Sum(c => c.percentage);
                childrens.ForEach(c => {
                    var (subActual, subSkiped) = c.child.GetRaw();
                    current.actual += mult * c.percentage * subActual;
                    current.skiped += mult * c.percentage * subSkiped;
                });
            }
            return current;
        }
    }

    public bool IsFinished => GetRaw().actual + GetRaw().skiped > 0.9999999;

    private (double actual, double skiped) observedProgress = (0, 0);
    private double incrementProgress = 0;
    /// <summary>
    /// 获取一个在多次观测之间必定单调递增的进度值，范围为 [0, 1]。
    /// </summary>
    public double GetIncreasing() {
        lock (this) {
            (double actual, double skiped) current = GetRaw();
            if (current == observedProgress) return incrementProgress; // 未改变
            if (IsFinished) { // 已完成
                incrementProgress = 1;
            } else if (current.actual >= observedProgress.actual) { // 进度增加
                incrementProgress += (1 - incrementProgress) *
                    (current.actual - observedProgress.actual) / (1 - observedProgress.actual - current.skiped);
            }
            observedProgress = current;
            return incrementProgress;
        }
    }

}

/// <summary>
/// 监听进度变化，并将观测到的进度节流后传给更新方法。
/// </summary>
public class ProgressObserver : RateLimitedWorker {
    private readonly ProgressProvider provider;
    public ProgressObserver(ProgressProvider provider, Action<double> updateAction, double minimalIntervalMs = 70)
        : base(() => Observe(provider, updateAction), minimalIntervalMs, RateLimitMode.ImmediateThenMerge) {
        this.provider = provider;
        provider.ProgressChanged += StartObserver;
    }
    public override void Dispose() {
        provider.ProgressChanged -= StartObserver;
        base.Dispose();
    }
    private void StartObserver((double actual, double skiped) raw) => Start();
    private static void Observe(ProgressProvider provider, Action<double> updateAction) {
        double progress = provider.GetIncreasing();
        // TODO: 添加平缓过渡
        updateAction(progress);
    }

}
