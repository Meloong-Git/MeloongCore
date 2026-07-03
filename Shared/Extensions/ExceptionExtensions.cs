using System.Net.Sockets;

namespace MeloongCore.Extensions;
public static class ExceptionExtensions {

    /// <summary>
    /// 返回该异常最底层的 <see cref="Exception.InnerException"/>。
    /// </summary>
    public static Exception RootException(this Exception ex) {
        while (ex.InnerException is { } inner) ex = inner;
        return ex;
    }

    /// <summary>
    /// 该异常或其子异常是否为 <see cref="OperationCanceledException"/>、<see cref="TaskCanceledException"/> 或 <see cref="ThreadInterruptedException"/>。
    /// </summary>
    public static bool IsCanceled(this Exception ex) {
        foreach (var inner in ex.Flatten()) {
            if (inner is OperationCanceledException or TaskCanceledException or ThreadInterruptedException) return true;
        }
        return false;
    }
    /// <summary>
    /// 当该异常或其子异常是 <see cref="OperationCanceledException"/>、<see cref="TaskCanceledException"/> 或 <see cref="ThreadInterruptedException"/> 时，抛出对应异常。
    /// </summary>
    public static void ThrowIfCanceled(this Exception ex) {
        foreach (var inner in ex.Flatten()) {
            if (inner is OperationCanceledException or TaskCanceledException or ThreadInterruptedException) throw inner;
        }
    }

    /// <summary>
    /// 将异常所有的 <see cref="Exception.InnerException"/> 展开。
    /// 若为 <see cref="AggregateException"/>，也会展开其中所有的子异常。
    /// </summary>
    public static IEnumerable<Exception> Flatten(this Exception ex) {
        var toFlatten = new Stack<Exception>();
        toFlatten.Push(ex);
        while (toFlatten.Any()) {
            var current = toFlatten.Pop();
            yield return current;
            if (current is AggregateException ag) {
                foreach (var inner in ag.InnerExceptions) toFlatten.Push(inner);
            } else if (current.InnerException is not null) {
                toFlatten.Push(current.InnerException);
            }
        }
    }

    /// <summary>
    /// 获取 <paramref name="ex"/> 的用户友好描述。
    /// 若 <paramref name="multiline"/> 为 true，则返回多行的详细描述与堆栈信息；否则不整理堆栈，仅将 <see cref="Exception.Message"/> 汇总到一行。
    /// </summary>
    public static string GetDisplay(this Exception? ex, bool multiline) {
        if (ex is null) return "无可用错误信息！";

        // 提取堆栈信息
        var lines = new List<string>();
        bool isInnerException = false;
        static string getExceptionMessage(Exception e) => e.Message + (e switch {
            WebException webEx => $" (Status={webEx.Status})",
            SocketException socketEx => $" (SocketErrorCode={socketEx.SocketErrorCode})",
            _ => ""
        });
        foreach (var currentEx in ex.Flatten()) {
            if (multiline) {
                lines.Add((isInnerException ? "→ " : "") + getExceptionMessage(currentEx).ReplaceLineEndings("\r\n", true));
                var stackLines = (currentEx.StackTrace?.SplitLines(true) ?? [])
                    .Select(l => l.BeforeLast("(") + l.AfterLast(")"))
                    .Distinct();
                lines.AddRange(stackLines);
                if (currentEx.GetType() != typeof(Exception)) lines.Add("   错误类型：" + currentEx.GetType().FullName);
            } else {
                lines.Add(getExceptionMessage(currentEx).ReplaceLineEndings(" ", true));
            }
            isInnerException = true;
        }

        // 分析常见错误原因
        string? commonReason = null;
        var rootException = ex.RootException();
        if (rootException is TypeLoadException or BadImageFormatException or MissingMethodException or NotImplementedException or TypeInitializationException)
            commonReason = "运行环境存在问题。请尝试重新安装 .NET Framework 4.8 然后再试。若无法安装，请先卸载较新版本的 .NET Framework，然后再尝试安装。";
        else if (rootException is UnauthorizedAccessException)
            commonReason = "程序权限不足。请尝试右键程序，选择以管理员身份运行，或将文件移动到其他文件夹。";
        else if (rootException is OutOfMemoryException)
            commonReason = "系统的运行内存不足。请在关闭一部分不需要的程序后再试。";
        else if (rootException is System.Net.Sockets.SocketException && lines.Any(l => l.Contains("WSAStartup")))
            commonReason = "请尝试卸载中国移动云盘，然后再试。";
        else if (ex.IsBadNetwork())
            commonReason = "你的网络环境不佳，请稍后再试，或使用 VPN 改善网络环境。";

        // 输出
        if (multiline) {
            if (commonReason is null) {
                return lines.Join("\r\n");
            } else {
                return commonReason + "\r\n\r\n——————— 详细错误信息 ———————\r\n" + lines.Join("\r\n");
            }
        } else {
            lines = lines.Distinct().ToList();
            if (commonReason is null) {
                lines.Reverse();
                return lines.Join(" ← ");
            } else {
                return $"{commonReason}（{lines.First()}）";
            }
        }
    }

    /// <summary>
    /// 判断该异常是否是由于网络连接不良导致。
    /// </summary>
    public static bool IsBadNetwork(this Exception ex) {
        foreach (var currentEx in ex.Flatten()) {
            if (currentEx is TimeoutException) {
                return true;
            } else if (currentEx is TaskCanceledException) {
                return true;
            } else if (currentEx is HttpRequestCodeException reqEx) {
                if (reqEx.StatusCode is HttpStatusCode.Forbidden) return false;
                if (reqEx.StatusCode is HttpStatusCode.RequestTimeout) return true;
            } else if (currentEx is WebException webEx) {
                if (webEx.Response is HttpWebResponse response) {
                    if (response.StatusCode is HttpStatusCode.Forbidden) return false;
                    if (response.StatusCode is HttpStatusCode.RequestTimeout) return true;
                }
                if (webEx.Status is WebExceptionStatus.NameResolutionFailure or WebExceptionStatus.ConnectFailure or WebExceptionStatus.ReceiveFailure or 
                    WebExceptionStatus.SendFailure or WebExceptionStatus.ConnectionClosed or WebExceptionStatus.KeepAliveFailure or WebExceptionStatus.Timeout or 
                    WebExceptionStatus.ProxyNameResolutionFailure or WebExceptionStatus.SecureChannelFailure or WebExceptionStatus.TrustFailure) return true;
            } else if (currentEx is SocketException socketEx) {
                if (socketEx.SocketErrorCode is SocketError.NetworkDown or SocketError.NetworkUnreachable or SocketError.NetworkReset or SocketError.ConnectionAborted or 
                    SocketError.ConnectionReset or SocketError.TimedOut or SocketError.ConnectionRefused or SocketError.HostUnreachable or 
                    SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData) return true;
            }
        }
        return false;
    }

}
