namespace MeloongCore.Extensions;
public static class EnumerableExtensions {

    #region 去重

    /// <summary>
    /// 通过比较器进行去重。
    /// 该方法的效率较低，建议仅在小型列表上使用，或换用 DistinctBy。
    /// </summary>
    public static IEnumerable<T> Distinct<T>(this IList<T> list, Func<T, T, bool> comparer) {
        var seen = new List<T>();
        foreach (var item in list) {
            if (!seen.Any(s => comparer(s, item))) {
                seen.Add(item);
                yield return item;
            }
        }
    }
    /// <summary>
    /// 按对象的指定值去重。
    /// </summary>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
        var seen = new HashSet<TKey>();
        foreach (var element in source) {
            if (seen.Add(selector(element))) yield return element;
        }
    }
    /// <summary>
    /// 按对象的指定值去重。
    /// </summary>
    public static ParallelQuery<TSource> DistinctBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> selector) {
        var seen = new ConcurrentDictionary<TKey, bool>();
        return source.Where(element => seen.TryAdd(selector(element), true));
    }

    #endregion

    #region 最大 / 最小

    /// <summary>
    /// 选择所有最大值对应的对象。
    /// 若没有元素则返回空列表。
    /// </summary>
    public static List<TSource> MaxByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        results.Add(maxItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
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
    /// 选择所有最大值对应的对象。
    /// 若没有元素则返回空列表。
    /// </summary>
    public static List<TSource> MaxByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        results.Add(maxItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = comparer.Compare(currentValue, maxValue);
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
    public static List<TSource> MinByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        results.Add(minItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
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
    /// 选择所有最小值对应的对象。
    /// 若没有元素则返回空列表。
    /// </summary>
    public static List<TSource> MinByAll<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        var results = new List<TSource>();
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return results;
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        results.Add(minItem);
        while (enumerator.MoveNext()) {
            TSource currentItem = enumerator.Current;
            TKey currentValue = selector(currentItem);
            int comparisonResult = comparer.Compare(currentValue, minValue);
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
    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (value.CompareTo(maxValue) <= 0) continue;
            maxItem = enumerator.Current;
            maxValue = value;
        }
        return maxItem;
    }
    /// <summary>
    /// 选择最大值对应的对象。
    /// 若没有元素则返回 null。
    /// </summary>
    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        TSource maxItem = enumerator.Current;
        TKey maxValue = selector(maxItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (comparer.Compare(value, maxValue) <= 0) continue;
            maxItem = enumerator.Current;
            maxValue = value;
        }
        return maxItem;
    }
    /// <summary>
    /// 选择最小值对应的对象。
    /// 若没有元素则返回 null。
    /// </summary>
    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) { return default; }
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (value.CompareTo(minValue) >= 0) { continue; }
            minItem = enumerator.Current;
            minValue = value;
        }
        return minItem;
    }
    /// <summary>
    /// 选择最小值对应的对象。
    /// 若没有元素则返回 null。
    /// </summary>
    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) { return default; }
        TSource minItem = enumerator.Current;
        TKey minValue = selector(minItem);
        while (enumerator.MoveNext()) {
            TKey value = selector(enumerator.Current);
            if (comparer.Compare(value, minValue) >= 0) { continue; }
            minItem = enumerator.Current;
            minValue = value;
        }
        return minItem;
    }

    #endregion

    #region 排序

    /// <summary>
    /// 使用默认比较器对集合进行排序。顺序为从小到大。
    /// </summary>
    public static IEnumerable<T> Ordered<T>(this IEnumerable<T> source) where T : IComparable<T> 
        => source.OrderBy(x => x);

    /// <summary>
    /// 使用默认比较器对集合进行排序。顺序为从大到小。
    /// </summary>
    public static IEnumerable<T> OrderedDescending<T>(this IEnumerable<T> source) where T : IComparable<T> 
        => source.OrderByDescending(x => x);

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
    public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) 
        => dict.TryRemove(key, out _);

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
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue? defaultValue = default) 
        => dict.TryGetValue(key, out var result) ? result : defaultValue;

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
    public static IEnumerable<T> Flags<T>(this T value) where T : struct, Enum
        => EnumUtils.GetAllFlags<T>().Where(element => value.HasFlag(element));

    /// <summary>
    /// 从集合中移除满足条件的元素。
    /// </summary>
    public static void RemoveIf<T>(this ICollection<T> source, Func<T, bool> predicate) {
        var itemsToRemove = source.Where(predicate).ToList();
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    /// <summary>
    /// 从集合中移除另一个集合中的所有元素。
    /// </summary>
    public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> itemsToRemove) {
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    /// <summary>
    /// 仅保留满足条件的元素，移除不满足条件的元素。
    /// </summary>
    public static void KeepIf<T>(this ICollection<T> source, Func<T, bool> predicate) {
        var itemsToRemove = source.Where(item => !predicate(item)).ToList();
        foreach (var item in itemsToRemove) source.Remove(item);
    }

    /// <summary>
    /// 从集合中移除指定元素。
    /// </summary>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T element)
        => source.Except([element]);

    /// <summary>
    /// 判断集合是否有且仅有一个元素。
    /// 在输入 null 时返回 false。
    /// </summary>
    public static bool IsSingle<T>(this IEnumerable<T>? source) {
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
