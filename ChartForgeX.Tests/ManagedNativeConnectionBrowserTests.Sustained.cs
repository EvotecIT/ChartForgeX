using System.Xml.Linq;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ManagedNativeConnectionBrowserTests {
    /// <summary>A bounded ordering/lifetime contract, without a throughput or heap-size assertion.</summary>
    [BrowserFact]
    [Trait("BrowserSuite", "Sustained")]
    public async Task RepeatedNativeSupersessionKeepsAcceptedBasisFocusAndExportsAcrossFiveHostLifetimes() {
        if (!Enabled) return;
        const int cycles = 5, bursts = 20, requestsPerBurst = 5;
        var requests = 0; var commits = 0;
        for (var cycle = 0; cycle < cycles; cycle++) {
            var host = new NativeHost(390, VisualThemeMode.Dark);
            await using var session = await BrowserProofFonts.OpenAsync(host.Page, 414, 800);
            var page = session.Page;
            var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
            await ConnectAsync(page, host, "x");
            await page.EvaluateAsync("() => managed.select({targets:[managed.resolveTargets().find(target=>target.annotationId==='note')]})");
            await page.Locator("[data-cfx-annotation-id=note]").FocusAsync();
            var acceptedBasis = "initial";
            for (var burst = 0; burst < bursts; burst++) {
                var offset = burst * requestsPerBurst;
                for (var index = 0; index < requestsPerBurst; index++) {
                    var inset = 1 + burst % 10 * .1 + index * .01;
                    await page.EvaluateAsync("inset => managed.setViewport({x:{minimum:inset,maximum:10-inset}})", inset);
                    requests++;
                }
                await PendingAsync(page, offset + requestsPerBurst);
                for (var index = 0; index < requestsPerBurst; index++)
                    Assert.Equal(acceptedBasis, await RequestFieldAsync(page, offset + index, "basis"));
                Assert.True(await page.EvaluateAsync<bool>("offset => pending.slice(offset,offset+4).every(item=>item.signal.aborted)&&!pending[offset+4].signal.aborted", offset));
                await PrepareReplyAsync(page, offset);
                acceptedBasis = await ResolveAsync(page, offset + 4);
                await ChangesAsync(page, burst + 1); commits++;
                var revision = await page.EvaluateAsync<string>("() => managed.sceneRevision");
                await page.EvaluateAsync("offset => pending[offset].resolve(replies[offset])", offset);
                await DrainAsync(page);
                Assert.Equal(revision, await page.EvaluateAsync<string>("() => managed.sceneRevision"));
                var acceptedInset = 1 + burst % 10 * .1 + .04;
                AssertWindow(await page.EvaluateAsync<System.Text.Json.JsonElement>("() => managed.getXWindow()"), acceptedInset, 10 - acceptedInset);
                Assert.Equal("note", await page.EvaluateAsync<string>("() => managed.getSelection()[0].annotationId"));
                Assert.True(await page.Locator("[data-cfx-annotation-id=note]").EvaluateAsync<bool>("node => node===document.activeElement"));
            }
            var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
            var path = await download.PathAsync() ?? throw new InvalidOperationException("The final accepted native scene did not export.");
            var exported = XDocument.Load(path);
            Assert.Equal(await page.Locator("[data-cfx-role=axis-x-label]").AllTextContentsAsync(), exported.Descendants()
                .Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label").Select(node => node.Value).ToArray());
            Assert.Single(exported.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note");
            Assert.Empty(await page.EvaluateAsync<string[]>("() => managedErrors.map(error=>error.message)"));
            await CaptureAsync(page, "sustained-cycle-" + cycle);
            await page.EvaluateAsync("() => managed.dispose()");
            Assert.True(await page.EvaluateAsync<bool>("() => ChartForgeXCharts.get(managed.root)===null"));
            Assert.Equal(bursts, await page.EvaluateAsync<int>("() => managedChanges.length"));
            Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
        }
        Assert.Equal(500, requests); Assert.Equal(100, commits);
    }
}
