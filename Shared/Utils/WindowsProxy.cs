using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace MeloongCore.Utils;

/// <summary>
/// 适用于 Windows 平台的代理实现
/// </summary>
public class WindowsProxy : IWebProxy, IDisposable {

    public WindowsProxy() : this(ProxyWorkingMode.AutoDiscover) { }

    public WindowsProxy(ProxyWorkingMode mode) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
           throw new PlatformNotSupportedException("此实现不适用于当前平台");

        ProxyChanged += _ChangeProxy;

        Advapi32Interop.NotifyValueChange(
            Advapi32Interop.HKeyCurrentUser,
            "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
            (_, _) => _HandleProxyChange(),
            out _handle,
            out _regHandle);
        _mode = mode;
        if(mode == ProxyWorkingMode.AutoDiscover)
            _HandleProxyChange();
    }

    private readonly object _syncLock = new();

    private WebProxy? _proxy;

    // 更换实现，允许手动指定代理

    private ProxyWorkingMode _mode;

    private EventWaitHandle? _handle;

    private RegisteredWaitHandle? _regHandle;

    private bool _disposed;

    #region "事件"

    public event Action<string, string[]>? ProxyChanged;

    public event Action<string>? AccessLog;
        
    #endregion

    #region "IWebProxy 实现"

    public Uri GetProxy(Uri destination) => _proxy?.GetProxy(destination) ?? destination;

    public bool IsBypassed(Uri host) => Disable || (_proxy?.IsBypassed(host) ?? true);

    public ICredentials? Credentials { 
        get => _proxy?.Credentials; 
        set
        {
            if (_proxy is null) return;
            _proxy.Credentials = value;
        } 
    }

    #endregion

    #region "必要的工作属性"

    public ProxyWorkingMode Mode {
        get => _mode;
        set {
            if (value == ProxyWorkingMode.AutoDiscover && _mode == ProxyWorkingMode.Manual) _HandleProxyChange();
            _mode = value;
        }
    }

    public bool Disable { get; set; }

    #endregion

    #region "工具函数"

    /// <summary>
    /// 手动指定一个代理
    /// </summary>
    /// <param name="proxyServer"></param>
    /// <param name="bypassed"></param>
    public void RefreshOnce(string proxyServer, string[] bypassed) => _ChangeProxy(proxyServer, bypassed);
        

    /// <summary>
    /// 切换代理
    /// </summary>
    /// <param name="proxyAddress">代理服务器地址</param>
    /// <param name="bypassed">需要跳过的服务器地址</param>
    private void _ChangeProxy(string proxyAddress, string[] bypassed) {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowsProxy));
        lock (_syncLock) {
            // 视作禁用代理
            if (string.IsNullOrEmpty(proxyAddress)) {
                Disable = true;
                return;
            }
            if (_proxy?.Address == new Uri(proxyAddress)) return;
            _proxy = new WebProxy(proxyAddress, true, bypassed);
        }
    }

    /// <summary>
    /// 触发一次代理刷新
    /// </summary>
    private void _HandleProxyChange() {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowsProxy));
        _handle?.Reset();
        if (Mode == ProxyWorkingMode.Manual) return;

        if (Advapi32Interop.RegOpenKeyEx(
            new IntPtr(Advapi32Interop.HKeyCurrentUser),
            "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
            0, Advapi32Interop.KeyRead, out var registryHandle) != 0) {

            AccessLog?.Invoke($"打开注册表失败，错误代码：{Marshal.GetLastWin32Error()}");
            return;
        }

        using var proxyHandle = registryHandle;
        using var key = RegistryKey.FromHandle(proxyHandle);
        var enableProxy = key.GetValue("ProxyEnable");
        if(enableProxy is not null && Convert.ToInt32(enableProxy) == 0) {
            Disable = true;
            return;
        }

        var proxyServer = Convert.ToString(key.GetValue("ProxyServer"));
        if (string.IsNullOrEmpty(proxyServer)) Disable = true;
        var bypassList = Convert.ToString(key.GetValue("ProxyOverride")).Split(";");
        // http=www.exmaple.com;socks=www.example.org
        // 127.0.0.1:7890
        // 过滤不使用 http 协议的代理地址（因为 ServicePointManager 只支持这个）
        var proxy = proxyServer.Split(";").Where(w => w.Contains("http=") || !w.Contains("=")).Select(w => "http://" + w.Split("=").Last())
            .FirstOrDefault();
        ProxyChanged?.Invoke(proxy, bypassList);
        Disable = false;
        
    }

    #endregion

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        _regHandle?.Unregister(null);
        _handle?.Dispose();
    }

}

public enum ProxyWorkingMode {
    /// <summary>
    /// 自动发现
    /// </summary>
    AutoDiscover,
    /// <summary>
    /// 手动
    /// </summary>
    Manual
}