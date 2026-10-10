namespace MeloongCore.Tests;
public class FileUtilsTest : TestWithFolder {

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
