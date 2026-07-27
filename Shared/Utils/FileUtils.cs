using System.IO.Compression;

namespace MeloongCore;
public static class FileUtils {

    #region 读取

    /// <summary>
    /// 打开指定文件的只读 <see cref="Stream"/>。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static Stream ReadAsStream(string filePath, Type? type = null) {
        if (type is null) {
            return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _ 
                => new FileStream(PathUtils.ForApi(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        } else {
            return type.Assembly.GetManifestResourceStream(filePath) ?? throw new FileNotFoundException($"Embedded resource \"{filePath}\" was not found.");
        }
    }

    /// <summary>
    /// 读取文件中的所有内容。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static byte[] ReadAsBytes(string filePath, Type? type = null) {
        using Stream fs = FileUtils.ReadAsStream(filePath, type); // 不能使用 File.ReadAllBytes，它不指定 FileShare.ReadWrite，会在文件被占用时抛出异常
        using MemoryStream ms = new();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 读取文件中的所有内容。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static string ReadAsString(string filePath, Encoding? encoding = null, Type? type = null) 
        => FileUtils.ReadAsBytes(filePath, type).GetString(encoding);
    /// <summary>
    /// 读取文件中的所有内容。
    /// <para/>若文件不存在或读取失败，返回 <see langword="null"/>，而不是抛出异常。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static string? TryReadAsString(string filePath, Encoding? encoding = null, Type? type = null) {
        try {
            if (type == null && !FileUtils.Exists(filePath)) return null;
            return FileUtils.ReadAsBytes(filePath, type).GetString(encoding);
        } catch {
            return null;
        }
    }

    /// <summary>
    /// 读取文件中的所有内容，并按行分割。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static string[] ReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null, Type? type = null)
        => FileUtils.ReadAsString(filePath, encoding, type).SplitLines(skipEmptyLines);
    /// <summary>
    /// 读取文件中的所有内容，并按行分割。
    /// <para/>若文件不存在或读取失败，返回空数组，而不是抛出异常。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static string[] TryReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null, Type? type = null)
        => FileUtils.TryReadAsString(filePath, encoding, type)?.SplitLines(skipEmptyLines) ?? [];

    /// <summary>
    /// 读取 JSON 文件中的所有内容。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static JToken? ReadAsJson(string filePath, Encoding? encoding = null, Type? type = null)
        => FileUtils.ReadAsString(filePath, encoding, type).DeserializeJson();
    /// <summary>
    /// 读取 JSON 文件中的所有内容。
    /// <para/>若指定了 <paramref name="type"/>，则会改为从该类型的程序集中读取嵌入的资源。
    /// </summary>
    public static T? ReadAsJson<T>(string filePath, Encoding? encoding = null, Type? type = null) where T : JToken 
        => FileUtils.ReadAsString(filePath, encoding, type).DeserializeJson<T>();

    #endregion

    #region 创建 / 写入

    /// <summary>
    /// 创建文件，并将 <paramref name="text" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, string text, Encoding? encoding = null) 
        => FileUtils.Write(filePath, (encoding ?? new UTF8Encoding()).GetBytes(text));
    /// <summary>
    /// 创建文件，并将 <paramref name="content" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, IEnumerable<byte> content) 
        => FileUtils.Write(filePath, [.. content]);
    /// <summary>
    /// 创建文件，并将 <paramref name="content" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, byte[] content) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"写入文件：{filePath}（{content.Length} 字节）");
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            FileUtils.Delete(filePath);
            File.WriteAllBytes(PathUtils.ForApi(filePath), content);
        });
    }

    /// <summary>
    /// 创建文件，并将 <paramref name="stream" /> 写入文件。
    /// 会将流的位置主动重置到开头。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, Stream stream) {
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            FileUtils.Delete(filePath);
            using FileStream fileStream = FileUtils.CreateAsStream(filePath);
            if (stream.CanSeek && stream.Position != 0) stream.Seek(0, SeekOrigin.Begin);
            Logger.Trace($"写入文件：{filePath}（{stream.GetType().Name}{(stream.CanSeek ? $" {stream.Length} 字节" : "")}）");
            stream.CopyTo(fileStream);
        });
    }
    /// <summary>
    /// 创建文件，并打开 <see cref="FileMode.Create"/> 模式的 <see cref="FileStream"/>。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static FileStream CreateAsStream(string filePath) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"创建文件流：{filePath}");
        return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _ => {
            FileUtils.Delete(filePath);
            return new FileStream(PathUtils.ForApi(filePath), FileMode.Create);
        });
    }

    /// <summary>
    /// 在临时文件夹下创建一个随机名称的文件，并返回其路径。
    /// </summary>
    public static string CreateRandom()
        => Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _ 
            => Path.GetTempFileName());

    /// <summary>
    /// 从 <paramref name="type"/> 的程序集中，读取指定 <paramref name="logicalName"/> 的嵌入的资源，写入到 <paramref name="localFilePath"/> 中。
    /// <para/> 如果文件已存在且内容相同，则不会写入。
    /// </summary>
    public static void ExtractResources(string logicalName, string localFilePath, Type type) {
        var content = ReadAsBytes(logicalName, type);
        if (Exists(localFilePath) && CryptographyUtils.ComputeFileHash(localFilePath) == CryptographyUtils.ComputeHash(content)) return;
        Logger.Info($"将资源写入到文件：{logicalName} → {localFilePath}");
        Write(localFilePath, content);
    }

    #endregion

    #region 复制 / 剪切

    /// <summary>
    /// 复制文件。
    /// 会创建对应文件夹、覆盖已有的文件。
    /// 若复制自身到自身，则不执行操作；若仅大小写不同，则重命名此文件。
    /// </summary>
    public static void Copy(string sourceFilePath, string destFilePath) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.CompareOrdinal(sourceFilePath, destFilePath) == 0) {
            // 复制自身到自身，则不执行操作
            Logger.Trace($"复制文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (string.Compare(sourceFilePath, destFilePath, StringComparison.OrdinalIgnoreCase) == 0) {
            // 路径仅大小写不同，等效于重命名
            Logger.Trace($"复制文件到自身，但大小写不同，等效于重命名文件：{sourceFilePath} → {destFilePath}");
            FileUtils.Move(sourceFilePath, destFilePath);
        } else {
            // 实际的复制
            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            Logger.Trace($"复制文件：{sourceFilePath} → {destFilePath}");
            if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ 
                => File.Copy(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath), true));
        }
    }

    /// <summary>
    /// 剪切文件。
    /// 会创建对应文件夹、覆盖已有的文件。
    /// </summary>
    public static void Move(string sourceFilePath, string destFilePath) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.CompareOrdinal(sourceFilePath, destFilePath) == 0) {
            // 剪切自身到自身，则不执行操作
            Logger.Trace($"剪切文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (string.Compare(sourceFilePath, destFilePath, StringComparison.OrdinalIgnoreCase) == 0) {
            // 路径仅大小写不同
            Logger.Trace($"剪切文件到自身，但大小写不同：{sourceFilePath} → {destFilePath}");
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                var temp = Path.Combine(PathUtils.RemoveLastPart(sourceFilePath), Path.GetRandomFileName());
                FileUtils.SetReadOnly(sourceFilePath, false);
                if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
                File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(temp));
                File.Move(PathUtils.ForApi(temp), PathUtils.ForApi(destFilePath));
            });
        } else {
            // 实际的剪切
            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            FileUtils.Delete(destFilePath);
            Logger.Trace($"剪切文件：{sourceFilePath} → {destFilePath}");
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                FileUtils.SetReadOnly(sourceFilePath, false);
                if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
                File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath));
            });
        }
    }

    #endregion

    #region 删除

    /// <summary>
    /// 删除文件。
    /// <para/>若指定了 <paramref name="toRecycleBin"/>，则会尝试删除到回收站，但如果失败则会回退到永久删除。
    /// </summary>
    /// <returns>
    /// 如果文件不存在，返回 <see langword="null"/>。
    /// <para/>如果成功删除到回收站，返回 <see langword="true"/>。
    /// <para/>如果永久删除，返回 <see langword="false"/>。
    /// </returns>
    public static bool? Delete(string filePath, bool toRecycleBin = false) {
        if (!FileUtils.Exists(filePath)) return null;
        Logger.Trace($"{(toRecycleBin ? "将文件删除到回收站" : "删除文件")}：{filePath}");
        // 删除到回收站
        if (toRecycleBin) {
            try {
                DeleteToRecycleBin(filePath);
                return true;
            } catch (Exception ex) {
                Logger.Warn(ex, $"无法将文件删除到回收站，回退到永久删除：{filePath}");
            }
        }
        // 永久删除
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            FileUtils.SetReadOnly(filePath, false);
            File.Delete(PathUtils.ForApi(filePath));
        });
        return false;
    }

    /// <summary>
    /// 将文件或文件夹删除到回收站。
    /// </summary>
    internal static void DeleteToRecycleBin(string target) {
        // 实际的删除方法
        void Run() {
            IShellItem? item = null;
            IFileOperation? op = null;
            try {
                var iid = typeof(IShellItem).GUID;
                Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(PathUtils.RemoveExtendedPrefix(target), IntPtr.Zero, ref iid, out item));
                // ReSharper disable once SuspiciousTypeConversion.Global
                op = (IFileOperation) new FileOperation();
                op.SetOperationFlags(0x0040 | 0x0010 | 0x0004); // FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                op.DeleteItem(item, IntPtr.Zero);
                op.PerformOperations();
                op.GetAnyOperationsAborted(out bool aborted);
                if (aborted) throw new OperationCanceledException("Delete operation was aborted.");
            } finally {
                if (op != null && Marshal.IsComObject(op)) Marshal.FinalReleaseComObject(op);
                if (item != null && Marshal.IsComObject(item)) Marshal.FinalReleaseComObject(item);
            }
        }
        // 在 STA 线程中执行删除方法
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => Run());
        } else {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo? internalEx = null; // 捕获内部异常
            var thread = new Thread(() => { 
                try { 
                    Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException or COMException, action: _ => Run()); 
                } catch (Exception ex) { 
                    internalEx = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex); 
                } 
            }) { IsBackground = true, Name = nameof(DeleteToRecycleBin) };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            internalEx?.Throw();
        }
    }
    // API
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem { }
    [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
    class FileOperation { }
    [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOperation {
        void Advise(IntPtr pfops, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOperationFlags(uint dwOperationFlags);
        void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
        void SetProgressDialog(IntPtr popd);
        void SetProperties(IntPtr pproparray);
        void SetOwnerWindow(IntPtr hwndOwner);
        void ApplyPropertiesToItem(IShellItem psiItem);
        void ApplyPropertiesToItems(IntPtr punkItems);
        void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void RenameItems(IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IntPtr pfopsItem);
        void CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void DeleteItem(IShellItem psiItem, IntPtr pfopsItem);
        void DeleteItems(IntPtr punkItems);
        void NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IntPtr pfopsItem);
        void PerformOperations();
        void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
    }

    #endregion

    #region 压缩 / 解压

    /// <summary>
    /// 以只读模式打开压缩文件。
    /// 会先尝试 UTF8 编码，失败后换用 GB18030 编码。
    /// </summary>
    public static ZipArchive OpenZip(string zipFilePath, CancellationToken c = default) {
        ZipArchive TryOpen(Encoding encoding) {
            Logger.Trace($"尝试以 {encoding.EncodingName} 编码打开压缩包：{zipFilePath}");
            var result = ZipFile.Open(PathUtils.ForApi(zipFilePath), ZipArchiveMode.Read, encoding);
            try {
                _ = result.Entries; // 如果编码有误，会在这里抛出 DecoderFallbackException；如果文件异常，会在这里抛出 InvalidDataException
                return result;
            } catch {
                result.Dispose();
                throw;
            }
        }
        return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), maxAttempts:10, isRetryAllowed: ex => ex is IOException, cancellationToken: c, func: _ => {
            try {
                try { // 尝试两种编码
                    return TryOpen(new UTF8Encoding(false, true));
                } catch (DecoderFallbackException) {
                    return TryOpen(Encoding.GetEncoding("GB18030"));
                }
            } catch (InvalidDataException ex) {
                throw new InvalidDataException($"文件不是压缩包，或者文件已损坏（{zipFilePath}）", ex);
            }
        });
        
    }

    /// <summary>
    /// 尝试根据后缀名判断文件种类并解压文件，支持 gz 与 zip，会尝试将 jar 以 zip 方式解压。
    /// 会自动创建文件夹。会覆盖已有文件，但不会删除多余文件。
    /// </summary>
    /// <param name="p">参数为已完成的总比例（0~1）。</param>
    public static void ExtractToDirectory(string compressionFile, string outputDirectory, CancellationToken c = default, ProgressProvider? p = null) {
        compressionFile = PathUtils.ForApi(compressionFile);
        DirectoryUtils.Create(outputDirectory);
        // 解压 gz（gz 不需要考虑编码）
        if (compressionFile.EndsWithF(".gz", true)) {
            string outFilePath = Path.Combine(outputDirectory, PathUtils.GetFileNameWithoutExtension(compressionFile));
            Logger.Trace($"解压 gz 文件：{compressionFile} → {outFilePath}");
            using var fileStream = FileUtils.ReadAsStream(compressionFile);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            FileUtils.Write(outFilePath, gzipStream);
            p?.Finish();
            return;
        }
        // 解压 zip
        using var archive = FileUtils.OpenZip(compressionFile, c);
        var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList(); // 跳过文件夹条目（ZipArchive 会将文件夹也作为一个 entry，但它们的 Name 为空）
        int totalCount = entries.Count;
        int doneCount = 0;
        Logger.Trace($"解压 zip 文件：{compressionFile} → {outputDirectory}（共 {totalCount} 项）");
        foreach (var entry in entries) {
            c.ThrowIfCancellationRequested();
            doneCount++;
            if (totalCount > 0) p?.Set((double) doneCount / totalCount);
            // ZipSlip 修复
            string outputFilePath = Path.Combine(outputDirectory, entry.FullName);
            if (!PathUtils.IsParentOf(outputDirectory, outputFilePath))
                throw new InvalidDataException($"Zip 文件项 {entry.FullName} 的路径在压缩包之外，这可能导致安全问题");
            // 实际的解压
            using var entryStream = entry.Open();
            FileUtils.Write(outputFilePath, entryStream);
        }
    }

    /// <summary>
    /// 将指定文件夹的内容打包为 zip 文件。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    public static void CreateZipFromDirectory(string outputFullPath, string sourceDirectory) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        Logger.Trace($"将文件夹中的内容压缩为 zip 文件：{sourceDirectory} → {outputFullPath}");
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ 
            => ZipFile.CreateFromDirectory(PathUtils.ForApi(sourceDirectory), PathUtils.ForApi(outputFullPath)));
    }

    /// <summary>
    /// 将多个文件打包为 zip 文件，所有文件都会被放在压缩文件的根目录。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    public static void CreateZipFromFiles(string outputFullPath, params string[] sourceFiles) {
        var sources = new Dictionary<string, string>();
        foreach (var source in sourceFiles) {
            string fileName = PathUtils.GetLastPart(source);
            if (sources.ContainsKey(fileName)) throw new ArgumentException($"尝试将多个同文件名的文件放进压缩包中（{fileName}）", nameof(sourceFiles));
            sources.Add(fileName, source);
        }
        FileUtils.CreateZipFromFiles(outputFullPath, sources);
    }

    /// <summary>
    /// 将多个文件打包为 zip 文件。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    /// <param name="sources">键为 zip 文件下的路径，值为文件的本地路径。</param>
    public static void CreateZipFromFiles(string outputFullPath, IDictionary<string, string> sources) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        using var archive = Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _ 
            => ZipFile.Open(PathUtils.ForApi(outputFullPath), ZipArchiveMode.Create));
        Logger.Trace($"创建 zip 文件：{sources.Count} 个文件 → {outputFullPath}\n{sources.Select(p => $"- {p.Value} → {p.Key}").Join('\n')}");
        foreach (var pair in sources) archive.CreateEntryFromFile(PathUtils.ForApi(pair.Value), pair.Key.Replace('\\', '/'), CompressionLevel.Optimal);
    }

    #endregion

    /// <summary>
    /// 确定指定的文件是否存在。
    /// </summary>
    public static bool Exists(string filePath) 
        => File.Exists(PathUtils.ForApi(filePath));

    /// <summary>
    /// 获取 <see cref="FileInfo"/> 对象。
    /// </summary>
    public static FileInfo GetInfo(string path) 
        => new(PathUtils.ForApi(path));

    /// <summary>
    /// 设置文件或文件夹的只读属性。
    /// 对文件夹使用时，只会设置文件夹本身的属性，不会递归设置其中的文件或子文件夹。
    /// </summary>
    public static void SetReadOnly(string path, bool readOnly) {
        path = PathUtils.ForApi(path);
        var attributes = File.GetAttributes(path);
        var newAttributes = readOnly
            ? attributes | FileAttributes.ReadOnly
            : attributes & ~FileAttributes.ReadOnly;
        if (attributes == newAttributes) return;
        Logger.Trace($"{(readOnly ? "添加" : "移除")}只读属性：{path}（原属性：{attributes}，新属性：{newAttributes}）");
        File.SetAttributes(path, newAttributes);
    }

}

/// <summary>
/// 检查文件是否符合校验规则，包括大小、Hash 值、是否为 JSON 文件。
/// </summary>
public class FileChecker {

    /// <summary>
    /// 文件的准确大小。
    /// <para/>不检查则为 -1。
    /// </summary>
    public long ActualSize = -1;

    /// <summary>
    /// 文件的最小大小。
    /// <para/>不检查则为 -1。
    /// </summary>
    public long MinSize = -1;

    /// <summary>
    /// 文件的 MD5、SHA1、SHA256 或 SHA512。会根据输入字符串的长度自动判断种类。
    /// <para/>不检查则为 <c>null</c>。
    /// </summary>
    public string? Hash = null;

    /// <summary>
    /// 是否要求为 JSON 文件。
    /// </summary>
    public bool IsJson = false;

    /// <summary>
    /// 检查文件。
    /// <para/>若成功则返回 <c>null</c>，失败则返回错误的描述文本（文本不以句号结尾）。
    /// <para/>不会抛出异常。
    /// </summary>
    public string? Check(string localPath) {
        try {
            var info = FileUtils.GetInfo(localPath);
            if (!info.Exists) return "文件不存在：" + localPath;

            long fileSize = info.Length;
            if (ActualSize >= 0 && ActualSize != fileSize) {
                return $"文件大小应为 {ActualSize} B，实际为 {fileSize} B" +
                    (fileSize < 2000 ? "，内容为：" + FileUtils.ReadAsString(localPath) : "");
            }
            if (MinSize >= 0 && MinSize > fileSize) {
                return $"文件大小应至少为 {MinSize} B，实际为 {fileSize} B" +
                    (fileSize < 2000 ? "，内容为：" + FileUtils.ReadAsString(localPath) : "");
            }

            string? hash = Hash;
            if (!string.IsNullOrEmpty(hash)) {
                hash = hash!.Lower();
                switch (hash.Length) {
                    case 32:
                        string md5 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Md5);
                        if (hash != md5) return "文件 MD5 应为 " + hash + "，实际为 " + md5;
                        break;
                    case 40:
                        string sha1 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha1);
                        if (hash != sha1) return "文件 SHA1 应为 " + hash + "，实际为 " + sha1;
                        break;
                    case 64:
                        string sha256 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha256);
                        if (hash != sha256) return "文件 SHA256 应为 " + hash + "，实际为 " + sha256;
                        break;
                    case 128:
                        string sha512 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha512);
                        if (hash != sha512) return "文件 SHA512 应为 " + hash + "，实际为 " + sha512;
                        break;
                    default:
                        return $"无法识别的 Hash 类型（长度为 {hash.Length}）：{hash}";
                }
            }

            if (IsJson) {
                string content = FileUtils.ReadAsString(localPath);
                if (content == "") return "读取到的文件为空";
                content.DeserializeJson();
            }

            return null;
        } catch (Exception ex) {
            Logger.Warn(ex, "检查文件出错");
            return ex.GetDisplay(false);
        }
    }
}
