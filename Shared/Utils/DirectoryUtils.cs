namespace MeloongCore;
public static class DirectoryUtils {

    /// <summary>
    /// 创建文件夹，或文件所在的文件夹。
    /// 文件夹已存在时不会抛出异常。
    /// </summary>
    public static void Create(string folder) {
        if (DirectoryUtils.Exists(folder)) return;
        Logger.Trace($"新建文件夹：{folder}");
        Directory.CreateDirectory(PathUtils.ForApi(folder));
    }

    /// <summary>
    /// 在临时文件夹下创建一个随机名称的文件夹，并返回其路径。
    /// </summary>
    public static string CreateRandom() {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        DirectoryUtils.Create(tempDir);
        return tempDir;
    }

    /// <summary>
    /// 判断文件夹是否存在。
    /// </summary>
    public static bool Exists(string folder) 
        => Directory.Exists(PathUtils.ForApi(folder));

    /// <summary>
    /// 获取 <see cref="DirectoryInfo"/> 对象。
    /// </summary>
    public static DirectoryInfo GetInfo(string folder) 
        => new(PathUtils.ForApi(folder));

    /// <summary>
    /// 返回指定路径下的所有文件，不以 \\?\ 开头。
    /// 如果文件夹不存在，返回空列表。
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(string folder, bool includeSubDirectories = false, string searchPattern = "*") {
        if (!DirectoryUtils.Exists(folder)) return [];
        return Directory.EnumerateFiles(PathUtils.ForApi(folder), searchPattern, includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
            Select(PathUtils.RemoveExtendedPrefix);
    }

    /// <summary>
    /// 返回指定路径下的所有文件夹，不以分隔符结尾，不以 \\?\ 开头。
    /// 如果文件夹不存在，返回空列表。
    /// </summary>
    public static IEnumerable<string> EnumerateDirectories(string folder, bool includeSubDirectories = false, string searchPattern = "*") {
        if (!DirectoryUtils.Exists(folder)) return [];
        return Directory.EnumerateDirectories(PathUtils.ForApi(folder), searchPattern, includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
            Select(PathUtils.RemoveExtendedPrefix);
    }

    /// <summary>
    /// 该文件夹是否为空。
    /// 如果文件夹不存在，返回 true。
    /// </summary>
    public static bool IsEmpty(string folder) {
        if (!DirectoryUtils.Exists(folder)) return true;
        return !Directory.EnumerateFileSystemEntries(PathUtils.ForApi(folder)).Any();
    }

    /// <summary>
    /// 复制文件夹。
    /// 若复制自身到自身，则不执行操作；若仅大小写不同，则重命名此文件夹。
    /// </summary>
    public static void Copy(string sourceFolder, string destFolder) {
        sourceFolder = PathUtils.ForCompare(sourceFolder);
        destFolder = PathUtils.ForCompare(destFolder);
        if (string.Compare(sourceFolder, destFolder, ignoreCase: false) == 0) {
            // 复制自身到自身，则不执行操作
            Logger.Trace($"复制文件夹到自身，不执行操作：{sourceFolder} → {destFolder}");
        } else if (string.Compare(sourceFolder, destFolder, ignoreCase: true) == 0) {
            // 路径仅大小写不同，等效于重命名
            Logger.Trace($"复制文件夹到自身，但大小写不同，等效于重命名文件夹：{sourceFolder} → {destFolder}");
            DirectoryUtils.Move(sourceFolder, destFolder);
        } else {
            // 实际的复制
            Logger.Trace($"复制文件夹：{sourceFolder} → {destFolder}");
            foreach (var file in DirectoryUtils.EnumerateFiles(sourceFolder, includeSubDirectories: true).ToList()) FileUtils.Copy(file, file.Replace(sourceFolder, destFolder));
        }
    }

    /// <summary>
    /// 剪切文件夹。
    /// 会创建对应文件夹、覆盖已有的文件夹。
    /// </summary>
    public static void Move(string sourceFolder, string destFolder) {
        SafetyCheck(sourceFolder);
        sourceFolder = PathUtils.ForCompare(sourceFolder);
        destFolder = PathUtils.ForCompare(destFolder);
        if (string.Compare(sourceFolder, destFolder, ignoreCase: false) == 0) {
            // 剪切自身到自身，则不执行操作
            Logger.Trace($"剪切文件夹到自身，不执行操作：{sourceFolder} → {destFolder}");
        } else if (string.Compare(sourceFolder, destFolder, ignoreCase: true) == 0) {
            // 路径仅大小写不同
            Logger.Trace($"剪切文件夹到自身，但大小写不同：{sourceFolder} → {destFolder}");
            var temp = Path.Combine(PathUtils.RemoveLastPart(sourceFolder), Path.GetRandomFileName());
            DirectoryUtils.Move(sourceFolder, temp);
            DirectoryUtils.Move(temp, destFolder);
        } else if (PathUtils.GetDiskName(sourceFolder) == PathUtils.GetDiskName(destFolder)) {
            // 同一磁盘剪切，直接调用 Move（这只修改文件夹名，效率更高）
            Logger.Trace($"剪切文件夹到同一磁盘：{sourceFolder} → {destFolder}");
            DirectoryUtils.Delete(destFolder); // Move 要求此前不存在对应文件夹
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                FileUtils.SetReadOnly(sourceFolder, false);
                Directory.Move(PathUtils.ForApi(sourceFolder), PathUtils.ForApi(destFolder));
            });
        } else {
            // 不同磁盘，必须先复制再删除，这就是我们傻逼微软
            Logger.Trace($"剪切文件夹到不同磁盘：{sourceFolder} → {destFolder}");
            DirectoryUtils.Copy(sourceFolder, destFolder);
            DirectoryUtils.Delete(sourceFolder);
        }
    }

    /// <summary>
    /// 删除文件夹。
    /// <para/>若指定了 <paramref name="toRecycleBin"/>，则会尝试删除到回收站，但如果失败则会回退到永久删除。
    /// </summary>
    /// <returns>
    /// 如果文件夹不存在，返回 <see langword="null"/>。
    /// <para/>如果成功删除到回收站，返回 <see langword="true"/>。
    /// <para/>如果永久删除，返回 <see langword="false"/>。
    /// </returns>
    public static bool? Delete(string folder, bool toRecycleBin = false) {
        if (!DirectoryUtils.Exists(folder)) return null;
        Logger.Trace($"{(toRecycleBin ? "将文件夹删除到回收站" : "删除文件夹")}：{folder}");
        SafetyCheck(folder);
        // 删除到回收站
        if (toRecycleBin) {
            try {
                FileUtils.DeleteToRecycleBin(folder);
                return true;
            } catch (Exception ex) {
                Logger.Warn(ex, $"无法将文件夹删除到回收站，回退到永久删除：{folder}");
            }
        }
        // 永久删除
        DeleteInternal(folder);
        static void DeleteInternal(string folder) {
            try {
                folder = PathUtils.ForApi(folder);
                foreach (string filePath in DirectoryUtils.EnumerateFiles(folder)) FileUtils.Delete(filePath); // 删除文件
                foreach (string str in DirectoryUtils.EnumerateDirectories(folder)) DeleteInternal(str); // 递归删除子文件夹
                Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                    FileUtils.SetReadOnly(folder, false);
                    Directory.Delete(folder, true);
                }); // 最终删除文件夹
            } catch (DirectoryNotFoundException ex) { // #4549，也可能已被其他线程删除
                if (DirectoryUtils.Exists(folder)) {
                    Logger.Warn(ex, $"该文件夹可能为孤立的符号链接，尝试直接删除（{folder}）");
                    FileUtils.SetReadOnly(folder, false);
                    Directory.Delete(folder);
                } else {
                    throw;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 进行安全检查：若尝试操作桌面、文档、下载、Windows、当前程序所在文件夹，则抛出异常。
    /// </summary>
    /// <exception cref="UnauthorizedAccessException" />
    private static void SafetyCheck(string folder) {
        folder = PathUtils.ForCompare(folder);
        if (folder == PathUtils.ForCompare(Path.GetPathRoot(folder)))
            throw new UnauthorizedAccessException($"不应操作磁盘根目录：{folder}");
        if (criticalFolders.Value.Any(f => PathUtils.IsParentOf(folder, f)))
            throw new UnauthorizedAccessException($"不应操作文件夹：{folder}");
    }
    private static readonly Lazy<HashSet<string>> criticalFolders = new(() => new(new[] {
        Paths.Base,
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") // 下载文件夹没有 SpecialFolder 枚举
    }.Where(f => f.Contains(Path.VolumeSeparatorChar)))); // 当缺少某个文件夹时，GetFolderPath 会返回空字符串（#8636）

}
