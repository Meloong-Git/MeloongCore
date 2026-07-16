using System.Diagnostics;

namespace MeloongCore;

/// <summary>
/// 日志等级。
/// </summary>
public enum LogLevel { Trace, Info, Warn, Error }

/// <summary>
/// 输出日志时执行的错误汇报行为。
/// </summary>
public enum LogBehavior {
    /// <summary>
    /// 无提示。
    /// </summary>
    None,
    /// <summary>
    /// 只在调试状态下给出错误提示信息。
    /// </summary>
    ToastIfDebug,
    /// <summary>
    /// 给出错误提示信息。
    /// </summary>
    Toast,
    /// <summary>
    /// 弹出错误弹窗，不要求反馈。
    /// </summary>
    Alert,
    /// <summary>
    /// 弹出错误弹窗，要求提交反馈。
    /// </summary>
    AlertThenFeedback,
    /// <summary>
    /// 弹出系统原生样式的错误弹窗，要求提交反馈，然后使程序崩溃。
    /// </summary>
    AlertThenCrash
}

public static class Logger {
    public static BaseLogger Instance { get; set; } = new();

    // 转发给 BaseLogger 实例的对外方法

    public static void Trace(string message, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(null, message, LogLevel.Trace, behavior, filePath);
    public static void Info(string message, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(null, message, LogLevel.Info, behavior, filePath);
    public static void Warn(string message, LogBehavior behavior = LogBehavior.ToastIfDebug, [CallerFilePath] string filePath = "")
        => Log(null, message, LogLevel.Warn, behavior, filePath);
    public static void Error(string message, LogBehavior behavior = LogBehavior.AlertThenFeedback, [CallerFilePath] string filePath = "")
        => Log(null, message, LogLevel.Error, behavior, filePath);
    public static void Log(string message, LogLevel level = LogLevel.Info, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(null, message, level, behavior, filePath);

    public static void Trace(Exception ex, string? message = null, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(ex, message, LogLevel.Trace, behavior, filePath);
    public static void Info(Exception ex, string? message = null, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(ex, message, LogLevel.Info, behavior, filePath);
    public static void Warn(Exception ex, string? message = null, LogBehavior behavior = LogBehavior.ToastIfDebug, [CallerFilePath] string filePath = "")
        => Log(ex, message, LogLevel.Warn, behavior, filePath);
    public static void Error(Exception ex, string? message = null, LogBehavior behavior = LogBehavior.AlertThenFeedback, [CallerFilePath] string filePath = "")
        => Log(ex, message, LogLevel.Error, behavior, filePath);
    public static void Log(Exception? ex, string? message = null, LogLevel level = LogLevel.Warn, LogBehavior behavior = LogBehavior.ToastIfDebug, [CallerFilePath] string filePath = "")
        => Instance.Log(ex, message, level, behavior, filePath);

    /// <summary>
    /// 在不调用 Logger 的情况下，将信息直接发送到输出窗口。
    /// </summary>
    [Conditional("DEBUG")]
    public static void SendToDebug(string message, LogLevel level = LogLevel.Error, [CallerFilePath] string filePath = "")
        => Debug.WriteLine($"{BaseLogger.GetLogPrefix(level, filePath)}{message}");
    /// <summary>
    /// 在不调用 Logger 的情况下，将信息直接发送到输出窗口。
    /// </summary>
    [Conditional("DEBUG")]
    public static void SendToDebug(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerFilePath] string filePath = "")
        => Debug.WriteLine($"{BaseLogger.GetLogPrefix(level, filePath)}{(message is null ? "" : $"{message}：")}{ex.GetDisplay(true)}");

    /// <summary>
    /// 记录当前堆栈信息。
    /// </summary>
    public static void StackTrace(LogLevel level = LogLevel.Trace, [CallerFilePath] string filePath = "")
        => Instance.Log(null, new StackTrace(skipFrames: 1, fNeedFileInfo: true).ToString(), level, LogBehavior.None, filePath);

    #region 内插优化

    // VB.NET：由于不兼容 InterpolatedStringHandler，只能手动使用 Lambda

    public static void Log(Func<string?> messageFactory, LogLevel level = LogLevel.Info, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "") {
        if (Instance.MinLevel > level) return;
        Instance.Log(null, messageFactory() ?? string.Empty, level, behavior, filePath);
    }
    public static void Trace(Func<string?> messageFactory, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(messageFactory, LogLevel.Trace, behavior, filePath);
    public static void Info(Func<string?> messageFactory, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "")
        => Log(messageFactory, LogLevel.Info, behavior, filePath);
    public static void Warn(Func<string?> messageFactory, LogBehavior behavior = LogBehavior.ToastIfDebug, [CallerFilePath] string filePath = "")
        => Log(messageFactory, LogLevel.Warn, behavior, filePath);
    public static void Error(Func<string?> messageFactory, LogBehavior behavior = LogBehavior.AlertThenFeedback, [CallerFilePath] string filePath = "")
        => Log(messageFactory, LogLevel.Error, behavior, filePath);

    // C#：仅当实际需要输出日志时，才格式化内插字符串，以优化性能

    public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] ref LogInterpolatedStringHandler handler, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "") {
        if (handler.isEnabled) Instance.Log(null, handler.ToStringAndClear(), level, behavior, filePath);
    }
    public static void Trace(ref LogInterpolatedStringHandler<TraceLogLevel> handler, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "") {
        if (handler.IsEnabled) Instance.Log(null, handler.ToStringAndClear(), LogLevel.Trace, behavior, filePath);
    }
    public static void Info(ref LogInterpolatedStringHandler<InfoLogLevel> handler, LogBehavior behavior = LogBehavior.None, [CallerFilePath] string filePath = "") {
        if (handler.IsEnabled) Instance.Log(null, handler.ToStringAndClear(), LogLevel.Info, behavior, filePath);
    }
    public static void Warn(ref LogInterpolatedStringHandler<WarnLogLevel> handler, LogBehavior behavior = LogBehavior.ToastIfDebug, [CallerFilePath] string filePath = "") {
        if (handler.IsEnabled) Instance.Log(null, handler.ToStringAndClear(), LogLevel.Warn, behavior, filePath);
    }
    public static void Error(ref LogInterpolatedStringHandler<ErrorLogLevel> handler, LogBehavior behavior = LogBehavior.AlertThenFeedback, [CallerFilePath] string filePath = "") {
        if (handler.IsEnabled) Instance.Log(null, handler.ToStringAndClear(), LogLevel.Error, behavior, filePath);
    }

    [InterpolatedStringHandler]
    public ref struct LogInterpolatedStringHandler {
        private StringBuilder? _sb;
        public readonly bool isEnabled;
        public LogInterpolatedStringHandler(int literalLength, LogLevel level, out bool isEnabled) {
            this.isEnabled = isEnabled = Logger.Instance.MinLevel <= level;
            _sb = this.isEnabled ? new StringBuilder(literalLength) : null;
        }
        public void AppendLiteral(string value) => _sb?.Append(value);
        public void AppendFormatted<T>(T value) => _sb?.Append(value);
        public void AppendFormatted<T>(T value, string? format) => _sb?.Append(value is IFormattable formattable ? formattable.ToString(format, null) : value?.ToString());
        public void AppendFormatted<T>(T value, int alignment) {
            if (_sb is null) return;
            var s = value?.ToString() ?? string.Empty;
            _sb.Append(alignment > 0 ? s.PadLeft(alignment) : alignment < 0 ? s.PadRight(-alignment) : s);
        }
        public void AppendFormatted<T>(T value, int alignment, string? format) {
            if (_sb is null) return;
            var s = value is IFormattable formattable ? formattable.ToString(format, null) : value?.ToString() ?? string.Empty;
            _sb.Append(alignment > 0 ? s.PadLeft(alignment) : alignment < 0 ? s.PadRight(-alignment) : s);
        }
        public void AppendFormatted(string? value) => _sb?.Append(value);
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) {
            if (_sb is null) return;
            var s = value is IFormattable formattable ? formattable.ToString(format, null) : value?.ToString() ?? string.Empty;
            _sb.Append(alignment > 0 ? s.PadLeft(alignment) : alignment < 0 ? s.PadRight(-alignment) : s);
        }
        internal string ToStringAndClear() {
            var result = _sb?.ToString() ?? string.Empty;
            _sb = null;
            return result;
        }
    }

    [InterpolatedStringHandler]
    public ref struct LogInterpolatedStringHandler<TLevel> where TLevel : struct, ILogLevelProvider {
        private LogInterpolatedStringHandler _inner;
        public bool IsEnabled => _inner.isEnabled;
        public LogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled) => _inner = new(literalLength, default(TLevel).Level, out isEnabled);
        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        internal string ToStringAndClear() => _inner.ToStringAndClear();
    }

    public readonly struct TraceLogLevel : ILogLevelProvider { public LogLevel Level => LogLevel.Trace; }
    public readonly struct InfoLogLevel : ILogLevelProvider { public LogLevel Level => LogLevel.Info; }
    public readonly struct WarnLogLevel : ILogLevelProvider { public LogLevel Level => LogLevel.Warn; }
    public readonly struct ErrorLogLevel : ILogLevelProvider { public LogLevel Level => LogLevel.Error; }
    public interface ILogLevelProvider { LogLevel Level { get; } }

    #endregion

}

/// <summary>
/// 最基础的日志实现。
/// 仅将日志输出到 <see cref="Debug"/>，不实现 <see cref="LogBehavior"/>。
/// </summary>
public class BaseLogger {

    /// <summary>
    /// 最低日志输出等级，低于此等级的日志将被忽略。
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    // 核心方法
    public virtual void Log(Exception? ex, string? message, LogLevel level, LogBehavior behavior, string filePath) {
        if (ex?.IsCanceled() == true) {
            Log(null, $"操作已取消（{message}）", LogLevel.Info, LogBehavior.None, filePath);
            return;
        }
        if (MinLevel > level) return;
        try {
            // 格式化文本
            var formattedMessage = ex is null ?
                (message ?? "") :
                (message is null ? "" : $"{message}：") + ex.GetDisplay(true);
            formattedMessage = Format(formattedMessage, level, filePath, ex);
            // 将格式化后的日志文本置入缓冲区
            if (available || queue.Count < 100) { // 在初始化前保留至多 100 条日志
                queue.Enqueue(new(level, formattedMessage));
                queuedEvent.Set();
            }
            // 执行事件
            HandleBehavior(message, formattedMessage, behavior, ex);
        } catch (Exception logEx) {
            Logger.SendToDebug(logEx, "打印日志失败");
        }
    }

    /// <summary>
    /// 格式化日志文本。
    /// </summary>
    public virtual string Format(string text, LogLevel level, string filePath, Exception? ex) {
        string prefix = GetLogPrefix(level, filePath);
        if (string.IsNullOrEmpty(text)) return prefix;
        return text
            .ReplaceLineEndings("\n", mergeMultiple: true).Split('\n', true)
            .Select(t => prefix + t)
            .Join("\r\n");
    }
    public static string GetLogPrefix(LogLevel level, string filePath)
        => $"{DateTime.Now:HH':'mm':'ss'.'fff} {level.ToString().First()} {
            (Thread.CurrentThread.Name is not null ? $"<{Thread.CurrentThread.ManagedThreadId} · {Thread.CurrentThread.Name}> " : 
            (Thread.CurrentThread.IsThreadPoolThread ? "<Async> " : ""))}[{
            PathUtils.GetLastPart(filePath).BeforeFirst(".")}] ";

    /// <summary>
    /// 执行 <see cref="LogBehavior"/>。
    /// </summary>
    public virtual void HandleBehavior(string? rawMessage, string formattedMessage, LogBehavior behavior, Exception? ex) {
    }

    /// <summary>
    /// 输出格式化后的日志文本。
    /// </summary>
    public virtual void Output(LogEntry entry) {
        Debug.WriteLine(entry.message);
    }

    public virtual void Init() { }

    #region 缓冲区与初始化

    internal volatile bool available = false;
    public record struct LogEntry(LogLevel Level, string message);
    internal readonly ConcurrentQueue<LogEntry> queue = new();
    internal readonly AutoResetEvent queuedEvent = new(false);

    public BaseLogger() {
        var thread = new Thread(() => {
            try {
                Init();
                available = true;
                Logger.Info($"{GetType().Name} 日志初始化结束");
                while (available) {
                    queuedEvent.WaitOne();
                    Flush();
                }
            } catch (Exception ex) {
                available = false;
                Logger.SendToDebug(ex, "基础日志线程出错");
            }
        }) { Name = nameof(Logger), Priority = ThreadPriority.Lowest, IsBackground = true };
        thread.Start();
    }

    /// <summary>
    /// 立即输出缓冲区的日志。
    /// 不会抛出异常。
    /// </summary>
    public void Flush() {
        try {
            if (!available) return;
            lock (flushLock) {
                while (queue.TryDequeue(out LogEntry entry)) Output(entry); // writer 指定了 AutoFlush
            }
        } catch (Exception logEx) {
            Logger.SendToDebug(logEx, "输出日志失败");
        }
    }
    private readonly object flushLock = new();

    #endregion

}

/// <summary>
/// 将日志输出到 <see cref="Debug"/> 并写入指定的文件夹，最多保留 5 个文件。
/// 不实现 <see cref="LogBehavior"/>。
/// </summary>
public class FileLogger : BaseLogger, IDisposable {

    public string logFolder = Path.Combine(Paths.Base, "logs");
    public string fileNamePrefix = "Log";
    public string fileNameSuffix = ".txt";
    private StreamWriter? writer;

    /// <summary>
    /// 初始化此 Logger。
    /// 会在新线程中，将之前的日志文件依次后移，最多保留 5 个文件。
    /// </summary>
    public override void Init() {
        // 轮转日志文件，将 Log1.txt 留空
        Logger.Info("开始轮转日志文件");
        try {
            for (int i = 4; i >= 1; i--) {
                string newerFile = Path.Combine(logFolder, $"{fileNamePrefix}{i + 1}{fileNameSuffix}");
                string olderFile = Path.Combine(logFolder, $"{fileNamePrefix}{i}{fileNameSuffix}");
                if (!FileUtils.Exists(olderFile)) continue;
                FileUtils.Copy(olderFile, newerFile);
                FileUtils.Delete(olderFile);
            }
        } catch (IOException ex) {
            Logger.Warn(ex, "可能同时开启了多个程序，这或许会导致未知问题", LogBehavior.Toast);
        } catch (Exception ex) {
            Logger.Warn(ex, "整理日志文件失败");
        }
        // 新建 writer
        string logFilePath = PathUtils.ForApi(Path.Combine(logFolder, $"{fileNamePrefix}1{fileNameSuffix}"));
        DirectoryUtils.Create(logFolder);
        writer = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
    }

    /// <inheritdoc/>
    public override void Output(LogEntry entry) {
        base.Output(entry);
        writer?.WriteLine(entry.message); // writer 指定了 AutoFlush
    }

    public void Dispose() {
        available = false;
        if (writer is not null) ((IDisposable) writer).Dispose();
        queuedEvent.Dispose();
        GC.SuppressFinalize(this);
    }

}
