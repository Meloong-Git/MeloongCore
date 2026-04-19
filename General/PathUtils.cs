using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PCLCS {
    public static class PathUtils {

        /// <summary>
        /// 若路径较长，则尽量将其转换为短路径。
        /// 若输入的是文件夹路径，不保证其结尾是否有 \ 。
        /// </summary>
        public static string Shorten(string fullName, bool keepFileName = false) {
            if (string.IsNullOrEmpty(fullName) || fullName.Length <= 200) return fullName;
            fullName = fullName.Replace('/', '\\');

            // 保留文件名
            string pathToKeep = "";
            string pathToShorten = fullName;
            if (fullName.EndsWithF(".jar", true)) keepFileName = true; // jar 文件的文件名需要保留原样，否则会导致 Forge 1.20.1 无法通过文件名识别模块名
            if (keepFileName && File.Exists(fullName)) {
                pathToKeep = Path.GetFileName(fullName);
                pathToShorten = Path.GetDirectoryName(fullName);
            }

            // 逐级向上寻找已存在的文件夹，将不存在的部分挪到 suffix，不再缩短
            while (!Directory.Exists(pathToShorten) && !File.Exists(pathToShorten)) { // 如果路径不存在
                string parentPath = Path.GetDirectoryName(pathToShorten);
                if (string.IsNullOrEmpty(parentPath) || parentPath == pathToShorten) return fullName; // 已经到达根目录，全都不存在，直接返回
                pathToKeep = Path.Combine(Path.GetFileName(pathToShorten), pathToKeep);
                pathToShorten = parentPath;
            }
            if (pathToShorten.Length <= 10) return fullName;

            // 缩短路径
            StringBuilder buffer = new StringBuilder(260);
            if (GetShortPathNameW(pathToShorten, buffer, buffer.Capacity) == 0) return fullName;
            return Path.Combine(buffer.ToString(), pathToKeep);
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetShortPathNameW(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

    }
}
