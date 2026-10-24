using MeloongCore.Utils;
using System.Net.Http;

namespace MeloongCore.Tests.Utils;

public class WindowsProxyTest: TestBase {
    [Test]
    [Category("Proxy")]
    public async Task TestProxyWorking() {
        using var proxy = new WindowsProxy();
        using var client = new HttpClient(new HttpClientHandler {
            UseProxy = true,
            Proxy = proxy
        });
        Console.WriteLine(await client.GetStringAsync("https://www.20260712.xyz"));
    }

    [Test]
    [Category("Proxy")]
    public async Task TestManualProxy() {

        using var proxy = new WindowsProxy(ProxyWorkingMode.Manual);
        proxy.RefreshOnce("http://127.0.0.1:9000", []);
        using var client = new HttpClient(new HttpClientHandler {
            UseProxy = true,
            Proxy = proxy
        });
        Console.WriteLine(await client.GetStringAsync("https://www.20260712.xyz"));
    }

    [Test]
    [Category("Proxy")]
    public async Task TestDisabledProxy() {

        using var proxy = new WindowsProxy(ProxyWorkingMode.AutoDiscover);
        proxy.Disable = true;
        using var client = new HttpClient(new HttpClientHandler {
            UseProxy = true,
            Proxy = proxy
        });
        Console.WriteLine(await client.GetStringAsync("https://www.20260712.xyz"));
    }
}