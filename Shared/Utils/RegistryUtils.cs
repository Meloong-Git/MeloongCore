using Microsoft.Win32;
namespace MeloongCore;

public static class RegistryUtils {
    /// <summary>在注册表中表示 <c>null</c> 的标记字符串。</summary>
    public const string NULL_STRING = "__NULL__";

    /// <summary>
    /// 读取注册表值。
    /// <para/>若键值为 <see cref="NULL_STRING"/>，则返回 <c>null</c>。
    /// </summary>
    public static (object? result, bool exists) Read(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder);
        var result = folderKey?.GetValue(entry);
        if (result is null) return (null, false);
        return (result is string str && str == NULL_STRING ? null : result, true);
    }
    /// <summary>
    /// 读取注册表值，若该值不存在或发生异常则返回 <paramref name="defaultValue"/>。
    /// <para/>若键值为 <see cref="NULL_STRING"/>，则返回 <c>null</c>。
    /// </summary>
    public static object? TryRead(string folder, string entry, object? defaultValue = null) {
        try {
            var (result, exists) = Read(folder, entry);
            return exists ? result : defaultValue;
        } catch (Exception ex) {
            Logger.Warn(ex, $"读取注册表值失败（{folder}，{entry}）");
            return defaultValue;
        }
    }

    /// <summary>
    /// 写入注册表值。
    /// <para/>会自动创建对应的子键。
    /// <para/>若 <paramref name="value"/> 为 <c>null</c>，则写入 <see cref="NULL_STRING"/>。
    /// </summary>
    public static void Write(string folder, string entry, object? value) {
        using RegistryKey folderKey = GetRegistryKey(folder, create: true)!;
        folderKey.SetValue(entry, value ?? NULL_STRING);
    }

    /// <summary>
    /// 判断对应的注册表值是否存在。
    /// </summary>
    public static bool Has(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder);
        if (folderKey is null) return false;
        object notFound = new();
        return !ReferenceEquals(folderKey.GetValue(entry, notFound), notFound);
    }

    /// <summary>
    /// 删除注册表值。若不存在则不执行操作。
    /// </summary>
    public static void Delete(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder, writable: true);
        folderKey?.DeleteValue(entry, throwOnMissingValue: false);
    }

    private static RegistryKey? GetRegistryKey(string fullPath, bool writable = false, bool create = false) {
        using RegistryKey key = fullPath.BeforeFirst("\\").ToUpperInvariant() switch {
            "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_USERS" => Registry.Users,
            "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => throw new ArgumentException($"注册表路径有误（{fullPath}）", nameof(fullPath))
        };
        string subKey = fullPath.AfterFirst("\\");
        return create ? key.CreateSubKey(subKey) : key.OpenSubKey(subKey, writable);
    }

}
