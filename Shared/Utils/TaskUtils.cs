using System.Diagnostics;

namespace MeloongCore;
public static class TaskUtils {

    /// <summary>
    /// 并行执行所有任务。
    /// </summary>
    public static Task WhenAll(IEnumerable<Func<ProgressProvider?, Task>> tasks, ProgressProvider? progress = null) {
        var taskList = tasks.ToList();
        if (!taskList.Any()) {
            progress?.Skip();
            return Task.CompletedTask;
        }
        // 实际分割
        var progressEach = 1d / taskList.Count;
        return Task.WhenAll(taskList.Select(async (task, _) => {
            var cp = progress?.SplitBy(progressEach)[0];
            await task(cp).NoCapture();
            cp?.Finish();
        }));
    }
    /// <summary>
    /// 并行执行所有任务并返回结果。
    /// </summary>
    public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Func<ProgressProvider?, Task<TResult>>> tasks, ProgressProvider? progress = null) {
        var taskList = tasks.ToList();
        if (!taskList.Any()) {
            progress?.Skip();
            return Task.FromResult(Array.Empty<TResult>());
        }
        // 实际分割
        var progressEach = 1d / taskList.Count;
        return Task.WhenAll(taskList.Select(async (task, _) => {
            var cp = progress?.SplitBy(progressEach)[0];
            var result = await task(cp).NoCapture();
            cp?.Finish();
            return result;
        }));
    }
    /// <summary>
    /// 并行执行所有任务。
    /// </summary>
    public static Task WhenAll(IEnumerable<Task> tasks, ProgressProvider? progress = null)
        => WhenAll(tasks.Select(task => new Func<ProgressProvider?, Task>(_ => task)), progress);
    /// <summary>
    /// 并行执行所有任务并返回结果。
    /// </summary>
    public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks, ProgressProvider? progress = null)
        => WhenAll(tasks.Select(task => new Func<ProgressProvider?, Task<TResult>>(_ => task)), progress);

    public static void ForEach<T>(IEnumerable<T> source, Action<T> body)
        => Parallel.ForEach(source, body);

    /// <summary>
    /// 静默运行程序并等待其结束，返回其输出和退出码。
    /// 支持 notepad、git 等命令。
    /// 超时或启动失败会抛出异常。
    /// </summary>
    public static async Task<(string Output, int ExitCode)> RunProgramAsync(string file, string arguments = "", int? timeoutMs = null, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new ProcessStartInfo {
            Arguments = arguments,
            FileName = PathUtils.ToShortPath(file),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true, StandardOutputEncoding = encoding,
            RedirectStandardError = true, StandardErrorEncoding = encoding
        };
        using var program = new Process();
        program.StartInfo = info;
        program.EnableRaisingEvents = true;
        if (!program.Start()) throw new InvalidOperationException($"运行程序时出现意外错误：{file} {arguments}");
        bool hasTimeout = timeoutMs is > 0;
        Logger.Info($"运行程序，并返回其输出：{file} {arguments}{(hasTimeout ? $"，最长可等待 {timeoutMs}ms" : "")}");

        // 输出和错误流
        var outputTask = program.StandardOutput.ReadToEndAsync();
        var errorTask = program.StandardError.ReadToEndAsync();

        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        program.Exited += (_, _) => completionSource.TrySetResult(null);
        if (program.HasExited) completionSource.TrySetResult(null);
        var task = completionSource.Task;
        var timeoutTask = Task.Delay(hasTimeout ? timeoutMs!.Value : Timeout.Infinite);
        var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
        Logger.Trace($"等待程序 {program.Id} 完成");
        var completedTask = await Task.WhenAny(task, timeoutTask, cancelTask).NoCapture();
        if (completedTask != task) {
            try {
                if (!program.HasExited) program.Kill();
            } catch (InvalidOperationException) { // 进程已退出，无需处理
            }
            if (completedTask == cancelTask) throw new OperationCanceledException(cancellationToken);
            throw new TimeoutException($"运行程序超时：{file} {arguments}");
        }
        return (await outputTask.NoCapture() + await errorTask.NoCapture(), program.ExitCode);
    }

}
