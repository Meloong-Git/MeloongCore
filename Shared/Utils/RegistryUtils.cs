using Microsoft.Win32;
namespace MeloongCore;

public static class RegistryUtils {
    /// <summary>在注册表中表示 <c>null</c> 的标记字符串。</summary>
    public const string NULL_STRING = "__NULL__";

    /// <summary>
    /// 读取注册表键。
    /// <para/>若键值为 <see cref="NULL_STRING"/>，则返回 <c>null</c>。
    /// </summary>
    public static (object? result, bool exists) Read(string keyPath) {
        var (hive, subKeyPath, valueName) = ParsePath(keyPath);
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath);
        var result = subKey?.GetValue(valueName);
        if (result is null) return (null, false);
        return (result is string str && str == NULL_STRING ? null : result, true);
    }
    /// <summary>
    /// 读取注册表键，若该键不存在或发生异常则返回 <paramref name="defaultValue"/>。
    /// <para/>若键值为 <see cref="NULL_STRING"/>，则返回 <c>null</c>。
    /// </summary>
    public static object? TryRead(string keyPath, object? defaultValue = null) {
        try {
            var (result, exists) = Read(keyPath);
            return exists ? result : defaultValue;
        } catch (Exception ex) {
            Logger.Warn(ex, $"读取注册表键失败（{keyPath}）");
            return defaultValue;
        }
    }

    /// <summary>
    /// 写入注册表键。
    /// <para/>会自动创建对应的子键。
    /// <para/>若 <paramref name="value"/> 为 <c>null</c>，则写入 <see cref="NULL_STRING"/>。
    /// </summary>
    public static void Write(string keyPath, object? value) {
        var (hive, subKeyPath, valueName) = ParsePath(keyPath);
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using RegistryKey subKey = baseKey.CreateSubKey(subKeyPath);
        subKey.SetValue(valueName, value ?? NULL_STRING);
    }

    /// <summary>
    /// 判断对应的注册表键是否存在。
    /// </summary>
    public static bool Has(string keyPath) {
        var (hive, subKeyPath, valueName) = ParsePath(keyPath);
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath);
        if (subKey is null) return false;
        object notFound = new();
        return !ReferenceEquals(subKey.GetValue(valueName, notFound), notFound);
    }

    /// <summary>
    /// 删除注册表键。若不存在则不执行操作。
    /// </summary>
    public static void Delete(string keyPath) {
        var (hive, subKeyPath, valueName) = ParsePath(keyPath);
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath, writable: true);
        subKey?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    private static (RegistryHive Hive, string SubKeyPath, string ValueName) ParsePath(string fullPath) {
        int hiveEnd = fullPath.IndexOf('\\');
        int valueStart = fullPath.LastIndexOf('\\');
        if (hiveEnd <= 0 || hiveEnd > valueStart) throw new ArgumentException($"注册表路径不完整（{fullPath}）", nameof(fullPath));
        RegistryHive hive = fullPath.Substring(0, hiveEnd).ToUpperInvariant() switch {
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException($"注册表路径有误（{fullPath}）", nameof(fullPath))
        };
        string subKeyPath = valueStart == hiveEnd ? "" : fullPath.Substring(hiveEnd + 1, valueStart - hiveEnd - 1);
        return (hive, subKeyPath, fullPath.Substring(valueStart + 1));
    }

}
