using System.Globalization;
using System.Text.RegularExpressions;

namespace MeloongCore;
public static class StringUtils {

    #region 转义

    /// <summary>
    /// WPF XML 转义。
    /// </summary>
    public static string XmlEscape(string str) {
        if (str.StartsWithF("{")) str = "{}" + str; // #4187
        return str
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;")
            .Replace("\"", "&quot;").ReplaceLineEndings("&#xa;");
    }

    /// <summary>
    /// VB.NET 的 Like 关键字转义。
    /// </summary>
    public static string LikePatternEscape(string input) {
        var builder = new StringBuilder();
        foreach (char c in input) {
            if (c is '[' or ']' or '*' or '?' or '#') {
                builder.Append('[').Append(c).Append(']');
            } else {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 正则表达式转义。
    /// 等同于 <see cref="Regex.Escape"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexEscape(string value) 
        => Regex.Escape(value);

    /// <summary>
    /// 正则表达式去转义。
    /// 等同于 <see cref="Regex.Unescape"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexUnescape(string value) 
        => Regex.Unescape(value);

    /// <summary>
    /// URL 转义。
    /// 等同于 <see cref="Uri.EscapeDataString"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlEscape(string value) 
        => Uri.EscapeDataString(value);

    /// <summary>
    /// URL 去转义。
    /// 等同于 <see cref="Uri.UnescapeDataString"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlUnescape(string value) 
        => Uri.UnescapeDataString(value);

    /// <summary>
    /// 表单转义。
    /// 等同于 <see cref="WebUtility.UrlEncode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlEscape(string value) 
        => WebUtility.UrlEncode(value)!;

    /// <summary>
    /// 表单去转义。
    /// 等同于 <see cref="WebUtility.UrlDecode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlUnescape(string value) 
        => WebUtility.UrlDecode(value);

    /// <summary>
    /// HTML 转义。
    /// 等同于 <see cref="WebUtility.HtmlEncode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlEscape(string value) 
        => WebUtility.HtmlEncode(value);

    /// <summary>
    /// HTML 去转义。
    /// 等同于 <see cref="WebUtility.HtmlDecode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlUnescape(string value) 
        => WebUtility.HtmlDecode(value);

    #endregion

    #region 格式化

    /// <summary>
    /// 将文件大小的字节数格式化为适合阅读的文本，如 <c>"1.28 M"</c>、<c>"128 G"</c>。
    /// </summary>
    public static string FormatByteSize(long byteSize) {
        var sign = byteSize < 0 ? "-" : "";
        if (byteSize < 0) byteSize *= -1;
        if (byteSize < 1000) { // B 级
            return $"{sign}{byteSize} B";
        } else if (byteSize < 1024 * 1000) { // K 级
            var roundResult = Math.Round(byteSize / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D, (3 - roundResult.Length).Clamp(0, 2))} K";
        } else if (byteSize < 1024 * 1024 * 1000) { // M 级
            var roundResult = Math.Round(byteSize / 1024D / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D / 1024D, (3 - roundResult.Length).Clamp(0, 2))} M";
        } else { // G 级
            var roundResult = Math.Round(byteSize / 1024D / 1024D / 1024D).ToString(CultureInfo.InvariantCulture);
            return $"{sign}{Math.Round(byteSize / 1024D / 1024D / 1024D, (3 - roundResult.Length).Clamp(0, 2))} G";
        }
    }

    /// <summary>
    /// 将时间间隔格式化为适合阅读的文本。
    /// <para/>若指定了 <paramref name="isShort"/>，则只使用一个单位，如 <c>"5 分钟前"</c>，否则有可能会使用两个单位，如 <c>"5 分 10 秒前"</c>。
    /// </summary>
    public static string FormatTimeSpan(TimeSpan span, bool isShort) {
        var suffix = span.TotalMilliseconds > 0 ? "后" : "前";
        if (span.TotalMilliseconds < 0) span = -span;
        var totalMonths = (int) Math.Floor(span.Days / 30D);
        if (isShort) {
            if (totalMonths >= 12) {
                // 1+ 年，“3 年”
                return $"{Math.Floor(totalMonths / 12D)} 年{suffix}";
            } else if (totalMonths >= 2) {
                // 2~11 月，“5 个月”
                return $"{totalMonths} 个月{suffix}";
            } else if (span.TotalDays >= 2) {
                // 2 天 ~ 2 月，“23 天”
                return $"{span.Days} 天{suffix}";
            } else if (span.TotalHours >= 1) {
                // 1 小时 ~ 2 天，“15 小时”
                return $"{span.Hours} 小时{suffix}";
            } else if (span.TotalMinutes >= 1) {
                // 1 分钟 ~ 1 小时，“49 分钟”
                return $"{span.Minutes} 分钟{suffix}";
            } else if (span.TotalSeconds >= 1) {
                // 1 秒 ~ 1 分钟，“23 秒”
                return $"{span.Seconds} 秒{suffix}";
            } else {
                // 不到 1 秒
                return $"1 秒{suffix}";
            }
        } else {
            if (totalMonths >= 61) {
                // 5+ 年，“5 年”
                return $"{Math.Floor(totalMonths / 12D)} 年{suffix}";
            } else if (totalMonths >= 12) {
                // 12~60 月，“1 年 2 个月”
                return $"{Math.Floor(totalMonths / 12D)} 年{(totalMonths % 12 > 0 ? $" {totalMonths % 12} 个月" : "")}{suffix}";
            } else if (totalMonths >= 4) {
                // 4~11 月，“5 个月”
                return $"{totalMonths} 个月{suffix}";
            } else if (totalMonths >= 1) {
                // 1~4 月，“2 个月 13 天”
                return $"{totalMonths} 月{(span.Days % 30 > 0 ? $" {span.Days % 30} 天" : "")}{suffix}";
            } else if (span.TotalDays >= 4) {
                // 4~30 天，“23 天”
                return $"{span.Days} 天{suffix}";
            } else if (span.TotalDays >= 1) {
                // 1~3 天，“2 天 20 小时”
                return $"{span.Days} 天{(span.Hours > 0 ? $" {span.Hours} 小时" : "")}{suffix}";
            } else if (span.TotalHours >= 10) {
                // 10 小时 ~ 1 天，“15 小时”
                return $"{span.Hours} 小时{suffix}";
            } else if (span.TotalHours >= 1) {
                // 1~10 小时，“1 小时 20 分钟”
                return $"{span.Hours} 小时{(span.Minutes > 0 ? $" {span.Minutes} 分钟" : "")}{suffix}";
            } else if (span.TotalMinutes >= 10) {
                // 10 分钟 ~ 1 小时，“49 分钟”
                return $"{span.Minutes} 分钟{suffix}";
            } else if (span.TotalMinutes >= 1) {
                // 1~10 分钟，“9 分 23 秒”
                return $"{span.Minutes} 分{(span.Seconds > 0 ? $" {span.Seconds} 秒" : "")}{suffix}";
            } else if (span.TotalSeconds >= 1) {
                // 1 秒 ~ 1 分钟，“23 秒”
                return $"{span.Seconds} 秒{suffix}";
            } else {
                // 不到 1 秒
                return $"1 秒{suffix}";
            }
        }
    }

    #endregion

    /// <summary>
    /// 将字符串解析为版本号。
    /// 允许缺少任意多个段，且会将缺失的段不为 0，而不是 -1。例如，"2" 会被解析为 2.0.0.0。
    /// </summary>
    public static Version ParseVersionWithDefaults(string value) {
        while (value.Count(c => c == '.') < 3) value += ".0";
        return Version.Parse(value);
    }

}