namespace MeloongCore;
public static class PathUtils {

    #region 分隔符

    /// <summary>
    /// 确保路径的末尾包含任意文件夹分隔符。
    /// </summary>
    public static string AddSlashSuffix(string folder) 
        => folder + (folder.EndsWithF(Path.DirectorySeparatorChar) || folder.EndsWithF(Path.AltDirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar);

    /// <summary>
    /// 确保路径的结尾不包含文件夹分隔符。
    /// </summary>
    public static string RemoveSlashSuffix(string folder) 
        => folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    #endregion

    #region 长路径

    /// <summary>
    /// 若路径较长，则尽量将其转换为短路径。
    /// <para/>结果的开头不含 <c>\\?\</c>，结尾不含分隔符。
    /// </summary>
    public static string ToShortPath(string pathName, bool keepFileName = false) {
        if (string.IsNullOrEmpty(pathName)) return pathName;
        if (!pathName.Contains(":")) return pathName;
        if (pathName.Length <= 200) return pathName;
        pathName = PathUtils.ForCompare(pathName);
        // 保留文件名
        string pathToKeep = "";
        string pathToShorten = pathName;
        if (pathName.EndsWithF(".jar", true)) keepFileName = true; // jar 文件的文件名需要保留原样，否则会导致 Forge 1.20.1 无法通过文件名识别模块名
        if (keepFileName && FileUtils.Exists(pathName)) {
            pathToKeep = PathUtils.GetLastPart(pathName);
            pathToShorten = PathUtils.RemoveLastPart(pathName);
        }
        // 逐级向上寻找已存在的文件夹，将不存在的部分挪到 suffix，不再缩短
        while (!DirectoryUtils.Exists(pathToShorten) && !FileUtils.Exists(pathToShorten)) { // 如果路径不存在
            string? parentPath = Path.GetDirectoryName(pathToShorten);
            if (string.IsNullOrEmpty(parentPath) || parentPath == pathToShorten) return pathName; // 已经到达根目录，全都不存在，直接返回
            pathToKeep = Path.Combine(PathUtils.GetLastPart(pathToShorten), pathToKeep);
            pathToShorten = parentPath;
        }
        if (pathToShorten.Length <= 10) return pathName;
        // 实际缩短路径
        char[] buffer = new char[260];
        int result = GetShortPathNameW(PathUtils.ForApi(pathToShorten), buffer, buffer.Length);
        if (result == 0) return pathName;
        string shortPath = new(buffer, 0, result);
        return PathUtils.RemoveSlashSuffix(PathUtils.RemoveExtendedPrefix(Path.Combine(shortPath, pathToKeep)));
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetShortPathNameW(string lpszLongPath, [Out] char[] buffer, int cchBuffer);

    /// <summary>
    /// 将完整路径转换为以 <c>\\?\</c> 开头的扩展格式。
    /// 这会去除路径末尾的分隔符，将 / 替换为 \。
    /// </summary>
    public static string ToExtendedFormat(string pathName) {
        if (string.IsNullOrWhiteSpace(pathName)) return pathName;
        if (!pathName.Contains(@":")) return pathName; // 可能是相对路径，保持不变
        if (pathName.StartsWithF(@"\\?\")) return pathName; // 已经是长路径
        pathName = RemoveSlashSuffix(pathName).Replace('/', '\\'); // API 要求这个格式……
        if (pathName.StartsWithF(@"\\")) {
            return $@"\\?\UNC\{pathName[2..]}";
        } else {
            return $@"\\?\{pathName}";
        }
    }

    /// <summary>
    /// 去除路径开头的 <c>\\?\</c>。
    /// </summary>
    public static string RemoveExtendedPrefix(string pathName) {
        if (pathName.StartsWithF(@"\\?\UNC\")) return @"\\" + pathName.AfterLast(@"\\?\UNC\");
        if (pathName.StartsWithF(@"\\?\")) return pathName.AfterLast(@"\\?\");
        return pathName;
    }

    #endregion

    #region 路径处理

    /// <summary>
    /// 去除路径的最后一级，并移除末尾的分隔符。
    /// <code>
    /// "C:\foo\bar.txt" → "C:\foo"
    /// "C:\foo\bar\" → "C:\foo"
    /// "C:\foo\bar" → "C:\foo"
    /// "C:\foo\" → "C:"
    /// "https://foo.bar/file/pack.zip?arg=1" → "https://foo.bar/file"
    /// </code></summary>
    public static string RemoveLastPart(string pathName) {
        if (pathName.Contains("://")) { // 网络路径
            pathName = PathUtils.RemoveSlashSuffix(pathName.BeforeFirst("#").BeforeFirst("?")); // 去除参数
        }
        return PathUtils.RemoveSlashSuffix(pathName).BeforeLastOfAny([@"\", "/"]);
    }

    /// <summary>
    /// 获取路径的最后一级，即文件名，或当前文件夹名。
    /// <code>
    /// "C:\foo\bar.txt" → "bar.txt"
    /// "C:\foo\bar\" → "bar"
    /// "C:\foo\bar" → "bar"
    /// "https://foo.bar/file/pack.zip?arg=1" → "pack.zip"
    /// </code></summary>
    public static string GetLastPart(string pathName) {
        if (pathName.Contains("://")) { // 网络路径
            pathName = PathUtils.RemoveSlashSuffix(pathName.BeforeFirst("#").BeforeFirst("?")); // 去除参数
        } else { // 文件路径
            pathName = PathUtils.RemoveSlashSuffix(pathName);
        }
        return pathName.AfterLastOfAny([@"\", "/"]);
    }

    /// <summary>
    /// 获取路径或文件名中，仅不包含最后一级扩展名的部分。
    /// <code>
    /// "C:\foo\bar.txt" → "bar"
    /// "create.jar.disabled" → "create.jar"
    /// "https://foo.bar/file/page.xaml.vb?arg=1" → "page.xaml"
    /// </code>
    /// </summary>
    public static string GetFileNameWithoutExtension(string pathName) 
        => GetLastPart(pathName).BeforeLast(".");

    /// <summary>
    /// 获取路径或文件名中的最后一级文件扩展名，不包含 <c>.</c>，固定小写。
    /// 如果没有 <c>.</c> 则返回空字符串。
    /// <code>
    /// "C:\FOO\BAR.TXT" → "txt"
    /// "create.jar.disabled" → "disabled"
    /// "C:\foo\bar" → ""
    /// "https://foo.bar/file/page.xaml.vb?arg=1" → "vb"
    /// </code>
    /// </summary>
    public static string GetExtension(string pathName) {
        var name = GetLastPart(pathName);
        return name.Contains(".") ? name.AfterLast(".").Lower() : "";
    }

    /// <summary>
    /// 获取路径的盘符，返回一个大写字母。
    /// 若路径不包含盘符，则返回 <c>null</c>。
    /// </summary>
    public static char? GetDiskName(string? pathName) {
        if (string.IsNullOrEmpty(pathName)) return null;
        pathName = PathUtils.RemoveExtendedPrefix(pathName!);
        if (pathName.StartsWithF(@"\\") || pathName.StartsWithF("/")) return null;
        return char.ToUpper(pathName[0]);
    }

    /// <summary>
    /// 统一路径格式，以便比较。
    /// <para/>具体而言：将短路径展开，去除前导的 <c>\\?\</c>，将分隔符改为 \，去除末尾的分隔符。
    /// </summary>
    public static string ForCompare(string pathName) {
        try {
            return PathUtils.RemoveSlashSuffix(PathUtils.RemoveExtendedPrefix(Path.GetFullPath(pathName)).Replace("/", @"\"));
        } catch (ArgumentException ex) {
            throw new ArgumentException($"路径不正确：{pathName}", ex);
        }
    }

    /// <summary>
    /// 将路径转换为兼容各种 Windows API 的格式。
    /// <para/>具体而言：将分隔符改为 \。添加前导的 <c>\\?\</c>。若为驱动器则添加末尾分隔符，否则去除分隔符。
    /// </summary>
    public static string ForApi(string pathName) {
        pathName = PathUtils.ToExtendedFormat(pathName);
        if (pathName.EndsWithF(":")) pathName += @"\"; // 驱动器路径必须保留末尾的分隔符
        if (pathName.EndsWithF(@":\")) return pathName;
        return PathUtils.RemoveSlashSuffix(pathName);
    }

    #endregion

    /// <summary>
    /// 判断 <param name="parentPath"/> 是否为 <paramref name="childPath"/> 的父路径，或两者是否相同。
    /// </summary>
    public static bool IsParentOf(string parentPath, string childPath) {
        parentPath = PathUtils.ForCompare(parentPath);
        childPath = PathUtils.ForCompare(childPath);
        return childPath.StartsWithF(parentPath + @"\") || childPath == parentPath;
    }

}

/// <summary>
/// 常用路径合集。
/// 所有路径均以分隔符结尾。
/// </summary>
public static class Paths {
    /// <summary>
    /// 可执行文件所在的文件夹。
    /// </summary>
    public static string Base => PathUtils.AddSlashSuffix(AppContext.BaseDirectory);
    /// <summary>
    /// 可执行文件所在的文件夹中，名为 <see cref="Main.AppName"/> 的子文件夹。
    /// </summary>
    public static string BaseThenName => PathUtils.AddSlashSuffix(Base + Main.AppName);
    /// <summary>
    /// %AppData% 文件夹。
    /// </summary>
    public static string AppData => PathUtils.AddSlashSuffix(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    /// <summary>
    /// %AppData% 文件夹中，名为 <see cref="Main.AppName"/> 的子文件夹。
    /// </summary>
    public static string AppDataThenName => PathUtils.AddSlashSuffix(AppData + Main.AppName);
}