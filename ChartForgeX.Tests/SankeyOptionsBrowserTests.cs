using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class SankeyOptionsBrowserTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task ConfiguredGeometryKeepsPointerAndKeyboardSelectionOnAuthoredTargets(bool dark, int width) {
        if (!Enabled) return;
        string variant = width < 500 ? "compact-options" : "options";
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sankey, variant).WithSize(width, width < 500 ? 360 : 440)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Requests across processing stages").WithSubtitle("Aligned and ordered weighted flows");
        chart.WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        chart.Series[0].WithInteractionKey("request-flows");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 630 : 640);
        var page = session.Page;
        string name = "sankey-styled-" + variant + "-" + (dark ? "dark" : "light");
        await Capture(page, name + "-static", html, prepared);
        Assert.Equal(8, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(9, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal("Center", await page.Locator("[data-cfx-role=sankey-series]").GetAttributeAsync("data-cfx-alignment"));
        Assert.Equal("2", await Target(page, "node", "review").GetAttributeAsync("data-cfx-layer"));
        Assert.Equal("1", await Target(page, "node", "imported").GetAttributeAsync("data-cfx-layer"));
        Assert.Equal(2, await page.Locator("[data-cfx-target-kind=node][data-cfx-label=Support]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind][data-cfx-point], [data-cfx-target-kind][data-cfx-source-point]").CountAsync());
        var standard = Target(page, "link", "north-standard"); var priority = Target(page, "link", "north-priority");
        var priorityRibbon = priority.Locator("[data-cfx-role=sankey-ribbon]");
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.FromHex("#AF6B24"), .7).ToCss(), await priorityRibbon.GetAttributeAsync("fill"));
        Assert.Equal(ChartColor.FromHex("#764415").ToCss(), await priorityRibbon.GetAttributeAsync("stroke"));
        Assert.Equal("1", await priorityRibbon.GetAttributeAsync("stroke-width"));
        Assert.Equal("Warning", await priority.GetAttributeAsync("data-cfx-state"));
        Assert.Equal("None", await standard.GetAttributeAsync("data-cfx-state"));
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail)); }");

        var support = Target(page, "node", "south-support");
        var box = (await support.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        Assert.Equal("true", await support.GetAttributeAsync("aria-selected"));
        await AssertSelected(page, "node", "south-support", "28");
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("28", await TooltipTextAsync(page));
        var pointer = await priority.EvaluateAsync<double[]>("""
            link => {
                const path = link.querySelector('[data-cfx-role=sankey-ribbon]');
                const box = path.getBBox();
                for (const fraction of [.05, .15, .3, .5, .75, .9]) {
                    const x = box.x + box.width * fraction;
                    for (let y = box.y + .5; y < box.y + box.height; y += Math.max(.5, box.height / 40)) {
                        if (!path.isPointInFill(new DOMPoint(x, y))) continue;
                        const point = new DOMPoint(x, y).matrixTransform(path.getScreenCTM());
                        const target = document.elementFromPoint(point.x, point.y)?.closest('[data-cfx-target-kind]');
                        if (target === link) return [point.x, point.y];
                    }
                }
                throw new Error('The styled ribbon has no exposed native pointer surface.');
            }
            """);
        await page.Mouse.ClickAsync((float)pointer[0], (float)pointer[1]);
        Assert.Equal("true", await priority.GetAttributeAsync("aria-selected"));
        await AssertSelected(page, "link", "north-priority", "20");
        await Capture(page, name + "-styled-pointer");
        await priority.FocusAsync(); await page.Keyboard.PressAsync("Space");

        await standard.FocusAsync(); await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await priority.EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await priority.GetAttributeAsync("aria-selected"));
        await AssertSelected(page, "link", "north-priority", "20");
        Assert.Equal("north", await priority.GetAttributeAsync("data-cfx-source"));
        Assert.Equal("north-support", await priority.GetAttributeAsync("data-cfx-target"));
        Assert.True(await page.EvaluateAsync<bool>("() => { const box = document.querySelector('.cfx-tooltip').getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        await Capture(page, name + "-styled-keyboard");
        AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await File.WriteAllTextAsync(Path.Combine(directory, name + "-console.json"), JsonSerializer.Serialize(session.ConsoleLog));
    }

    private static ILocator Target(IPage page, string kind, string id) => page.Locator("[data-cfx-target-kind='" + kind + "'][data-cfx-target-id='" + id + "']");
    private static async Task AssertSelected(IPage page, string kind, string id, string value) {
        using var record = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selections.at(-1).target)"));
        var target = record.RootElement;
        Assert.Equal(kind, target.GetProperty("targetKind").GetString()); Assert.Equal(id, target.GetProperty("targetId").GetString());
        Assert.Equal(value, target.GetProperty("value").GetString()); Assert.Equal("request-flows", target.GetProperty("seriesKey").GetString());
        Assert.False(target.TryGetProperty("point", out _)); Assert.False(target.TryGetProperty("sourcePoint", out _));
    }

    private static async Task Capture(IPage page, string name, string? html = null, PreparedVisual? prepared = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        if (html != null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        if (prepared != null) {
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + "-native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        }
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
