using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveChordBrowserTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task DirectedRepeatedParallelAndSelfFlowsKeepNativePointerAndKeyboardIdentity(bool dark, int width) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Chord, "options").WithSize(width, width < 500 ? 320 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Directed transfers").WithLegend(true);
        chart.Series[0].WithInteractionKey("transfer-source");
        var name = "chord-" + width + "-" + (dark ? "dark" : "light");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 460 : 600);
        var page = session.Page;
        await CaptureAsync(chart, page, name, html);
        Assert.Equal(4, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(9, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point], [data-cfx-target-kind=link][data-cfx-point]").CountAsync());
        await page.EvaluateAsync("() => { window.selections = []; window.hovers = []; const root = document.querySelector('.cfx-interactive-chart'); root.addEventListener('cfxselect', event => window.selections.push(event.detail)); root.addEventListener('cfxhover', event => window.hovers.push(event.detail)); }");
        foreach (var id in new[] { "north-standard", "north-priority", "south-north", "support-internal", "zero-transfer" }) {
            var fact = chart.Series[0].FlowLinks.Single(link => link.Id == id);
            var link = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']");
            await link.FocusAsync(); await page.Keyboard.PressAsync("Space");
            using var record = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selections.at(-1).target)"));
            var selected = record.RootElement;
            Assert.Equal(id, selected.GetProperty("targetId").GetString());
            Assert.Equal("link", selected.GetProperty("targetKind").GetString());
            Assert.Equal("transfer-source", selected.GetProperty("seriesKey").GetString());
            Assert.Equal(fact.SourceId, await link.GetAttributeAsync("data-cfx-source"));
            Assert.Equal(fact.TargetId, await link.GetAttributeAsync("data-cfx-target"));
            Assert.Equal(await link.GetAttributeAsync("data-cfx-value"), selected.GetProperty("value").GetString());
            Assert.False(selected.TryGetProperty("point", out _)); Assert.False(selected.TryGetProperty("sourcePoint", out _));
        }
        foreach (var id in new[] { "north-standard", "north-priority", "support-internal" }) {
            var link = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']");
            var endpoint = await link.EvaluateAsync<double[]>("""
                link => {
                    const svg = link.closest('svg');
                    const source = Array.from(svg.querySelectorAll('[data-cfx-target-kind=node]')).find(node => node.dataset.cfxTargetId === link.dataset.cfxSource);
                    const angle = Number(link.dataset.cfxSourceStartAngle) + Number(link.dataset.cfxSourceSweep) / 2;
                    const radius = Number(source.dataset.cfxInnerRadius) * .995;
                    const point = new DOMPoint(Number(source.dataset.cfxCenterX) + Math.cos(angle) * radius,
                        Number(source.dataset.cfxCenterY) + Math.sin(angle) * radius).matrixTransform(svg.getScreenCTM());
                    return [point.x, point.y];
                }
                """);
            await page.Mouse.ClickAsync((float)endpoint[0], (float)endpoint[1]);
            Assert.Equal(id, await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        }
        var node = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=north-support]");
        await node.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("north-support", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        Assert.Equal("Support", await node.GetAttributeAsync("data-cfx-label"));
        await page.Keyboard.PressAsync("Space"); // Toggle off the keyboard-pinned tooltip before observing real pointer hover.
        await node.EvaluateAsync("node => node.blur()");
        var position = await node.EvaluateAsync<double[]>("""
            node => {
                const angle = Number(node.dataset.cfxStartAngle) + Number(node.dataset.cfxSweep) / 2;
                const radius = (Number(node.dataset.cfxInnerRadius) + Number(node.dataset.cfxOuterRadius)) / 2;
                const point = new DOMPoint(Number(node.dataset.cfxCenterX) + Math.cos(angle) * radius,
                    Number(node.dataset.cfxCenterY) + Math.sin(angle) * radius).matrixTransform(node.closest('svg').getScreenCTM());
                return [point.x, point.y];
            }
            """);
        await MoveAwayAsync(page);
        await page.EvaluateAsync("() => window.hovers = []");
        await page.Mouse.MoveAsync((float)position[0], (float)position[1], new MouseMoveOptions { Steps = 4 });
        await page.WaitForFunctionAsync("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && !tip.classList.contains('cfx-tooltip--pinned') && tip.textContent.includes('26') && window.hovers.at(-1)?.target.targetId === 'north-support'; }");
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("26", await TooltipTextAsync(page));
        await CaptureAsync(chart, page, name + "-pointer", html, native: false);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.Equal("north-support", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllZeroChordRetainsKeyboardFactsWithoutFilledGeometry(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(360, 320).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("No positive transfers")
            .AddChord("Transfers", new[] { new ChartNode("first", "Support"), new ChartNode("second", "Support") }, new[] { new ChartFlowLink("none", "first", "second", 0) });
        var html = chart.ToInteractiveHtmlPage();
        await using var session = await OpenAsync(html, 384, 460);
        var page = session.Page;
        Assert.Equal(0, await page.Locator("[data-cfx-role=chord-ribbon], [data-cfx-role=chord-node-mark]").CountAsync());
        Assert.Equal("0", await page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=none]").GetAttributeAsync("data-cfx-value"));
        var target = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=none]");
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await CaptureAsync(chart, page, "chord-zero-" + (dark ? "dark" : "light"), html); AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(Chart chart, IPage page, string name, string html, bool native = true) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        if (native) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllBytesAsync(Path.Combine(directory, name + "-native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            await File.WriteAllTextAsync(Path.Combine(directory, name + "-native.svg"), prepared.ToSvg());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        }
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
