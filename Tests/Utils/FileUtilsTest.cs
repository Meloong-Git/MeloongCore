namespace MeloongCore.Tests;
public class FileUtilsTest : TestWithFolder {

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
