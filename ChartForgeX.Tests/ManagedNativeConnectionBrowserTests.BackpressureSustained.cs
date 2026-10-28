using System.Xml.Linq;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ManagedNativeConnectionBrowserTests {
    /// <summary>Repeated source/admission/commit ordering, without a throughput, allocation or heap bound.</summary>
    [BrowserTheory]
    [Trait("BrowserSuite", "Sustained")]
    [InlineData(false)] [InlineData(true)]
    public async Task RepeatedNumericAndTypedProducerBurstsKeepOneConfiguredFlightAcrossThreeOwnedLifetimes(bool typed) {
        if (!Enabled) return;
        const int cycles = 3, bursts = 12, inputCount = 100;
        for (var cycle = 0; cycle < cycles; cycle++) {
            var host = new NativeHost(390, VisualThemeMode.Dark, typed);
            await using var session = await BrowserProofFonts.OpenAsync(host.Page, 414, 800);
            var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
            await ConnectLatestWindowAsync(page, host); var basis = "initial";
            for (var burst = 0; burst < bursts; burst++) {
                var offset = burst * 2;
                await page.EvaluateAsync("() => managed.setViewport({x:{minimum:1,maximum:9}})"); await PendingAsync(page, offset + 1);
                await page.EvaluateAsync("count => {for(let index=0;index<count;index++)managed.setViewport({x:{minimum:2+index/1000,maximum:8-index/1000}})}", inputCount);
                await DrainAsync(page);
                Assert.Equal(offset + 1, await page.EvaluateAsync<int>("() => pending.length"));
                Assert.Equal(basis, await RequestFieldAsync(page, offset, "basis"));
                host.ReplaceProducerOffset(burst % 8);
                await ResolveAsync(page, offset); await PendingAsync(page, offset + 2);
                Assert.Equal(basis, await RequestFieldAsync(page, offset + 1, "basis"));
                basis = await ResolveAsync(page, offset + 1); await ChangesAsync(page, burst + 1);
                AssertWindow(await page.EvaluateAsync<System.Text.Json.JsonElement>("() => managed.getXWindow()"), 2.099, 7.901);
            }
            await CaptureAdmissionAsync(page, host, basis, "sustained-" + typed + "-" + cycle, errors, session.ConsoleLog);
            var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
            var downloadedPath = await download.PathAsync() ?? throw new InvalidOperationException("The final sustained native scene did not export.");
            var exported = XDocument.Load(downloadedPath);
            Assert.Contains(exported.Descendants(), node => (string?)node.Attribute("data-cfx-source-point") == "5"
                && (string?)node.Attribute(typed ? "data-cfx-start" : "data-cfx-y") == "48");
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(output)) File.Copy(downloadedPath, Path.Combine(output, "native-admission-sustained-" + typed + "-" + cycle + ".export.svg"), true);
            Assert.Equal(bursts * 2, await page.EvaluateAsync<int>("() => pending.length"));
            Assert.Equal(bursts * 2, await page.EvaluateAsync<int>("() => admissionRequests.length"));
            Assert.Equal(bursts, await page.EvaluateAsync<int>("() => managedChanges.length"));
            Assert.Empty(await page.EvaluateAsync<string[]>("() => managedErrors.map(error=>error.message)"));
            await page.EvaluateAsync("() => managed.dispose()");
            Assert.True(await page.EvaluateAsync<bool>("() => ChartForgeXCharts.get(managed.root)===null"));
            Assert.Empty(errors); AssertNoConsoleErrors(session);
        }
    }
}
