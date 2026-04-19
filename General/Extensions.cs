using System;
using System.Runtime.CompilerServices;

namespace PCLCS {
    public static class Extensions {

        #region String

        /// <summary>
        /// 高速的 StartsWith。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithF(this string value, string prefix, bool ignoreCase = false) {
            if (value == null) return false;
            return value.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        /// <summary>
        /// 高速的 EndsWith。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithF(this string value, string suffix, bool ignoreCase = false) {
            if (value == null) return false;
            return value.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// 忽略大小写的 Contains。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsIgnoreCase(this string value, string subString) {
            return value.IndexOf(subString,StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 高速的 IndexOf。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfF(this string value, string subString, bool ignoreCase = false) {
            return value.IndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        /// <summary>
        /// 高速的 IndexOf。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false) {
            return value.IndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// 高速的 LastIndexOf。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfF(this string value, string subString, bool ignoreCase = false) {
            return value.LastIndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        /// <summary>
        /// 高速的 LastIndexOf。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false) {
            return value.LastIndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        #endregion

    }
}
