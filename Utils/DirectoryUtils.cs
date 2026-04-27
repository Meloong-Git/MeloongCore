namespace MeloongCore;
public static class DirectoryUtils {

    /// <summary>
    /// 确保路径以 / 或 \ 结尾。
    /// </summary>
    public static string ToDirectoryFormat(string folder) => IsDirectoryFormat(folder) ? folder : folder + Path.DirectorySeparatorChar;

    /// <summary>
    /// 检查路径是否以 / 或 \ 结尾。
    /// </summary>
    public static bool IsDirectoryFormat(string path) => path.EndsWithF("/") || path.EndsWithF("\\");

    /// <summary>
    /// 创建文件夹，或文件所在的文件夹。
    /// 支持长路径。
    /// 文件夹已存在时不会抛出异常。
    /// </summary>
    public static void Create(string path, bool isFilePath = false) {
        path = FileUtils.ShortenPath(path);
        if (isFilePath) path = Path.GetDirectoryName(path);
        Directory.CreateDirectory(path);
    }

}
