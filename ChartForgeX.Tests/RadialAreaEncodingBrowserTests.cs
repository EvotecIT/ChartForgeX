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

[Trait("BrowserSuite", "Compatibility")]
public sealed class RadialAreaEncodingBrowserTests {
    private const string North = "[data-cfx-role=point][data-cfx-series-key=observed][data-cfx-observation-id=north]";

    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1000)]
    [InlineData(true, 1000)]
    public async Task FreshNativeAreaSectionsRetainSelectionRawValuesAndIdentityAcrossStackingReorderAndRefill(bool dark, int width) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(dark, width, false);
        var first = renderer.PrepareSection(chart, configure: Fit); var next = renderer.PrepareSection(Model(dark, width, true), configure: Fit);
        var emptyChart = Model(dark, width, true); emptyChart.ClearSeries(); var empty = renderer.PrepareSection(emptyChart, configure: Fit);
        Assert.Null(first.PreparedVisual.XProjection); Assert.Null(first.PreparedVisual.PolarProjection);
        var body = "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + first.Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width, 650); var page = session.Page;
        await page.EvaluateAsync("() => { window.areaController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart')); window.areaSelections=[]; areaController.root.addEventListener('cfxselect',e=>areaSelections.push(e.detail.target)); }");
        var mark = NumericRadialSeriesTests.Marks(first.PreparedVisual.Scene)[0]; var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var position = await page.EvaluateAsync<double[]>("point=>{const svg=areaController.root.querySelector('svg');const p=svg.createSVGPoint();p.x=point.x;p.y=point.y;const q=p.matrixTransform(svg.getScreenCTM());return [q.x,q.y];}",
            new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync((float)position[0], (float)position[1], new MouseMoveOptions { Steps = 3 }); Assert.Contains("25", await TooltipTextAsync(page));
        await MoveAwayAsync(page); await page.Locator(North).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        await Capture(page, first.PreparedVisual, width, dark, "selected");
        await page.EvaluateAsync("html=>areaController.update(html)", next.Html);
        var north = page.Locator(North); Assert.Equal("1", await north.GetAttributeAsync("data-cfx-series"));
        Assert.Equal("2", await north.GetAttributeAsync("data-cfx-point")); Assert.Equal("40", await north.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("area", await north.GetAttributeAsync("data-cfx-value-encoding")); Assert.Equal("true", await north.GetAttributeAsync("aria-selected"));
        Assert.True(await north.EvaluateAsync<bool>("node=>document.activeElement===node"));
        await north.FocusAsync(); Assert.Contains("40", await TooltipTextAsync(page));
        Assert.Equal("north", await page.EvaluateAsync<string>("()=>areaSelections[0].observationId"));
        Assert.Equal("25", await page.EvaluateAsync<string>("()=>areaSelections[0].value"));
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        await Capture(page, next.PreparedVisual, width, dark, "replaced");
        await page.EvaluateAsync("html=>areaController.update(html)", empty.Html); Assert.Equal(0, await page.Locator(North).CountAsync());
        await page.EvaluateAsync("html=>areaController.update(html)", next.Html); Assert.Equal(1, await page.Locator(North).CountAsync());
        Assert.NotEqual("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        await page.Locator(North).FocusAsync(); Assert.Contains("40", await TooltipTextAsync(page));
        await Capture(page, next.PreparedVisual, width, dark, "refilled");
        await page.EvaluateAsync("()=>areaController.dispose()"); AssertNoConsoleErrors(session);
    }

    private static void Fit(HtmlChartInteractionOptions options) => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    private static Chart Model(bool dark, int width, bool reordered) {
        var original = new[] { new ChartPoint(1, reordered ? 40 : 25), new ChartPoint(2, 100), new ChartPoint(3, 0) };
        var points = reordered ? original.AsEnumerable().Reverse().ToArray() : original;
        var series = new ChartSeries(new ChartSeriesId("observed"), reordered ? "Updated measurements" : "Measured requests", ChartSeriesKind.RadialColumn, points);
        var ids = new[] { "north", "south", "east" };
        for (var index = 0; index < points.Length; index++) series.WithObservationId(index, new ChartObservationId(ids[(int)points[index].X - 1]));
        var peer = new ChartSeries(new ChartSeriesId("follow-up"), "Follow-up", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(4, 80) });
        if (reordered) { series.WithStackGroup("requests"); peer.WithStackGroup("requests"); }
        var chart = Chart.Create().WithSize(width, 560).WithFontFamily(BrowserProofFonts.Family)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Requests by intake channel").WithXLabels("North", "South", "East", "West").WithYAxisBounds(0, 140)
            .WithLegend().WithDataLabels(false).WithRadialGeometry(new(-90, 270, .15, .18, .1, valueEncoding: ChartRadialValueEncoding.Area));
        chart.Options.YAxis.TickCount = 4; chart.Options.YAxis.Reversed = reordered; chart.Options.XAxis.Reversed = reordered;
        return reordered ? chart.AddSeries(peer).AddSeries(series) : chart.AddSeries(series).AddSeries(peer);
    }

    private static async Task Capture(IPage page, PreparedVisual prepared, int width, bool dark, string phase) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "categorical-radial-area-" + width + "-" + (dark ? "dark" : "light") + "-" + phase);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png" });
        File.WriteAllText(stem + ".native.svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
        await File.WriteAllTextAsync(stem + ".runtime.json", await page.EvaluateAsync<string>("""
            () => JSON.stringify({selected:areaSelections,points:Array.from(areaController.root.querySelectorAll('[data-cfx-role=point]')).map(node=>({
              series:node.dataset.cfxSeriesKey,id:node.dataset.cfxObservationId,source:node.dataset.cfxPoint,value:node.dataset.cfxY,
              encoding:node.dataset.cfxValueEncoding,selected:node.getAttribute('aria-selected')})),
              viewport:{width:innerWidth,scrollWidth:document.documentElement.scrollWidth}},null,2)
            """));
    }
}
