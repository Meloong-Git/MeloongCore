using System.Text.RegularExpressions;

namespace MeloongCore.Extensions;
public static class StringExtensions {

    #region 区域性 / 大小写 简化

    /// <summary>
    /// 高速的 StartsWith。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithF(this string? value, string prefix, bool ignoreCase = false) {
        if (value is null) return false;
        return value.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    /// <summary>
    /// 高速的 EndsWith。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithF(this string? value, string suffix, bool ignoreCase = false) {
        if (value is null) return false;
        return value.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>
    /// 忽略大小写的 Contains。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string value, string subString) {
        return value.IndexOf(subString, StringComparison.OrdinalIgnoreCase) >= 0;
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

    /// <summary>
    /// ToLowerInvariant 的简略写法。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Lower(this string str) => str.ToLowerInvariant();
    /// <summary>
    /// ToUpperInvariant 的简略写法。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Upper(this string str) => str.ToUpperInvariant();

    #endregion

    #region 分割与裁切

    /// <summary>
    /// 分割字符串。
    /// 若原始字符串为空，则返回 {""}。
    /// </summary>
    public static string[] Split(this string fullStr, string splitStr) {
        if (splitStr.IsSingle())
            return fullStr.Split(splitStr[0]);
        else
            return fullStr.Split([splitStr], StringSplitOptions.None);
    }

    /// <summary>
    /// 获取在子字符串第一次出现之前的部分，例如对 2024/11/08 拆切 / 会得到 2024。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string BeforeFirst(this string str, [AllowNull] string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text!, ignoreCase);
        if (pos >= 0)
            return str.Substring(0, pos);
        else
            return str;
    }
    /// <summary>
    /// 获取在子字符串最后一次出现之前的部分，例如对 2024/11/08 拆切 / 会得到 2024/11。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string BeforeLast(this string str, [AllowNull] string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text!, ignoreCase);
        if (pos >= 0)
            return str.Substring(0, pos);
        else
            return str;
    }

    /// <summary>
    /// 获取在子字符串第一次出现之后的部分，例如对 2024/11/08 拆切 / 会得到 11/08。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string AfterFirst(this string str, [AllowNull] string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text!, ignoreCase);
        if (pos >= 0)
            return str.Substring(pos + text!.Length);
        else
            return str;
    }
    /// <summary>
    /// 获取在子字符串最后一次出现之后的部分，例如对 2024/11/08 拆切 / 会得到 08。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string AfterLast(this string str, [AllowNull] string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text!, ignoreCase);
        if (pos >= 0)
            return str.Substring(pos + text!.Length);
        else
            return str;
    }

    /// <summary>
    /// 获取处于两个子字符串之间的部分，裁切尽可能多的内容。
    /// 等效于 AfterLast 后接 BeforeFirst。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string Between(this string str, string after, string before, bool ignoreCase = false) {
        int startPos = string.IsNullOrEmpty(after) ? -1 : str.LastIndexOfF(after, ignoreCase);
        if (startPos >= 0)
            startPos += after.Length;
        else
            startPos = 0;

        int endPos = string.IsNullOrEmpty(before) ? -1 : str.IndexOfF(before, startPos, ignoreCase);
        if (endPos >= 0)
            return str.Substring(startPos, endPos - startPos);
        else if (startPos > 0)
            return str.Substring(startPos);
        else
            return str;
    }

    #endregion

    #region 替换

    /// <summary>
    /// 替换字符串中的内容。
    /// 仅当需要替换时，才调用 <paramref name="newValue"/> 获取结果字符串。
    /// </summary>
    public static string ReplaceOnDemand(this string str, string oldValue, Func<string> newValue) =>
        str.Contains(oldValue) ? str.Replace(oldValue, newValue()) : str;

    /// <summary>
    /// 将字符串中的换行符统一替换为指定字符。
    /// 若指定了 <paramref name="mergeMultiple"/>，会将多次换行合并成一次换行。
    /// </summary>
    public static string ReplaceLineEndings(this string input, string newValue, bool mergeMultiple = false) =>
        Regex.Replace(input,
            mergeMultiple ? @"(?:\r\n|[\n\r\f\u0085\u2028\u2029])+" : @"\r\n|[\n\r\f\u0085\u2028\u2029]",
            newValue.Replace("$", "$$"), // 避免识别成捕获组
            RegexOptions.Compiled);

    #endregion

    /// <summary>
    /// 将第一个字符转换为大写，其余字符转换为小写。
    /// </summary>
    public static string Capitalize(this string word) {
        if (string.IsNullOrEmpty(word)) return word;
        return $"{word.Substring(0, 1).Upper()}{word.Substring(1).Lower()}";
    }

    /// <summary>
    /// 将字符串统一至某个长度。
    /// 过短则用 <paramref name="code"/> 将其左侧填充，过长则截取靠左的指定长度。
    /// </summary>
    public static string EnsureLength(this string? str, char code, int length) {
        if (str == null) str = "";
        return str.Length > length ? str.Substring(0, length) : str.PadLeft(length, code);
    }

    /// <summary>
    /// 该字符串中的字符是否均为 ASCII 字符。
    /// </summary>
    public static bool IsAsciiOnly(this string input) => input.All(c => c < 128);

}
