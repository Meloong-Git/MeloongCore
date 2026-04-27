namespace MeloongCore.Extensions;
public static class EnumerableExtensions {

    #region Distinct

    /// <summary>
    /// 通过比较器进行去重。
    /// 该方法的效率较低，建议仅在小型列表上使用，或换用 DistinctBy。
    /// </summary>
    public static IEnumerable<T> Distinct<T>(this IList<T> list, Func<T, T, bool> comparer) {
        for (int i = 0; i < list.Count; i++) {
            if (!list.Skip(i + 1).Any(item => comparer(list[i], item))) yield return list[i];
        }
    }
    /// <summary>
    /// 按对象的指定值去重。
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector) {
        var seen = new HashSet<TKey>();
        foreach (var element in source) {
            if (seen.Add(selector(element))) yield return element;
        }
    }
    /// <summary>
    /// 按对象的指定值去重。
    /// </summary>
    public static ParallelQuery<T> DistinctBy<T, TKey>(this ParallelQuery<T> source, Func<T, TKey> selector) {
        var seen = new ConcurrentDictionary<TKey, bool>();
        return source.Where(element => seen.TryAdd(selector(element), true));
    }

    #endregion

    #region MinBy / MaxBy

    /// <summary>
    /// 选择所有最大值对应的对象。
    /// 若没有元素则返回空列表。
    /// </summary>
    public static List<T> MaxByAll<T, C>(this IEnumerable<T> source, Func<T, C> selector) where C : IComparable<C> {
        var results = new List<T>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        T maxItem = enumerator.Current;
        C maxValue = selector(maxItem);
        results.Add(maxItem);
        while (enumerator.MoveNext()) {
            T currentItem = enumerator.Current;
            C currentValue = selector(currentItem);
            int comparisonResult = currentValue.CompareTo(maxValue);
            if (comparisonResult > 0) {
                maxValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }
    /// <summary>
    /// 选择所有最小值对应的对象。
    /// 若没有元素则返回空列表。
    /// </summary>
    public static List<T> MinByAll<T, C>(this IEnumerable<T> source, Func<T, C> selector) where C : IComparable<C> {
        var results = new List<T>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        T minItem = enumerator.Current;
        C minValue = selector(minItem);
        results.Add(minItem);
        while (enumerator.MoveNext()) {
            T currentItem = enumerator.Current;
            C currentValue = selector(currentItem);
            int comparisonResult = currentValue.CompareTo(minValue);
            if (comparisonResult < 0) {
                minValue = currentValue;
                results.Clear();
                results.Add(currentItem);
            } else if (comparisonResult == 0) {
                results.Add(currentItem);
            }
        }
        return results;
    }

    /// <summary>
    /// 选择最大值对应的对象。
    /// 若没有元素则返回 null。
    /// </summary>
    public static T? MaxBy<T, C>(this IEnumerable<T> source, Func<T, C> selector) where C : IComparable<C> {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        T maxItem = enumerator.Current;
        C maxValue = selector(maxItem);
        while (enumerator.MoveNext()) {
            C value = selector(enumerator.Current);
            if (value.CompareTo(maxValue) <= 0) continue;
            maxItem = enumerator.Current;
            maxValue = value;
        }
        return maxItem;
    }
    /// <summary>
    /// 选择最小值对应的对象。
    /// 若没有元素则返回 null。
    /// </summary>
    public static T? MinBy<T, C>(this IEnumerable<T> List, Func<T, C> Selector) where C : IComparable<C> {
        using var enumerator = List.GetEnumerator();
        if (!enumerator.MoveNext()) { return default; }
        T minItem = enumerator.Current;
        C minValue = Selector(minItem);
        while (enumerator.MoveNext()) {
            C value = Selector(enumerator.Current);
            if (value.CompareTo(minValue) >= 0) { continue; }
            minItem = enumerator.Current;
            minValue = value;
        }
        return minItem;
    }

    #endregion

    #region Join

    /// <summary>
    /// 用指定的分割符将集合连接为字符串。
    /// </summary>
    public static string Join(this IEnumerable list, char split) {
        var builder = new StringBuilder();
        bool isFirst = true;
        foreach (var element in list) {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(split);
            if (element is not null) builder.Append(element.ToString());
        }
        return builder.ToString();
    }
    /// <summary>
    /// 用指定的分割符将集合连接为字符串。
    /// </summary>
    public static string Join(this IEnumerable list, string split) {
        if (split.IsSingle()) return list.Join(split[0]);
        var builder = new StringBuilder();
        bool isFirst = true;
        foreach (var element in list) {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(split);
            if (element is not null) builder.Append(element.ToString());
        }
        return builder.ToString();
    }

    #endregion

    #region Dictionary

    /// <summary>
    /// 从 <see cref="ConcurrentDictionary{TKey, TValue}"/> 中移除具有指定键的元素。
    /// 返回是否确实移除了元素。
    /// </summary>
    public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) => dict.TryRemove(key, out _);

    /// <summary>
    /// 将二维数组转换为字典。
    /// </summary>
    public static Dictionary<T, T> ToDictionary<T>(this T[,] arr) where T : notnull {
        Dictionary<T, T> result = [];
        if (arr.Length == 0) return result;
        if (arr.GetLength(1) != 2) throw new ArgumentException("数组必须为两列，第一列为 Key，第二列为 Value。");
        for (int i = 0; i < arr.GetLength(0); i++) result[arr[i, 0]] = arr[i, 1];
        return result;
    }

    /// <summary>
    /// 尝试从字典中获取某项，如果该项不存在，则返回默认值。
    /// </summary>
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default!) =>
        dict.TryGetValue(key, out var result) ? result : defaultValue;

    /// <summary>
    /// 将值添加到字典的对应项的列表中。
    /// </summary>
    public static void AddIntoValueCollection<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value) {
        if (dict.TryGetValue(key, out var collection)) {
            collection.Add(value);
        } else {
            dict.Add(key, [value]);
        }
    }

    #endregion

    /// <summary>
    /// 将 <see cref="FlagsAttribute"/> 枚举值解包为它包含的单一 Flag 成员的集合。
    /// 建议仅对没有极端值的正数枚举使用。
    /// </summary>
    /// <remarks>
    /// 会跳过：0、非 2 的幂的值、不在枚举值中的数、组合值、对应同一个枚举项的多个别名。
    /// </remarks>
    public static IEnumerable<T> Flags<T>(this T value) where T : struct, Enum {
        foreach (var element in Enum.GetValues(typeof(T)).Cast<T>().Distinct()) {
            var number = Convert.ToInt64(element);
            if (number == 0 || (number & (number - 1)) != 0) continue; // 跳过 0 和非 2 的幂的值
            if (value.HasFlag(element)) yield return element;
        }
    }

    /// <summary>
    /// 判断集合是否有且仅有一个元素。
    /// 在输入 null 时返回 false。
    /// </summary>
    public static bool IsSingle<T>([AllowNull] this IEnumerable<T> source) {
        if (source is null) return false;
        if (source is IList<T> list) return list.Count == 1;
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return false;
        if (enumerator.MoveNext()) return false;
        return true;
    }

    /// <summary>
    /// 对集合的每个元素执行指定操作。
    /// </summary>
    public static IEnumerable<T> ForAll<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (T item in source) action(item);
        return source;
    }

}
