using System.Globalization;

namespace MeloongCore;
public static class EnumUtils {

    /// <summary>
    /// 获取枚举的所有值。
    /// </summary>
    private static IEnumerable<T> GetAllValues<T>() where T : struct, Enum
        => Enum.GetValues(typeof(T)).Cast<T>();

    /// <summary>
    /// 将字符串转换为枚举。
    /// 支持枚举名或枚举序数；若为空字符串或 <see langword="null"/> 则返回 0。
    /// 转换失败会抛出异常。
    /// </summary>
    public static T FromString<T>(string? value) where T : struct, Enum {
        if (string.IsNullOrEmpty(value)) {
            return (T) Enum.ToObject(typeof(T), 0);
        } else if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var number)) {
            return (T) Enum.ToObject(typeof(T), Convert.ToInt64(number));
        } else {
            return (T) Enum.Parse(typeof(T), value, true);
        }
    }

    public static bool HasFlagF<T>(this T value, T flag) where T : struct, Enum
        => value.HasFlag(flag) && !EqualityComparer<T>.Default.Equals(flag, default);

    /// <summary>
    /// 对于具有 <see cref="FlagsAttribute"/> 的枚举，返回其所有 Flag 值。
    /// 建议仅对没有极端值的正数枚举使用。
    /// </summary>
    /// <remarks>
    /// 会跳过：0、非 2 的幂的值、不在枚举值中的数、组合值、对应同一个枚举项的多个别名。
    /// </remarks>
    public static List<T> GetAllFlags<T>() where T : struct, Enum {
        if (allFlagsCache.TryGetValue(typeof(T), out var cached)) return cached.Cast<T>().ToList();
        if (!typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false)) throw new ArgumentException($"枚举 {typeof(T).FullName} 没有定义 FlagsAttribute。");
        var flags = new List<T>();
        foreach (var element in EnumUtils.GetAllValues<T>().Distinct()) {
            var number = Convert.ToInt64(element, CultureInfo.InvariantCulture);
            if (number == 0 || (number & (number - 1)) != 0) continue; // 跳过 0 和非 2 的幂的值
            flags.Add(element);
        }
        allFlagsCache[typeof(T)] = flags.Cast<Enum>().ToList();
        return flags;
    }
    private static readonly ConcurrentDictionary<Type, List<Enum>> allFlagsCache = new();

}
