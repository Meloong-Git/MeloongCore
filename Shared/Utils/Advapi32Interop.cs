using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MeloongCore.Utils;

public class Advapi32Interop {

    #region "常量声明"

    /// <summary>
    /// HKEY_CLASSES_ROOT
    /// </summary>
    public const uint HKeyClassesRoot = 0x80000000;
    /// <summary>
    /// HKEY_CURRENT_USER
    /// </summary>
    public const uint HKeyCurrentUser = 0x80000001;
    /// <summary>
    /// HKEY_LOCAL_MACHINE
    /// </summary>
    public const uint HKeyLocalMachine = 0x80000002;
    /// <summary>
    /// HKEY_USERS
    /// </summary>
    public const uint HKeyUsers = 0x80000003;
    /// <summary>
    /// HKeyCurrentConfig
    /// </summary>
    public const uint HKeyCurrentConfig = 0x80000005;

    /// <summary>
    /// 通知
    /// </summary>
    public const uint KeyNotify = 0x0010;           
    /// <summary>
    /// 读取
    /// </summary>
    public const uint KeyRead = 0x20019;       
    /// <summary>
    /// 写入
    /// </summary>
    public const uint KeyWrite = 0x20006;           
    /// <summary>
    /// 完全访问
    /// </summary>
    public const uint KeyAllAccess = 0xF003F;

    private const uint RegNotifyChangeLastSet = 0x00000004;

    #endregion

    #region "Win32 API 声明"

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern int RegNotifyChangeKeyValue(IntPtr handle,
        bool bWatchSubtree,
        uint dwNotifyFilter,
        SafeWaitHandle hEvent,
        bool fAsynchronous
        );

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern long RegOpenKeyEx(
        IntPtr hKey,
        string lpSubKey,
        int ulOptions,
        uint samDesired,
        out SafeRegistryHandle phkResult
        );

    [DllImport("advapi32.dll")]
    internal static extern void RegCloseKey(IntPtr ptr);

    #endregion

    /// <summary>
    /// 对一个注册表项目进行异步监听
    /// </summary>
    /// <param name="keyRoot"></param>
    /// <param name="registryKeyPath"></param>
    public static void NotifyValueChange(
        uint keyRoot ,
        string registryKeyPath, 
        WaitOrTimerCallback onEventEmittedCallback,
        out EventWaitHandle eventHandle,
        out RegisteredWaitHandle waitHandle) {
        
        if (RegOpenKeyEx(new IntPtr(keyRoot), registryKeyPath, 0, KeyNotify, out var result) != 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        
        eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        // 使用 fAsynchronous = true 进行异步监听
        if (RegNotifyChangeKeyValue(
            result.DangerousGetHandle(),
            true,
            RegNotifyChangeLastSet,
            eventHandle.SafeWaitHandle, true) != 0) throw new Win32Exception(Marshal.GetLastWin32Error());

        waitHandle = ThreadPool.RegisterWaitForSingleObject(
            eventHandle,
            onEventEmittedCallback,
            null,
            Timeout.Infinite,
            true);

    }


}