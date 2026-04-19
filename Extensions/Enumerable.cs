using System;
using System.Collections.Generic;

namespace MeloongCore.Extensions {
    public static class EnumerableExtensions {

        /// <summary>
        /// 去重。
        /// </summary>
        public static List<T> Distinct<T>(this IList<T> Arr, Func<T, T, bool> IsEqual) {
            var ResultArray = new List<T>();
            for (int i = 0; i <= Arr.Count - 1; i++) {
                for (int ii = i + 1; ii <= Arr.Count - 1; ii++) {
                    if (IsEqual(Arr[i], Arr[ii])) { goto NextElement; }
                }
                ResultArray.Add(Arr[i]);
NextElement:;
            }
            return ResultArray;
        }

        /// <summary>
        /// 判断这是否是仅有一个元素的集合。
        /// </summary>
        public static bool IsSingle<T>(this IEnumerable<T> Source) {
            if (Source == null) {
                return false;
            } else if (Source is IList<T> list) {
                return list.Count == 1;
            } else {
                using (var Enumerator = Source.GetEnumerator()) {
                    if (!Enumerator.MoveNext()) { return false; }
                    if (Enumerator.MoveNext()) { return false; }
                    return true;
                }
            }
        }

        /// <summary>
        /// 对集合的每个元素执行指定操作。
        /// </summary>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> Collection, Action<T> Action) {
            foreach (T Item in Collection) {
                Action(Item);
            }
            return Collection;
        }

        /// <summary>
        /// 选择所有最大值对应的对象。
        /// 若没有元素则返回空列表。
        /// </summary>
        public static List<T> MaxByAll<T, C>(this IEnumerable<T> Source, Func<T, C> Selector) where C : IComparable<C> {
            var Results = new List<T>();
            using (var Enumerator = Source.GetEnumerator()) {
                if (!Enumerator.MoveNext()) { return Results; }
                T MaxItem = Enumerator.Current;
                C MaxValue = Selector(MaxItem);
                Results.Add(MaxItem);
                while (Enumerator.MoveNext()) {
                    T CurrentItem = Enumerator.Current;
                    C CurrentValue = Selector(CurrentItem);
                    int Comparison = CurrentValue.CompareTo(MaxValue);
                    if (Comparison > 0) {
                        MaxValue = CurrentValue;
                        Results.Clear();
                        Results.Add(CurrentItem);
                    } else if (Comparison == 0) {
                        Results.Add(CurrentItem);
                    }
                }
            }
            return Results;
        }

        /// <summary>
        /// 选择所有最小值对应的对象。
        /// 若没有元素则返回空列表。
        /// </summary>
        public static List<T> MinByAll<T, C>(this IEnumerable<T> List, Func<T, C> Selector) where C : IComparable<C> {
            var Results = new List<T>();
            using (var Enumerator = List.GetEnumerator()) {
                if (!Enumerator.MoveNext()) { return Results; }
                T MinItem = Enumerator.Current;
                C MinValue = Selector(MinItem);
                Results.Add(MinItem);
                while (Enumerator.MoveNext()) {
                    T CurrentItem = Enumerator.Current;
                    C CurrentValue = Selector(CurrentItem);
                    int Comparison = CurrentValue.CompareTo(MinValue);
                    if (Comparison < 0) {
                        MinValue = CurrentValue;
                        Results.Clear();
                        Results.Add(CurrentItem);
                    } else if (Comparison == 0) {
                        Results.Add(CurrentItem);
                    }
                }
            }
            return Results;
        }

        /// <summary>
        /// 选择最大值对应的对象。
        /// 若没有元素则返回 Nothing。
        /// </summary>
        public static T MaxBy<T, C>(this IEnumerable<T> Source, Func<T, C> Selector) where C : IComparable<C> {
            using (var Enumerator = Source.GetEnumerator()) {
                if (!Enumerator.MoveNext()) { return default(T); }
                T MaxItem = Enumerator.Current;
                C MaxValue = Selector(MaxItem);
                while (Enumerator.MoveNext()) {
                    C Value = Selector(Enumerator.Current);
                    if (Value.CompareTo(MaxValue) <= 0) { continue; }
                    MaxItem = Enumerator.Current;
                    MaxValue = Value;
                }
                return MaxItem;
            }
        }

        /// <summary>
        /// 选择最小值对应的对象。
        /// 若没有元素则返回 Nothing。
        /// </summary>
        public static T MinBy<T, C>(this IEnumerable<T> List, Func<T, C> Selector) where C : IComparable<C> {
            using (var Enumerator = List.GetEnumerator()) {
                if (!Enumerator.MoveNext()) { return default; }
                T MinItem = Enumerator.Current;
                C MinValue = Selector(MinItem);
                while (Enumerator.MoveNext()) {
                    C Value = Selector(Enumerator.Current);
                    if (Value.CompareTo(MinValue) >= 0) { continue; }
                    MinItem = Enumerator.Current;
                    MinValue = Value;
                }
                return MinItem;
            }
        }

    }
}