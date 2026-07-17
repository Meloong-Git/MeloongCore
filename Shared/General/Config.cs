using System.Security.Cryptography;
namespace MeloongCore;

#region ConfigProvider | 配置提供器

public interface IConfigProvider {
    void Set<T>(string key, T? value, bool encrypted);
    void Remove(string key);
    T? Read<T>(string key, T? defaultValue, bool encrypted);
    bool HasValue(string key);
    void DiscardCache();
}

public class JsonConfigProvider : IConfigProvider {
    public static readonly ConcurrentBag<WeakReference<JsonConfigProvider>> allProviders = [];
    private readonly string filePath;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。
    public JsonConfigProvider(string filePath) {
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。
        this.filePath = filePath;
        allProviders.Add(new(this));
        saveWorker = new(Save, 500, RateLimitMode.WaitUnillNoNewInvoke);
        InitJson();
    }

    // 原始文件缓存
    private Lazy<JObject> json;
    // 反序列化后的结果缓存
    private readonly ConcurrentDictionary<string, object?> cache = new();
    private void InitJson() {
        json = new(() => {
            try {
                return FileUtils.Exists(filePath) ? ((JObject?) FileUtils.ReadAsJson(filePath) ?? []) : [];
            } catch (Exception ex) {
                Logger.Error(ex, $"读取配置文件失败（{filePath}）", LogBehavior.Alert);
                return [];
            }
        });
    }

    // ================================= 设置、读取、缓存 =================================

    public void Set<T>(string key, T? value, bool encrypted) {
        cache[key] = value;
        lock (this) {
            if (value is null) {
                // 直接在 JSON 中表示为 null
                json.Value[key] = JValue.CreateNull();
                Logger.Trace($"配置已修改：{key} = null（{filePath}）");
            } else if (encrypted) {
                // 需要加密，在 JSON 中保存密文字符串
                throw new NotImplementedException("加密需要改为使用识别码，现在尚未实现。");
                // Logger.Trace($"配置已修改：{key} = 已加密（{filePath}）");
                // json.Value[key] = CryptographyUtils.AesEncrypt(value is string str ? str : JsonConvert.SerializeObject(value));
            } else {
                // 用 JToken 保留原始结构
                JToken token = JToken.FromObject(value);
                json.Value[key] = token;
                Logger.Trace($"配置已修改：{key} = {token.ToString(Formatting.None)}（{filePath}）");
            }
        }
        MakeDirty();
    }
    public T? Read<T>(string key, T? defaultValue, bool encrypted) {
        if (cache.TryGetValue(key, out var cachedValue)) return (T?) cachedValue; // 读取缓存
        JToken entry;
        lock (this) {
            if (!json.Value.TryGetValue(key, out entry!)) return defaultValue; // 未找到该键
            entry = entry.DeepClone();
        }
        try {
            T? result;
            if (entry.Type is JTokenType.Null or JTokenType.Undefined) {
                result = default;
            } else if (encrypted) {
                string plainText = CryptographyUtils.AesDecrypt(entry.Value<string>())!;
                result = typeof(T) == typeof(string) ? (T) (object) plainText : JsonConvert.DeserializeObject<T>(plainText);
            } else {
                result = entry.ToObject<T>();
            }
            cache[key] = result;
            return result;
        } catch (CryptographicException ex) {
            Logger.Error(ex, $"解密配置失败，该配置将被重置（{key}）", LogBehavior.Alert);
            Remove(key);
            return defaultValue;
        }
    }

    public bool HasValue(string key) {
        lock (this) return json.Value.ContainsKey(key);
    }
    public void Remove(string key) {
        lock (this) json.Value.Remove(key);
        cache.TryRemove(key, out _);
        MakeDirty();
    }
    public void DiscardCache() {
        lock (this) InitJson();
        cache.Clear();
    }

    // ===================================== 保存 =====================================

    private readonly RateLimitedWorker saveWorker;
    private bool isDirty = false;
    public void MakeDirty() {
        isDirty = true;
        saveWorker.Start();
    }
    public void Save() {
        if (!isDirty) return;
        isDirty = false;
        lock (this) FileUtils.Write(filePath, json.Value.ToString(Formatting.Indented));
    }
}

public static class ConfigProviders {
    public static JsonConfigProvider Default = new(Path.Combine(Paths.AppDataThenName, "config.json"));
    public static readonly JsonConfigProvider Secret = new(Path.Combine(Paths.AppData, "secret.json"));
}

#endregion

#region ConfigEntry | 单个配置项

/// <summary>
/// 使用固定的 <see cref="IConfigProvider"/> 的配置项。
/// <para/>若不指定 <paramref name="provider"/>，则使用 <see cref="ConfigProviders.Default"/>。
/// </summary>
public class ConfigEntry<T>(string key, T? defaultValue, bool encrypted = false, IConfigProvider? provider = null) {
    public IConfigProvider Provider { get; } = provider ?? ConfigProviders.Default;

    /// <summary>
    /// 当调整配置项的值时触发，参数为新值。<para/>新值可能和旧值相同。
    /// </summary>
    public event Action<T?, IConfigProvider>? Changed;

    public void Edit(Func<T?, T?> editFunc) => Set(editFunc(Get()));
    public void Edit(RefAction editAction) {
        var current = Get();
        editAction(ref current);
        Set(current);
    }
    public delegate void RefAction(ref T? value);

    public void Set(T? value) {
        Provider.Set(key, value, encrypted);
        Changed?.Invoke(value, Provider);
    }
    public void Reset() {
        if (!Provider.HasValue(key)) return; // 值未设置，无需重置
        Provider.Remove(key);
        Changed?.Invoke(defaultValue, Provider);
    }
    public T? Get() => Provider.Read(key, defaultValue, encrypted);
    public bool HasValue() => Provider.HasValue(key);
}

/// <summary>
/// 在每次操作时动态指定 <see cref="IConfigProvider"/> 的配置项。
/// </summary>
public class DynamicConfigEntry<T>(string key, T? defaultValue, bool encrypted = false) {
    /// <summary>
    /// 当调整配置项的值时触发，参数为新值。<para/>新值可能和旧值相同。
    /// </summary>
    public event Action<T?, IConfigProvider>? Changed;

    public void Edit(Func<T?, T?> editFunc, IConfigProvider provider) => Set(editFunc(Get(provider)), provider);
    public void Edit(RefAction editAction, IConfigProvider provider) {
        var current = Get(provider);
        editAction(ref current);
        Set(current, provider);
    }
    public delegate void RefAction(ref T? value);

    public void Set(T? value, IConfigProvider provider) {
        provider.Set(key, value, encrypted);
        Changed?.Invoke(value, provider);
    }
    public void Reset(IConfigProvider provider) {
        if (!provider.HasValue(key)) return; // 值未设置，无需重置
        provider.Remove(key);
        Changed?.Invoke(defaultValue, provider);
    }
    public T? Get(IConfigProvider provider) => provider.Read(key, defaultValue, encrypted);
    public bool HasValue(IConfigProvider provider) => provider.HasValue(key);
}

#endregion

public static class ConfigUtils {

    /// <summary>立即保存所有配置。</summary>
    public static void SaveAll() {
        foreach (var weakRef in JsonConfigProvider.allProviders) {
            if (weakRef.TryGetTarget(out var provider)) provider.Save();
        }
    }

    /// <summary>从 secret.json 中读取特定密钥，如果未找到对应密钥则抛出异常。</summary>
    public static string GetSecret(string key)
        => ConfigProviders.Secret.Read<string>(key, null, false) ?? throw new KeyNotFoundException($"未找到密钥：{key}");

}
