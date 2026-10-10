namespace MeloongCore.Tests;
public class PathUtilsTests : TestBase {

    #region 长路径

    [Test]
    public async Task 长路径_ToShortPath_输出超过260字符() {
        string root = Path.Combine(Path.GetTempPath(), $"{nameof(PathUtilsTests)}-{Guid.NewGuid():N}");
        string path = root;
        for (int i = 0; i < 40; i++) path = Path.Combine(path, $"D{i:000000}");
        try {
            DirectoryUtils.Create(path);
            string result = PathUtils.ToShortPath(path);
            await Assert.That(result.Length > 260).IsTrue();
            await Assert.That(DirectoryUtils.Exists(result)).IsTrue();
        } finally {
            DirectoryUtils.Delete(root);
        }
    }

    [Test]
    public async Task 长路径_ToShortPath_创建短文件名() {
        string root = Path.Combine(Path.GetTempPath(), $"{nameof(PathUtilsTests)}-{Guid.NewGuid():N}");
        string path = Path.Combine(root, new string('A', 220), "versions", "test");
        try {
            DirectoryUtils.Create(path);
            string result = PathUtils.ToShortPath(path);
            await Assert.That(result.Length <= 200).IsTrue();
            await Assert.That(result.Length < path.Length).IsTrue();
            await Assert.That(DirectoryUtils.Exists(result)).IsTrue();
        } finally {
            DirectoryUtils.Delete(root);
        }
    }

    #endregion

    #region 路径处理

    [Test]
    [Arguments(@"\\?\C:\foo\bar.txt", @"\\?\C:\foo")]
    [Arguments(@"C:\foo\extra/bar.txt", @"C:\foo\extra")]
    [Arguments(@"C:\foo/extra/bar.txt", @"C:\foo/extra")]
    [Arguments(@"C:\foo/extra\bar.txt", @"C:\foo/extra")]
    [Arguments(@"C:\foo\bar.txt", @"C:\foo")]
    [Arguments(@"\\?\C:\foo\Bar", @"\\?\C:\foo")]
    [Arguments(@"C:\foo\bar", @"C:\foo")]
    [Arguments(@"C:\Foo\bar\", @"C:\Foo")]
    [Arguments(@"\\?\C:\foo\", @"\\?\C:")]
    [Arguments(@"C:\foo", @"C:")]
    [Arguments(@"C:\foo\", @"C:")]
    [Arguments(@"https://foo.bar/file\long/pack.zip?arg=1", @"https://foo.bar/file\long")]
    [Arguments(@"https://foo.bar/file\long\pack.zip?arg=1", @"https://foo.bar/file\long")]
    [Arguments(@"https://foo.bar/file/long\pack.zip?arg=1", @"https://foo.bar/file/long")]
    [Arguments(@"https://foo.bar/file/long/pack.zip?arg=1", @"https://foo.bar/file/long")]
    [Arguments(@"https://foo.bar/file/", "https://foo.bar")]
    [Arguments(@"https://foo.bar/file/?arg=1", "https://foo.bar")]
    [Arguments(@"https://foo.bar/file?arg=1", "https://foo.bar")]
    public async Task 路径处理_RemoveLastPart(string input, string expected)
        => await Assert.That(PathUtils.RemoveLastPart(input)).IsEqualTo(expected);

    [Test]
    [Arguments(@"\\?\C:\foo\bar.txt", "bar.txt")]
    [Arguments(@"C:\foo\extra/bar.txt", "bar.txt")]
    [Arguments(@"C:\foo/extra/bar.txt", "bar.txt")]
    [Arguments(@"C:\foo/extra\bar.txt", "bar.txt")]
    [Arguments(@"C:\foo\bar.txt", "bar.txt")]
    [Arguments(@"C:\foo\bar.txt", "bar.txt")]
    [Arguments(@"\\?\C:\foo/bar", "bar")]
    [Arguments(@"C:\foo\bar", "bar")]
    [Arguments(@"C:\foo\bar\", "bar")]
    [Arguments(@"\\?\C:\foo\", "foo")]
    [Arguments(@"C:\foo", "foo")]
    [Arguments(@"C:\foo\", "foo")]
    [Arguments(@"https://foo.bar/file\long/pack.zip?arg=1", "pack.zip")]
    [Arguments(@"https://foo.bar/file\long\pack.zip?arg=1", "pack.zip")]
    [Arguments(@"https://foo.bar/file/long\pack.zip?arg=1", "pack.zip")]
    [Arguments(@"https://foo.bar/file/long/pack.zip?arg=1", "pack.zip")]
    [Arguments(@"https://foo.bar/file/", "file")]
    [Arguments(@"https://foo.bar/file/?arg=1", "file")]
    [Arguments(@"https://foo.bar/File?arg=1", "File")]
    [Arguments(@"https://cdn.modrinth.com/data/RvfAlf4Z/versions/XauQUBeR\Redstone Tweaks 2.5.3.zip?mr_download_reason=modpack", "Redstone Tweaks 2.5.3.zip")]
    public async Task 路径处理_GetLastPart(string input, string expected)
        => await Assert.That(PathUtils.GetLastPart(input)).IsEqualTo(expected);

    [Test]
    [Arguments(@"\\?\C:\foo\bar.txt", "bar")]
    [Arguments(@"C:\foo\bar.2.txt", "bar.2")]
    [Arguments(@"\\?\C:\foo\bar", "bar")]
    [Arguments(@"C:\foo\bar", "bar")]
    [Arguments(@"C:\foo.bar\how/long\path.txt", "path")]
    [Arguments(@"C:\foo.bar\how/long/path.txt", "path")]
    [Arguments(@"C:\foo.bar\how\long/path.txt", "path")]
    [Arguments(@"C:\foo.bar\how\", "how")]
    [Arguments(@"\\?\C:\foo\", "foo")]
    [Arguments(@"C:\foo", "foo")]
    [Arguments(@"C:\foo\", "foo")]
    [Arguments(@"cREate.jar.disabled", "cREate.jar")]
    [Arguments(@"create.jar", "create")]
    [Arguments(@"create", "create")]
    [Arguments(@"https://foo.bar/file\long/pack.zip?arg=1", "pack")]
    [Arguments(@"https://foo.bar/file\long\pack.zip?arg=1", "pack")]
    [Arguments(@"https://foo.bar/file/long\pack.zip?arg=1", "pack")]
    [Arguments(@"https://foo.bar/file/long/pack.zip?arg=1", "pack")]
    [Arguments(@"https://foo.bar/file/page.xaml.vb?arg=1", "page.xaml")]
    [Arguments(@"https://foo.bar/some.how/file/", "file")]
    [Arguments(@"https://foo.bar/file/?arg=1", "file")]
    [Arguments(@"https://foo.bar/file?arg=1", "file")]
    public async Task 路径处理_GetFileNameWithoutExtension(string input, string expected)
        => await Assert.That(PathUtils.GetFileNameWithoutExtension(input)).IsEqualTo(expected);

    [Test]
    [Arguments(@"\\?\C:\FOO\BAR.TXT", "txt")]
    [Arguments(@"C:\foo\bar.txt", "txt")]
    [Arguments(@"C:\foo.bar\how/long\path.txt", "txt")]
    [Arguments(@"C:\foo.bar\how/long/path.txt", "txt")]
    [Arguments(@"C:\foo.bar\how\long/path.txt", "txt")]
    [Arguments(@"\\?\C:\foo\bar", "")]
    [Arguments(@"C:\foo\bar", "")]
    [Arguments(@"C:\foo\bar\", "")]
    [Arguments(@"\\?\C:\foo\", "")]
    [Arguments(@"C:\foo", "")]
    [Arguments(@"C:\foo\", "")]
    [Arguments("create.jar.diSAbled", "disabled")]
    [Arguments(@"https://foo.bar/file\long/pack.zip?arg=1", "zip")]
    [Arguments(@"https://foo.bar/file\long\pack.zip?arg=1", "zip")]
    [Arguments(@"https://foo.bar/file/long\pack.zip?arg=1", "zip")]
    [Arguments("https://foo.bar/file/page.xaml.vb?arg=1", "vb")]
    [Arguments("https://foo.bar/file/long/pack.ZIP?arg=1", "zip")]
    [Arguments("https://foo.bar/file/", "")]
    [Arguments("https://foo.bar/file/?arg=1", "")]
    [Arguments("https://foo.bar/file?arg=1", "")]
    public async Task 路径处理_GetExtension(string input, string expected)
        => await Assert.That(PathUtils.GetExtension(input)).IsEqualTo(expected);

    #endregion

}
