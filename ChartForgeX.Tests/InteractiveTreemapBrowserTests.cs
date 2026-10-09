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

public sealed class InteractiveTreemapBrowserTests {
    [Theory]
    [InlineData("wide", false, 800)]
    [InlineData("wide", true, 800)]
    [InlineData("wide", false, 360)]
    [InlineData("wide", true, 360)]
    [InlineData("options", false, 800)]
    [InlineData("options", true, 800)]
    [InlineData("options", false, 360)]
    [InlineData("options", true, 360)]
    public async Task ConfiguredHierarchyKeepsContainmentColorsAndKeyboardSelection(string variant, bool dark, int width) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Treemap, variant, mode).WithSize(width, width < 500 ? 340 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Allocated work by team")
            .WithSubtitle("Area: allocated work · Color: change (%)");
        chart.Series[0].WithInteractionKey("allocation-source");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 490 : 610);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var name = "treemap-" + variant + "-" + width + "-" + (dark ? "dark" : "light");
        await Capture(chart, page, name, html);
        Assert.Equal(12, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(2, await page.Locator("[data-cfx-target-kind=node][data-cfx-label=Support]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point], [data-cfx-target-kind=node][data-cfx-source-point]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => Array.from(document.querySelectorAll('[data-cfx-node][data-cfx-parent]')).every(node => {
                const mark = node.querySelector(':scope > [data-cfx-role$="-mark"]');
                if (!mark) return true;
                const parent = node.parentElement.closest('[data-cfx-node]');
                const own = mark.getBoundingClientRect(), outer = parent.querySelector(':scope > [data-cfx-role$="-mark"]').getBoundingClientRect();
                return parent.dataset.cfxNode === node.dataset.cfxParent && own.left > outer.left && own.right < outer.right && own.top > outer.top && own.bottom < outer.bottom;
            })
            """));
        var reserve = page.Locator("[data-cfx-node=reserve]");
        Assert.Equal("0", await reserve.GetAttributeAsync("data-cfx-value")); Assert.Equal(0, await reserve.Locator("rect, path").CountAsync());
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail)); }");
        var support = page.Locator("[data-cfx-node=south-support]");
        if (variant == "options") {
            var warning = VisualTheme.Graphite().Resolve(mode).Status.Medium.Fill;
            Assert.Equal("Warning", await support.GetAttributeAsync("data-cfx-state"));
            Assert.Equal($"rgb({warning.R}, {warning.G}, {warning.B})",
                await support.Locator(":scope > [data-cfx-role=treemap-tile-mark]").EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        }
        await support.FocusAsync(); await page.Keyboard.PressAsync("Space");
        using var detail = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selections.at(-1).target)"));
        Assert.Equal("south-support", detail.RootElement.GetProperty("targetId").GetString());
        Assert.Equal("node", detail.RootElement.GetProperty("targetKind").GetString());
        Assert.Equal("Support", detail.RootElement.GetProperty("label").GetString());
        Assert.Equal("22", detail.RootElement.GetProperty("value").GetString());
        Assert.Equal("allocation-source", detail.RootElement.GetProperty("seriesKey").GetString());
        Assert.False(detail.RootElement.TryGetProperty("point", out _)); Assert.False(detail.RootElement.TryGetProperty("sourcePoint", out _));
        Assert.Equal("8", await support.GetAttributeAsync("data-cfx-color-value"));
        await MoveToAsync(page, "[data-cfx-node=south-support] > [data-cfx-role=treemap-tile-mark]");
        await page.WaitForFunctionAsync("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.textContent.includes('22'); }");
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("22", await TooltipTextAsync(page));
        var mark = await support.Locator(":scope > [data-cfx-role=treemap-tile-mark]").EvaluateAsync<string>("node => getComputedStyle(node).fill");
        var expected = chart.Options.Treemap.ColorScale!.ColorFor(8);
        Assert.Equal($"rgb({expected.R}, {expected.G}, {expected.B})", mark);
        await Capture(chart, page, name + "-selection", html, native: false);
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenamedReorderedItemsKeepAuthoredHostEventIdentity(bool dark) {
        if (!Enabled) return;
        var items = TreemapHierarchyTests.ForestItems().AsEnumerable().Reverse().Select(item => new ChartTreemapItem(item.Id,
            item.Id == "south-support" ? "Customer care" : item.Label, item.ParentId, item.Value, item.ColorValue));
        var chart = Chart.Create().WithSize(640, 400).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).AddTreemap("Allocation", items);
        var html = chart.ToInteractiveHtmlPage(); await using var session = await OpenAsync(html, 680, 520);
        await session.Page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selection = event.detail.target)");
        var target = session.Page.Locator("[data-cfx-node=south-support]"); await target.FocusAsync(); await session.Page.Keyboard.PressAsync("Space");
        Assert.Equal("south-support", await session.Page.EvaluateAsync<string>("() => window.selection.targetId"));
        Assert.Equal("Customer care", await session.Page.EvaluateAsync<string>("() => window.selection.label"));
        Assert.Equal("8", await session.Page.EvaluateAsync<string>("() => window.selection.value"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => window.selection.sourcePoint === undefined && window.selection.point === undefined"));
        await Capture(chart, session.Page, "treemap-reordered-" + (dark ? "dark" : "light"), html); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingColorSingletonUsesNoDataPaintWithoutZeroScaleReadout(bool dark) {
        if (!Enabled) return;
        var missing = ChartColor.FromRgb(81, 124, 148);
        var chart = Chart.Create().WithSize(360, 320).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("One independent team").AddTreemap("Team", new[] { new ChartTreemapItem("support", "Support", value: 8) })
            .ConfigureTreemap(options => options.ColorScale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithNoDataColor(missing));
        var html = chart.ToInteractiveHtmlPage(); await using var session = await OpenAsync(html, 400, 440);
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role=treemap-color-scale-step]").CountAsync());
        Assert.Null(await session.Page.Locator("[data-cfx-role=treemap-color-scale]").GetAttributeAsync("data-cfx-min-value"));
        Assert.Equal(missing.ToCss(), await session.Page.Locator("[data-cfx-role=treemap-tile-mark]").GetAttributeAsync("fill"));
        Assert.Equal("No data", await session.Page.Locator("[data-cfx-role=treemap-color-scale-label]").TextContentAsync());
        await Capture(chart, session.Page, "treemap-missing-single-" + (dark ? "dark" : "light"), html); AssertNoConsoleErrors(session);
    }

    private static async Task Capture(Chart chart, IPage page, string name, string html, bool native = true) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        var facts = await page.EvaluateAsync<JsonElement>("""
            () => ({ nodes: Array.from(document.querySelectorAll('[data-cfx-node]')).map(node => ({
                id: node.dataset.cfxTargetId, label: node.dataset.cfxLabel, parent: node.dataset.cfxParent,
                size: node.dataset.cfxValue, color: node.dataset.cfxColorValue, missing: node.dataset.cfxColorMissing,
                point: node.dataset.cfxPoint, sourcePoint: node.dataset.cfxSourcePoint
            })), scale: document.querySelector('[data-cfx-role=treemap-color-scale]')?.dataset,
                selection: window.selections?.at(-1)?.target ?? window.selection,
                tooltip: document.querySelector('.cfx-tooltip')?.innerText })
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
        if (!native) return;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        await File.WriteAllTextAsync(Path.Combine(directory, name + "-native.svg"), prepared.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(directory, name + "-native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
