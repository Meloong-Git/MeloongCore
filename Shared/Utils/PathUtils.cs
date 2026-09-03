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
    public static string ToShortPath(string pathName) {
        // 快速返回
        if (string.IsNullOrEmpty(pathName)) return pathName;
        if (!pathName.Contains(":") || pathName.Contains("://")) return pathName;
        if (pathName.Length <= 220) return pathName;
        // 开始实际处理
        pathName = PathUtils.ForCompare(pathName);
        // 尽量保留原始文件名
        string pathToKeep = "";
        string pathToShorten = pathName;
        if (FileUtils.Exists(pathName)) {
            var fileName = PathUtils.GetLastPart(pathName);
            if (fileName.Length >= 180) {
                Logger.Warn($"文件名过长，将为文件名生成短路径：{pathName}", LogBehavior.None); // 部分程序，例如 Forge 的模块加载，依赖于读取文件名进行判断，因此对文件名生成短路径是有风险的
            } else {
                pathToKeep = fileName;
                pathToShorten = PathUtils.RemoveLastPart(pathName);
            }
        }
        // 逐级向上寻找已存在的文件夹，将不存在的部分挪到 suffix，不再缩短
        while (!DirectoryUtils.Exists(pathToShorten) && !FileUtils.Exists(pathToShorten)) { // 如果路径不存在
            string? parentPath = Path.GetDirectoryName(pathToShorten);
            if (string.IsNullOrEmpty(parentPath) || parentPath == pathToShorten) return pathName; // 已经到达根目录，全都不存在，直接返回
            pathToKeep = Path.Combine(PathUtils.GetLastPart(pathToShorten), pathToKeep);
            pathToShorten = parentPath;
        }
        if (pathToKeep.Length >= 200) {
            Logger.Warn($"路径中的大部分内容都尚未建立，无法在内容不存在时缩短路径：{pathName} → {pathToShorten}", LogBehavior.None);
            return pathName;
        }
        // 获取短路径
        Func<string, string> TryGetShortPath = static pathName => {
            char[] buffer = new char[220];
            int result = GetShortPathNameW(PathUtils.ForApi(pathName), buffer, buffer.Length);
            return result is 0 or > 220 ? pathName : new string(buffer, 0, result);
        };
        var shortPath = TryGetShortPath(pathToShorten);
        // 尝试生成新的短名称，以将长度降低到 220 以下（只会在 Windows 没有为其生成过短名称的情况下才会触发，大多数情况都不会进到这里）
        if (Path.Combine(shortPath, pathToKeep).Length > 220) {
            Logger.Warn($"{nameof(GetShortPathNameW)} 未能有效缩短路径，将尝试生成新的短名称：{pathToShorten} → {shortPath}", LogBehavior.None);
            var candidates = new List<(string Path, string Name)>();
            string? candidatePath = pathToShorten;
            while (!string.IsNullOrEmpty(candidatePath)) {
                string candidateName = PathUtils.GetLastPart(candidatePath);
                if (candidateName.Length > 12 &&
                    string.Equals(PathUtils.GetLastPart(TryGetShortPath(candidatePath)), candidateName, StringComparison.OrdinalIgnoreCase)) {
                    candidates.Add((candidatePath, candidateName));
                }
                candidatePath = Path.GetDirectoryName(candidatePath);
            }
            foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Name.Length)) {
                using var handle = CreateFileW(PathUtils.ForApi(candidate.Path), 0x40010000, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero); // GENERIC_WRITE | DELETE；共享读、写、删除；OPEN_EXISTING；FILE_FLAG_BACKUP_SEMANTICS
                if (handle.IsInvalid) continue;
                bool created = false;
                try {
                    unsafe {
                        fixed (char* originalNameBuffer = candidate.Name) {
                            char* shortNameBuffer = stackalloc char[12]; // 8.3 名称最长 12 个 Unicode 字符
                            var name = new UnicodeString {Length = (ushort)(candidate.Name.Length * 2), MaximumLength = (ushort)((candidate.Name.Length + 1) * 2), Buffer = originalNameBuffer};
                            var shortName = new UnicodeString {MaximumLength = 24, Buffer = shortNameBuffer};
                            var context = new GenerateNameContext();
                            for (int i = 0; i < 20; i++) { // 使用 Windows 自身的 8.3 名称生成算法，并通过 context 依次生成避让冲突的候选名
                                shortName.Length = 0;
                                if (RtlGenerate8dot3Name(ref name, 0, ref context, ref shortName) != 0) break;
                                string generatedName = new(shortName.Buffer, 0, shortName.Length / 2);
                                if (SetFileShortNameW(handle, generatedName)) { created = true; break; }
                                if (Marshal.GetLastWin32Error() != 183) break; // ERROR_ALREADY_EXISTS
                            }
                        }
                    }
                } catch (EntryPointNotFoundException) {}
                if (!created) continue;
                shortPath = TryGetShortPath(pathToShorten);
                if (Path.Combine(shortPath, pathToKeep).Length <= 220) break;
            }
        }
        return PathUtils.RemoveSlashSuffix(PathUtils.RemoveExtendedPrefix(Path.Combine(shortPath, pathToKeep)));
    }
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct UnicodeString { public ushort Length, MaximumLength; public char* Buffer; }
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct GenerateNameContext { public ushort Checksum; public byte ChecksumInserted, NameLength; public fixed char NameBuffer[8]; public uint ExtensionLength; public fixed char ExtensionBuffer[4]; public uint LastIndexValue; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] 
    private static extern int GetShortPathNameW(string lpszLongPath, [Out] char[]? buffer, int cchBuffer);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] 
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] 
    private static extern bool SetFileShortNameW(Microsoft.Win32.SafeHandles.SafeFileHandle file, string shortName);
    [DllImport("ntdll.dll")] 
    private static extern int RtlGenerate8dot3Name(ref UnicodeString name, byte allowExtendedCharacters, ref GenerateNameContext context, ref UnicodeString name8dot3);

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
