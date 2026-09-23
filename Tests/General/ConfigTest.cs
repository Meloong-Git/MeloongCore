namespace MeloongCore.Tests;
public class ConfigTest : TestWithFolder {

    #region JsonConfigProvider

    [Test]
    public async Task JsonConfigProvider_字符串列表() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<List<string>>("list", [], provider: provider);

        entry.Set(["Alpha", "中文", "123"]);
        provider.Save();

        var reloadedEntry = new ConfigEntry<List<string>>("list", [], provider: new JsonConfigProvider(configFile));
        var reloadedValue = reloadedEntry.Get();
        await Assert.That(reloadedValue is not null).IsTrue();
        await Assert.That(string.Join("|", reloadedValue!)).IsEqualTo("Alpha|中文|123");
        await Assert.That(FileUtils.ReadAsString(configFile)).Contains("\"list\": [");
    }

    [Test]
    public async Task JsonConfigProvider_自定义类列表() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<List<TestConfigItem>>("items", [], provider: provider);

        entry.Set([
            new() { Name = "Fabric", Count = 2, Enabled = true },
            new() { Name = "Forge 中文", Count = 5, Enabled = false }
        ]);
        provider.Save();

        var reloadedEntry = new ConfigEntry<List<TestConfigItem>>("items", [], provider: new JsonConfigProvider(configFile));
        var reloadedValue = reloadedEntry.Get();
        await Assert.That(reloadedValue is not null).IsTrue();
        await Assert.That(string.Join("|", reloadedValue!.Select(i => $"{i.Name}:{i.Count}:{i.Enabled}"))).IsEqualTo("Fabric:2:True|Forge 中文:5:False");

        string json = FileUtils.ReadAsString(configFile);
        await Assert.That(json).Contains("\"items\": [");
        await Assert.That(json).Contains("\"Name\": \"Fabric\"");
        await Assert.That(json).Contains("\"Count\": 2");
        await Assert.That(json).Contains("\"Enabled\": true");
        await Assert.That(json).Contains("\"Name\": \"Forge 中文\"");
        await Assert.That(json).Contains("\"Enabled\": false");
    }

    [Test]
    public async Task ConfigEntry_SetNull_RoundTripsThroughJson() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("nullable", "fallback", provider: provider);

        entry.Set(null);
        provider.Save();

        var reloadedEntry = new ConfigEntry<string>("nullable", "fallback", provider: new JsonConfigProvider(configFile));
        await Assert.That(reloadedEntry.HasValue()).IsTrue();
        await Assert.That(reloadedEntry.Get() is null).IsTrue();
        await Assert.That(FileUtils.ReadAsString(configFile)).Contains("\"nullable\": null");
    }

    [Test]
    public async Task ConfigEntry_SetString_RoundTripsThroughJson() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("text", "fallback", provider: provider);

        entry.Set("plain text 中文");
        provider.Save();

        var reloadedEntry = new ConfigEntry<string>("text", "fallback", provider: new JsonConfigProvider(configFile));
        await Assert.That(reloadedEntry.Get()).IsEqualTo("plain text 中文");
        await Assert.That(FileUtils.ReadAsString(configFile)).Contains("\"text\": \"plain text 中文\"");
    }

    [Test]
    public async Task ConfigUtils_SaveAll_WritesPendingChangesImmediately() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("text", "fallback", provider: provider);

        entry.Set("pending 中文");
        ConfigUtils.SaveAll();
        ConfigUtils.SaveAll();

        var reloadedEntry = new ConfigEntry<string>("text", "fallback", provider: new JsonConfigProvider(configFile));
        await Assert.That(reloadedEntry.Get()).IsEqualTo("pending 中文");
        await Assert.That(FileUtils.ReadAsString(configFile)).Contains("\"text\": \"pending 中文\"");
    }

    [Test]
    public async Task JsonConfigProvider_Save_原子替换且不残留临时文件() {
        string configFile = Path.Combine(tempFolder, "config.json");
        FileUtils.Write(configFile, "{\"existing\":true}");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("text", "", provider: provider);

        entry.Set("updated");
        provider.Save();

        Newtonsoft.Json.Linq.JObject config = FileUtils.ReadAsJson<Newtonsoft.Json.Linq.JObject>(configFile)!;
        await Assert.That(config.Value<bool>("existing")).IsTrue();
        await Assert.That(config.Value<string>("text")).IsEqualTo("updated");
        await Assert.That(DirectoryUtils.EnumerateFiles(tempFolder, searchPattern: "config.json.*.tmp")).IsEmpty();
    }

    [Test]
    public async Task JsonConfigProvider_Save_等待正在进行的保存() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("text", "", provider: provider);
        using var firstStarted = new ManualResetEventSlim(false);
        using var secondStarted = new ManualResetEventSlim(false);
        Task firstSave, secondSave;
        bool firstBlocked, secondRunning, returnedEarly;

        lock (provider) {
            entry.Set("pending");
            Thread? firstThread = null;
            firstSave = Task.Factory.StartNew(() => {
                firstThread = Thread.CurrentThread;
                firstStarted.Set();
                provider.Save();
            }, TaskCreationOptions.LongRunning);
            // 阻止首次保存写盘，稳定复现第二次 Save 在写盘完成前返回的窗口。
            firstBlocked = firstStarted.Wait(3000) && SpinWait.SpinUntil(
                () => (firstThread!.ThreadState & ThreadState.WaitSleepJoin) != 0, 3000);
            secondSave = Task.Factory.StartNew(() => {
                secondStarted.Set();
                provider.Save();
            }, TaskCreationOptions.LongRunning);
            secondRunning = secondStarted.Wait(3000);
            returnedEarly = secondSave.Wait(200);
        }
        await Task.WhenAll(firstSave, secondSave);

        await Assert.That(firstBlocked).IsTrue();
        await Assert.That(secondRunning).IsTrue();
        await Assert.That(returnedEarly).IsFalse();
        var reloadedEntry = new ConfigEntry<string>("text", "", provider: new JsonConfigProvider(configFile));
        await Assert.That(reloadedEntry.Get()).IsEqualTo("pending");
    }

    [Test]
    public async Task JsonConfigProvider_Save_写入失败后可直接重试() {
        string configFile = Path.Combine(tempFolder, "config.json");
        var provider = new JsonConfigProvider(configFile);
        var entry = new ConfigEntry<string>("text", "", provider: provider);
        entry.Set("before");
        provider.Save();

        lock (provider) {
            entry.Set("after");
            using (var stream = new FileStream(PathUtils.ForApi(configFile), FileMode.Open, FileAccess.Read, FileShare.None)) {
                Assert.Throws<IOException>(() => provider.Save());
            }
            // 无需再次 Set，失败的内容仍应处于待保存状态。
            provider.Save();
        }

        var reloadedEntry = new ConfigEntry<string>("text", "", provider: new JsonConfigProvider(configFile));
        await Assert.That(reloadedEntry.Get()).IsEqualTo("after");
    }

    public class TestConfigItem {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public bool Enabled { get; set; }
    }

    #endregion

}
