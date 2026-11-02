using System.IO.Compression;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveBundleTests {
    [Theory]
    [InlineData(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips, 96 * 1024, 30 * 1024)]
    [InlineData(HtmlChartRuntime.Standalone, ChartInteractionFeatures.ReportReview, 280 * 1024, 82 * 1024)]
    [InlineData(HtmlChartRuntime.Full, ChartInteractionFeatures.ReportReview, 337 * 1024, 100 * 1024)]
    public void EmittedRuntimeFitsRawAndGzipBudgets(HtmlChartRuntime runtime, ChartInteractionFeatures features, int rawBudget, int gzipBudget) {
        var asset = HtmlInteractiveAssetFiles.ChartScriptFor(runtime, features);
        var bytes = Encoding.UTF8.GetBytes(asset.Content);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true)) gzip.Write(bytes);
        Assert.True(bytes.Length <= rawBudget, $"{runtime}/{features}: {bytes.Length} raw bytes exceeds {rawBudget}.");
        Assert.True(output.Length <= gzipBudget, $"{runtime}/{features}: {output.Length} gzip bytes exceeds {gzipBudget}.");

        var page = Chart.Create().AddLine("Requests", ChartPoints.FromValues(2, 7, 4)).ToInteractiveHtmlPage(options => {
            options.Runtime = runtime;
            options.Interaction.Features = features;
        });
        Assert.Contains(asset.Content, page);
        Assert.DoesNotContain("<script src=", page);
        if (runtime == HtmlChartRuntime.Full) Assert.Equal(HtmlInteractiveChartRenderer.BuildInteractionScript(), asset.Content);
        if (runtime == HtmlChartRuntime.Standalone && features == ChartInteractionFeatures.Tooltips) {
            Assert.DoesNotContain("ChartForgeX.nativeTextExport", asset.Content);
            Assert.DoesNotContain("connectNative", asset.Content);
            Assert.True(bytes.Length < Encoding.UTF8.GetByteCount(HtmlInteractiveAssetFiles.ChartScript.Content) / 3);
        }
    }

    [Fact]
    public void ExternalReferencesAndFragmentsUseTheSameDeclaredRuntime() {
        var chart = Chart.Create().AddLine("Requests", ChartPoints.FromValues(2, 7, 4));
        var core = HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips);
        var page = chart.ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.ExternalAssets = new HtmlAssetReferences("assets");
        });
        Assert.Contains(core.FileName, page);
        Assert.Equal(core.Content, HtmlInteractiveAssetFiles.Charts(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips)[1].Content);
        Assert.DoesNotContain(HtmlInteractiveAssetFiles.ChartScript.FileName, page);
        Assert.Contains(core.Content, chart.ToInteractiveHtmlFragment(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
        }));
        Assert.Equal(HtmlInteractiveAssetFiles.ChartScript.Content,
            HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips, adaptive: true).Content);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedStandalonePageRunsDeclaredInteractionsWithNetworkBlocked(bool review) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 360).WithXLabels("Mon", "Tue", "Wed")
            .AddLine("Requests", ChartPoints.FromValues(2, 7, 4))
            .AddLine("Previous", ChartPoints.FromValues(1, 3, 2));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = review ? ChartInteractionFeatures.ReportReview : ChartInteractionFeatures.Tooltips;
            options.TextPresentation = HtmlChartTextPresentation.Browser;
        });
        var requests = new List<string>();
        await using var session = await OpenAsync(html, configurePageAsync: async page => {
            page.Request += (_, request) => requests.Add(request.Url);
            await page.RouteAsync("**/*", route => route.AbortAsync());
        });
        var browser = session.Page;
        await MoveToAsync(browser, Point(0, 1));
        Assert.True(await browser.Locator(".cfx-tooltip").IsVisibleAsync());
        Assert.Contains("Tue", await TooltipTextAsync(browser));
        if (review) {
            await browser.Locator(Point(0, 1)).ClickAsync();
            Assert.True(await browser.Locator(Point(0, 1)).EvaluateAsync<bool>("node => node.classList.contains('cfx-selected')"));
            await browser.Locator(Legend(0)).ClickAsync();
            Assert.True(await browser.Locator(Legend(0)).EvaluateAsync<bool>("node => node.getAttribute('aria-pressed') === 'false' || node.dataset.cfxMuted === 'true' || node.classList.contains('cfx-series-muted')"));
        }
        Assert.Empty(requests);
        Assert.False(await browser.EvaluateAsync<bool>("() => !!window.ChartForgeXCharts"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task MixedFragmentRuntimesUpgradeReadersAndKeepOneOwnerPerRoot() {
        if (!Enabled) return;
        var html = Chart.Create().WithSize(600, 360).AddLine("Requests", ChartPoints.FromValues(2, 7, 4))
            .ToInteractiveHtmlFragment(options => {
                options.Runtime = HtmlChartRuntime.Standalone;
                options.Interaction.Features = ChartInteractionFeatures.Tooltips;
                options.TextPresentation = HtmlChartTextPresentation.Browser;
            });
        await using var session = await OpenAsync(html);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
              const root = document.querySelector('.cfx-interactive-chart');
              window.hoverCount = 0;
              root.addEventListener('cfxhover', () => window.hoverCount++);
            }
            """);
        async Task AssertOneHoverAsync(string? readerKind) {
            var result = await page.EvaluateAsync<int>("""
                selector => {
                  window.hoverCount = 0;
                  document.querySelector(selector).dispatchEvent(new PointerEvent('pointerenter', { pointerId: 1 }));
                  return window.hoverCount;
                }
                """, Point(0, 1));
            Assert.True(result == 1, $"{readerKind ?? "full"} runtime emitted {result} hover events; expected one owner per root.");
            Assert.Equal(readerKind, await page.EvaluateAsync<string?>("() => document.querySelector('.cfx-interactive-chart')[Symbol.for('ChartForgeX.chartRuntime')]?.kind ?? null"));
        }
        await AssertOneHoverAsync("core");
        var reader = HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.ReportReview);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = reader.Content });
        await AssertOneHoverAsync("standalone");
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = HtmlInteractiveAssetFiles.ChartScript.Content });
        await AssertOneHoverAsync(null);
        Assert.True(await page.EvaluateAsync<bool>("() => !!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))"));
        foreach (var asset in new[] { reader, HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips) }) {
            await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = asset.Content });
            await AssertOneHoverAsync(null);
        }
        await page.EvaluateAsync("() => { const root = document.querySelector('.cfx-interactive-chart'); ChartForgeXCharts.get(root).dispose(); ChartForgeXCharts.mount(root); }");
        await AssertOneHoverAsync(null);
        AssertNoConsoleErrors(session);
    }
}
