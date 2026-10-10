namespace MeloongCore.Tests;
public class FileUtilsTest : TestWithFolder {

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task 土耳其语_文件同源及仅改大小写保留内容(bool copy) {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        string root = tempFolder;
        string source = Path.Combine(root, "FILE.txt");
        string destination = Path.Combine(root, "file.txt");
        try {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            DirectoryUtils.Create(root);
            FileUtils.Write(source, "文件内容");
            if (copy) {
                FileUtils.Copy(source, source);
                FileUtils.Copy(source, destination);
            } else {
                FileUtils.Move(source, source);
                FileUtils.Move(source, destination);
            }
            await Assert.That(FileUtils.ReadAsString(destination)).IsEqualTo("文件内容");
            await Assert.That(Directory.GetFiles(PathUtils.ForApi(root)).Select(Path.GetFileName).Single()).IsEqualTo("file.txt");
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            DirectoryUtils.Delete(root);
        }
    }

    [Test]
    public async Task 文件可用性_只读与占用() {
        string path = Path.Combine(tempFolder, "available.txt");
        await Assert.That(FileUtils.IsAvailable(path)).IsTrue();
        FileUtils.Write(path, "test");
        string apiPath = PathUtils.ForApi(path);
        File.SetAttributes(apiPath, FileAttributes.ReadOnly);
        try {
            await Assert.That(FileUtils.IsAvailable(path)).IsTrue();
            await Assert.That((File.GetAttributes(apiPath) & FileAttributes.ReadOnly) != 0).IsTrue();
        } finally {
            File.SetAttributes(apiPath, FileAttributes.Normal);
        }
        using (var stream = new FileStream(apiPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
            await Assert.That(FileUtils.IsAvailable(path)).IsFalse();
        }
        await Assert.That(FileUtils.IsAvailable(path)).IsTrue();
    }

    [Test]
    public async Task 文件可用性_短暂占用后重试() {
        string path = Path.Combine(tempFolder, "temporary-lock.txt");
        FileUtils.Write(path, "test");
        using var stream = new FileStream(PathUtils.ForApi(path), FileMode.Open, FileAccess.Read, FileShare.None);
        var release = Task.Run(async () => {
            await Task.Delay(100);
            stream.Dispose();
        });
        bool available = FileUtils.IsAvailable(path);
        await release;
        await Assert.That(available).IsTrue();
    }

    #region 解压

    [Test]
    [Arguments("GB Encoding.zip")]
    [Arguments("UTF8 Encoding.zip")]
    public async Task 解压_ReadZip(string testFile) {
        string output = Path.Combine(tempFolder, "Extracted");
        var p = new ProgressProvider();
        FileUtils.ExtractToDirectory(GetTestFile(testFile), output, p: p);
        await Assert.That(p.IsFinished).IsTrue();
        await Assert.That(DirectoryUtils.Exists(Path.Combine(output, "文件夹"))).IsTrue();
        await Assert.That(DirectoryUtils.Exists(Path.Combine(output, "空文件夹"))).IsFalse();
        await Assert.That(File.ReadAllText(PathUtils.ToExtendedFormat(Path.Combine(output, "fabricloader.log")))).Contains("FabricLoader");
        await Assert.That(File.ReadAllText(PathUtils.ToExtendedFormat(Path.Combine(output, "文件夹", "中文文件.txt")))).Contains("测试内容");
    }

    [Test]
    [Arguments("GZ.gz", "LTCat")]
    public async Task 解压_ReadGz(string testFile, string containsText) {
        string output = Path.Combine(tempFolder, "Extracted");
        var p = new ProgressProvider();
        FileUtils.ExtractToDirectory(GetTestFile(testFile), output, p: p);
        await Assert.That(p.IsFinished).IsTrue();
        await Assert.That(File.ReadAllText(PathUtils.ToExtendedFormat(Path.Combine(output, PathUtils.GetFileNameWithoutExtension(testFile))))).Contains(containsText);
    }

    [Test]
    [Arguments("Corrupted.zip")]
    [Arguments("Not zip.zip")]
    [Arguments("DotDot ZipSlip.zip")]
    [Arguments("AbsPath ZipSlip.zip")]
    public void 解压_ReadBad(string testFile)
        => Assert.Throws<InvalidDataException>(() => FileUtils.ExtractToDirectory(GetTestFile(testFile), tempFolder));

    #endregion

}
