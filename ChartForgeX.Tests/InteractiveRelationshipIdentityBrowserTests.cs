using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveRelationshipIdentityBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Tree, false, 360)]
    [InlineData(ChartSeriesKind.Tree, true, 360)]
    [InlineData(ChartSeriesKind.Tree, false, 800)]
    [InlineData(ChartSeriesKind.Tree, true, 800)]
    [InlineData(ChartSeriesKind.Sunburst, false, 360)]
    [InlineData(ChartSeriesKind.Sunburst, true, 360)]
    [InlineData(ChartSeriesKind.Sunburst, false, 800)]
    [InlineData(ChartSeriesKind.Sunburst, true, 800)]
    [InlineData(ChartSeriesKind.Sankey, false, 360)]
    [InlineData(ChartSeriesKind.Sankey, true, 360)]
    [InlineData(ChartSeriesKind.Sankey, false, 800)]
    [InlineData(ChartSeriesKind.Sankey, true, 800)]
    public async Task RepeatedLabelsKeepAuthoredSelectionAndTooltipIdentity(ChartSeriesKind kind, bool dark, int width) {
        if (!Enabled) return;
        var chart = RelationshipIdentityTests.RepeatedLabels(kind).WithSize(width, width < 500 ? 320 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Teams and support").WithLegend(true);
        chart.Series[0].WithInteractionKey("team-source");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 450 : 580);
        var page = session.Page;
        await Capture(page, kind.ToString().ToLowerInvariant() + "-" + width + "-" + (dark ? "dark" : "light") + "-static", html);
        await CaptureNative(chart, kind.ToString().ToLowerInvariant() + "-" + width + "-" + (dark ? "dark" : "light"));
        Assert.Equal(5, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(kind == ChartSeriesKind.Sunburst ? 0 : 4, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point], [data-cfx-target-kind=link][data-cfx-point]").CountAsync());
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail)); }");
        var target = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id='south-support']");
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        using var record = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selections.at(-1).target)"));
        var selection = record.RootElement;
        Assert.Equal("south-support", selection.GetProperty("targetId").GetString());
        Assert.Equal("node", selection.GetProperty("targetKind").GetString());
        Assert.Equal("Support", selection.GetProperty("label").GetString());
        Assert.Equal("team-source", selection.GetProperty("seriesKey").GetString());
        Assert.Equal("8", selection.GetProperty("value").GetString());
        Assert.False(selection.TryGetProperty("point", out _)); Assert.False(selection.TryGetProperty("sourcePoint", out _));
        if (kind == ChartSeriesKind.Sunburst) {
            var position = await target.EvaluateAsync<double[]>("""
                node => {
                    const root = node.closest('svg').querySelector('[data-cfx-node=root]');
                    const centre = root.getBoundingClientRect();
                    const svg = node.closest('svg'), scale = svg.getBoundingClientRect().width / svg.viewBox.baseVal.width;
                    const angle = Number(node.dataset.cfxStartAngle) + Number(node.dataset.cfxSweep) / 2;
                    const radius = (Number(node.dataset.cfxInnerRadius) + Number(node.dataset.cfxOuterRadius)) / 2 * scale;
                    return [centre.x + centre.width / 2 + Math.cos(angle) * radius, centre.y + centre.height / 2 + Math.sin(angle) * radius];
                }
                """);
            await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        } else await MoveToAsync(page, "[data-cfx-target-kind=node][data-cfx-target-id='south-support']");
        await page.WaitForFunctionAsync("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.textContent.includes('8'); }");
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("8", await TooltipTextAsync(page));
        var screenshot = await Capture(page, kind.ToString().ToLowerInvariant() + "-" + width + "-" + (dark ? "dark" : "light"), html);
        if (screenshot != null) Assert.True(File.Exists(screenshot));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task ParallelFlowsAndRenamedReorderedNodesEmitTheirAuthoredIds() {
        if (!Enabled) return;
        var nodes = new[] { new ChartNode("output", "Support"), new ChartNode("input", "Renamed support") };
        var chart = Chart.Create().WithSize(640, 350).AddSankey("Traffic", nodes, new[] {
            new ChartFlowLink("priority", "input", "output", 8), new ChartFlowLink("standard", "input", "output", 5)
        }).WithSankeyNodeState("input", ChartSeriesState.Warning);
        var html = chart.ToInteractiveHtmlPage();
        await using var session = await OpenAsync(html);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail)); }");
        foreach (var id in new[] { "priority", "standard" }) {
            var link = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']");
            await link.FocusAsync(); await page.Keyboard.PressAsync("Space");
            Assert.Equal(id, await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
            Assert.Equal("link", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetKind"));
            Assert.Equal("input", await link.GetAttributeAsync("data-cfx-source"));
            Assert.Equal("output", await link.GetAttributeAsync("data-cfx-target"));
        }
        var input = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=input]");
        Assert.Equal("Warning", await input.GetAttributeAsync("data-cfx-state"));
        Assert.Equal("Renamed support", await input.GetAttributeAsync("data-cfx-label"));
        await input.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("input", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.selections.at(-1).target.sourcePoint === undefined"));
        await Capture(page, "parallel-flow-identities", html); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Tree, false, 800)]
    [InlineData(ChartSeriesKind.Tree, true, 360)]
    [InlineData(ChartSeriesKind.Sunburst, false, 800)]
    [InlineData(ChartSeriesKind.Sunburst, true, 360)]
    [InlineData(ChartSeriesKind.Sankey, false, 800)]
    [InlineData(ChartSeriesKind.Sankey, true, 360)]
    public async Task ConfiguredExamplesRetainRepeatedLabelsAndNativePaint(ChartSeriesKind kind, bool dark, int width) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(kind, "options").WithSize(width, width < 500 ? 320 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Explicit relationship identities");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 430 : 570);
        var supports = session.Page.Locator("[data-cfx-target-kind=node][data-cfx-label=Support]");
        Assert.Equal(2, await supports.CountAsync());
        var ids = await supports.EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)");
        Assert.Equal(2, ids.Distinct(StringComparer.Ordinal).Count());
        if (kind == ChartSeriesKind.Sankey) {
            Assert.Equal(7, await session.Page.Locator("[data-cfx-target-kind=link]").CountAsync());
            Assert.Equal("Warning", await session.Page.Locator("[data-cfx-target-id=north-support][data-cfx-target-kind=node]").GetAttributeAsync("data-cfx-state"));
        }
        var name = "configured-" + kind.ToString().ToLowerInvariant() + "-" + width + "-" + (dark ? "dark" : "light");
        await Capture(session.Page, name, html); await CaptureNative(chart, name); AssertNoConsoleErrors(session);
    }

    private static async Task CaptureNative(Chart chart, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, name + "-native.png"), chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static async Task<string?> Capture(IPage page, string name, string html) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return null;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".png");
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
        return path;
    }
}
