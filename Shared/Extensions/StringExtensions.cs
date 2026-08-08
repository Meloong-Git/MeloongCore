using System.Text.RegularExpressions;

namespace MeloongCore.Extensions;
public static class StringExtensions {

    #region 区域性 / 大小写 简化

    /// <summary>
    /// 非区域性的 <see cref="string.StartsWith(string)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithF(this string? value, string prefix, bool ignoreCase = false) 
        => value?.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;
    /// <summary>
    /// 判断字符串是否以 <paramref name="prefix"/> 开头。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithF(this string? value, char prefix, bool ignoreCase = false) 
        => value?.StartsWith(prefix.ToString(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;
    /// <summary>
    /// 非区域性的 <see cref="string.EndsWith(string)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithF(this string? value, string suffix, bool ignoreCase = false) 
        => value?.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;
    /// <summary>
    /// 判断字符串是否以 <paramref name="suffix"/> 结尾。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithF(this string? value, char suffix, bool ignoreCase = false) 
        => value?.EndsWith(suffix.ToString(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ?? false;

    /// <summary>
    /// 忽略大小写的 <see cref="string.Contains(string)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string value, string subString) 
        => value.IndexOf(subString, StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// 非区域性的 <see cref="string.IndexOf(string)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfF(this string value, string subString, bool ignoreCase = false) 
        => value.IndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    /// <summary>
    /// 非区域性的 <see cref="string.IndexOf(string, int)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false) 
        => value.IndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>
    /// 非区域性的 <see cref="string.LastIndexOf(string)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfF(this string value, string subString, bool ignoreCase = false) 
        => value.LastIndexOf(subString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    /// <summary>
    /// 非区域性的 <see cref="string.LastIndexOf(string, int)"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfF(this string value, string subString, int startIndex, bool ignoreCase = false) 
        => value.LastIndexOf(subString, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>
    /// <see cref="string.ToLowerInvariant"/> 的简略写法。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Lower(this string str) 
        => str.ToLowerInvariant();
    /// <summary>
    /// <see cref="string.ToUpperInvariant"/> 的简略写法。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Upper(this string str) 
        => str.ToUpperInvariant();

    #endregion

    #region Split

    /// <summary>
    /// 分割字符串。
    /// 若原始字符串为空，则返回 {""}。
    /// </summary>
    public static string[] Split(this string fullStr, string splitStr, bool removeEmptyEntries = false) {
        if (splitStr.IsSingle()) {
            return fullStr.Split(splitStr[0], removeEmptyEntries);
        } else {
            return fullStr.Split([splitStr], removeEmptyEntries);
        }
    }
    /// <summary>
    /// 分割字符串。
    /// 若原始字符串为空，则返回 {""}。
    /// </summary>
    public static string[] Split(this string fullStr, char splitChar, bool removeEmptyEntries = false) 
        => fullStr.Split([splitChar], removeEmptyEntries);
    /// <summary>
    /// 分割字符串。
    /// 若原始字符串为空，则返回 {""}。
    /// </summary>
    public static string[] Split(this string fullStr, string[] splitStrings, bool removeEmptyEntries = false)
        => fullStr.Split(splitStrings, removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
    /// <summary>
    /// 分割字符串。
    /// 若原始字符串为空，则返回 {""}。
    /// </summary>
    public static string[] Split(this string fullStr, char[] splitChars, bool removeEmptyEntries = false)
        => fullStr.Split(splitChars, removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);

    /// <summary>
    /// 将字符串分割为多行。
    /// </summary>
    public static string[] SplitLines(this string input, bool skipEmptyLines = false)
        => input.ReplaceLineEndings("\n").Split('\n', skipEmptyLines);

    #endregion

    #region 裁切

    /// <summary>
    /// 获取在子字符串第一次出现之前的部分，如果未找到子字符串则不裁切。
    /// <code>"2024/11/08".BeforeFirst("/") => "2024"</code>
    /// </summary>
    public static string BeforeFirst(this string str, string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text, ignoreCase);
        if (pos < 0) return str;
        return str[..pos];
    }
    /// <summary>
    /// 获取在任意子字符串第一次出现之前的部分，如果未找到任意子字符串则不裁切。
    /// <code>"2024/11-08".BeforeFirstOfAny(["/", "-"]) => "2024"</code>
    /// </summary>
    public static string BeforeFirstOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.IndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos > p)) pos = p;
        }
        if (pos < 0) return str;
        return str[..pos];
    }

    /// <summary>
    /// 获取在子字符串最后一次出现之前的部分，如果未找到子字符串则不裁切。
    /// <code>"2024/11/08".BeforeLast("/") => "2024/11"</code>
    /// </summary>
    public static string BeforeLast(this string str, string text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text, ignoreCase);
        if (pos < 0) return str;
        return str[..pos];
    }
    /// <summary>
    /// 获取在任意子字符串最后一次出现之前的部分，如果未找到任意子字符串则不裁切。
    /// <code>"2024/11-08".BeforeLastOfAny(["/", "-"]) => "2024/11"</code>
    /// </summary>
    public static string BeforeLastOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.LastIndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos < p)) pos = p;
        }
        if (pos < 0) return str;
        return str[..pos];
    }

    /// <summary>
    /// 获取在子字符串第一次出现之后的部分，如果未找到子字符串则不裁切。
    /// <code>"2024/11/08".AfterFirst("/") => "11/08"</code>
    /// </summary>
    public static string AfterFirst(this string str, string? text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.IndexOfF(text!, ignoreCase);
        if (pos < 0) return str;
        return str[(pos + text!.Length)..];
    }
    /// <summary>
    /// 获取在任意子字符串第一次出现之后的部分，如果未找到任意子字符串则不裁切。
    /// <code>"2024/11-08".AfterFirstOfAny(["/", "-"]) => "11-08"</code>
    /// </summary>
    public static string AfterFirstOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        int len = 0;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.IndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos > p)) {
                pos = p;
                len = text.Length;
            }
        }
        if (pos < 0) return str;
        return str[(pos + len)..];
    }

    /// <summary>
    /// 获取在子字符串最后一次出现之后的部分，如果未找到子字符串则不裁切。
    /// <code>"2024/11/08".AfterLast("/") => "08"</code>
    /// </summary>
    public static string AfterLast(this string str, string? text, bool ignoreCase = false) {
        int pos = string.IsNullOrEmpty(text) ? -1 : str.LastIndexOfF(text!, ignoreCase);
        if (pos < 0) return str;
        return str[(pos + text!.Length)..];
    }
    /// <summary>
    /// 获取在任意子字符串最后一次出现之后的部分，如果未找到任意子字符串则不裁切。
    /// <code>"2024/11-08".AfterLastOfAny(["/", "-"]) => "08"</code>
    /// </summary>
    public static string AfterLastOfAny(this string str, IEnumerable<string>? texts, bool ignoreCase = false) {
        if (texts == null) return str;
        int pos = -1;
        int len = 0;
        foreach (string text in texts) {
            if (string.IsNullOrEmpty(text)) continue;
            int p = str.LastIndexOfF(text, ignoreCase);
            if (p >= 0 && (pos < 0 || pos < p)) {
                pos = p;
                len = text.Length;
            }
        }
        if (pos < 0) return str;
        return str[(pos + len)..];
    }

    /// <summary>
    /// 获取处于两个子字符串之间的部分，裁切尽可能多的内容。
    /// 等效于 AfterLast 后接 BeforeFirst。
    /// 如果未找到子字符串则不裁切。
    /// </summary>
    public static string Between(this string str, string after, string before, bool ignoreCase = false) {
        int startPos = string.IsNullOrEmpty(after) ? -1 : str.LastIndexOfF(after, ignoreCase);
        if (startPos >= 0) {
            startPos += after.Length;
        } else {
            startPos = 0;
        }

        int endPos = string.IsNullOrEmpty(before) ? -1 : str.IndexOfF(before, startPos, ignoreCase);
        if (endPos >= 0) {
            return str[startPos..endPos];
        } else if (startPos > 0) {
            return str[startPos..];
        } else {
            return str;
        }
    }

    #endregion

    #region 替换

    /// <summary>
    /// 替换字符串中的内容。
    /// 仅当需要替换时，才调用 <paramref name="newValueGetter"/> 获取结果字符串。
    /// </summary>
    public static string Replace(this string str, string oldValue, Func<string> newValueGetter) 
        => str.Contains(oldValue) ? str.Replace(oldValue, newValueGetter()) : str;

    /// <summary>
    /// 将字符串中的换行符统一替换为指定字符串。
    /// 若指定了 <paramref name="mergeMultiple"/>，会将多个连续换行符仅替换为一个目标内容。
    /// </summary>
    public static string ReplaceLineEndings(this string input, string newValue, bool mergeMultiple = false) 
        => (mergeMultiple ? regexLineEndingAndMerge : regexLineEnding).Replace(input, newValue.Replace("$", "$$")); // 避免识别成捕获组
    private static readonly Regex regexLineEndingAndMerge = new(@"(?:\r\n|[\n\r\f\u0085\u2028\u2029])+", RegexOptions.Compiled);
    private static readonly Regex regexLineEnding = new(@"\r\n|[\n\r\f\u0085\u2028\u2029]", RegexOptions.Compiled);

    #endregion

    #region 正则

    /// <summary>
    /// 搜索字符串中所有的正则匹配项。
    /// </summary>
    public static IEnumerable<string> RegexSearch(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            return Regex.Matches(str, pattern, options).Cast<Match>().Select(m => m.Value);
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexSearch)} 失败（{pattern}）");
            return [];
        }
    }

    /// <summary>
    /// 获取字符串中的第一个正则匹配项，若无匹配或失败则返回 <see langword="null"/>。
    /// </summary>
    public static string? RegexSeek(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            var match = Regex.Match(str, pattern, options);
            return match.Success ? match.Value : null;
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexSeek)} 失败（{pattern}）");
            return null;
        }
    }

    /// <summary>
    /// 检查字符串是否匹配某正则模式。
    /// </summary>
    public static bool RegexCheck(this string str, string pattern, RegexOptions options = RegexOptions.None) {
        try {
            return Regex.IsMatch(str, pattern, options);
        } catch (Exception ex) {
            Logger.Warn(ex, $"{nameof(RegexCheck)} 失败（{pattern}）");
            return false;
        }
    }

    /// <summary>
    /// 进行正则替换。
    /// 失败时会抛出异常。
    /// </summary>
    public static string RegexReplace(this string allContents, string searchRegex, string replaceTo, RegexOptions options = RegexOptions.None) {
        return Regex.Replace(allContents, searchRegex, replaceTo, options);
    }

    /// <summary>
    /// 对每个正则匹配分别进行替换。
    /// 失败时会抛出异常。
    /// </summary>
    public static string RegexReplace(this string allContents, string searchRegex, MatchEvaluator replaceTo, RegexOptions options = RegexOptions.None) {
        return Regex.Replace(allContents, searchRegex, replaceTo, options);
    }

    #endregion

    /// <summary>
    /// 读取 <see cref="Stream"/> 中的内容，并解码为字符串。
    /// 会将流的位置主动重置到开头。
    /// </summary>
    public static string ReadString(this Stream stream, Encoding? encoding = null) {
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek && stream.Position != 0) stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray().GetString(encoding);
    }

    /// <summary>
    /// 将 <paramref name="bytes"/> 解码为字符串。
    /// </summary>
    public static string GetString(this byte[] bytes, Encoding? encoding = null) {
        if (encoding is not null) return encoding.GetString(bytes);
        int length = bytes.Length;
        // 根据 BOM 判断编码
        if (length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return Encoding.UTF8.GetString(bytes, 3, length - 3); // 带 BOM 的 UTF8
        } else if (length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, length - 2);
        } else if (length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode.GetString(bytes, 2, length - 2);
        }
        // 无 BOM 文件：GB18030（ANSI）或 UTF8
        try {
            return new UTF8Encoding(false, true).GetString(bytes); // 不带 BOM 的 UTF8
        } catch (DecoderFallbackException) {
            return Encoding.GetEncoding("GB18030").GetString(bytes);
        }
    }

    /// <summary>
    /// 将第一个字符转换为大写，其余字符转换为小写。
    /// </summary>
    public static string Capitalize(this string word) {
        if (string.IsNullOrEmpty(word)) return word;
        return $"{word[..1].Upper()}{word[1..].Lower()}";
    }

    /// <summary>
    /// 将字符串统一至某个长度。
    /// 过短则用 <paramref name="code"/> 将其左侧填充，过长则截取靠左的指定长度。
    /// </summary>
    public static string EnsureLength(this string? str, char code, int length) {
        str ??= "";
        return str.Length > length ? str[..length] : str.PadLeft(length, code);
    }

    /// <summary>
    /// 该字符串中的字符是否均为 ASCII 字符。
    /// </summary>
    public static bool IsAsciiOnly(this string input) 
        => input.All(c => c < 128);

    /// <summary>
    /// 计算字符串的哈希，其结果在不同环境中均能保持一致。
    /// </summary>
    public static ulong GetStableHashCode(this string str) {
        ulong result = 5381;
        foreach (char v in str) result = (result << 5) ^ result ^ v;
        return result ^ 0xA98F501BC684032FUL;
    }

    /// <summary>
    /// 将字符串转化为 JSON 对象，若失败则抛出异常。
    /// </summary>
    public static JToken? DeserializeJson(this string data) {
        try {
            return JsonConvert.DeserializeObject<JToken>(data, new JsonSerializerSettings {DateTimeZoneHandling = DateTimeZoneHandling.Local});
        } catch (Exception ex) {
            int length = (data ?? "").Length;
            throw new FormatException("JSON deserialize failed: " + 
                (length > 2000
                    ? $"{data![..500]}...(total {length} characters)...{data[^500..]}"
                    : (data ?? "")), ex);
        }
    }
    /// <summary>
    /// 将字符串转化为 JSON 对象，若失败则抛出异常。
    /// </summary>
    public static T? DeserializeJson<T>(this string data) where T : JToken 
        => (T?) DeserializeJson(data);

}
