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

public sealed class PatternOnlyPaintBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bar, false)]
    [InlineData(ChartSeriesKind.Bar, true)]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.RadialBar, false)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true)]
    [InlineData(ChartSeriesKind.Area, false)]
    [InlineData(ChartSeriesKind.Area, true)]
    [InlineData(ChartSeriesKind.Radar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    [InlineData(ChartSeriesKind.PolarArea, false)]
    public async Task AuthoredPatternInkRetainsPhysicalPointerKeyboardAndLegendReadout(ChartSeriesKind kind, bool dark) {
        if (!Enabled) return;
        var chart = Create(kind, dark);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var name = "pattern-only-" + kind.ToString().ToLowerInvariant() + "-" + (dark ? "compact-dark" : "wide-light");
        await NativeAsync(prepared, name);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), dark ? 390 : 800, 560);
        var page = session.Page;
        if (kind == ChartSeriesKind.Radar)
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "[data-cfx-role='radar-point']{fill:none;stroke:none}" });
        var target = page.Locator("g[data-cfx-series='0'][data-cfx-point='0']:not([data-cfx-role='legend-item'])").First;
        var screen = await ScreenAsync(page, prepared);
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1], new MouseMoveOptions { Steps = 4 });
        var hover = await TooltipTextAsync(page);
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        var selected = await target.GetAttributeAsync("aria-selected");
        var pinned = await page.Locator(".cfx-tooltip").GetAttributeAsync("class");
        await CaptureImageAsync(page, name + "-pointer");
        var reset = page.Locator("[data-cfx-reset]");
        if (await reset.IsVisibleAsync()) await reset.ClickAsync();
        await MoveAwayAsync(page);
        await target.FocusAsync();
        var keyboard = await TooltipTextAsync(page);
        await page.Keyboard.PressAsync("Space");
        var keyboardSelected = await target.GetAttributeAsync("aria-selected");
        await CaptureImageAsync(page, name + "-keyboard");
        if (await reset.IsVisibleAsync()) await reset.ClickAsync();
        await MoveAwayAsync(page);
        await MoveToAsync(page, Legend(0));
        var legend = await TooltipTextAsync(page);
        await CaptureAsync(session, name, new { kind = kind.ToString(), dark, screen, hover, selected, pinned, keyboard, keyboardSelected, legend,
            target = await target.EvaluateAsync<JsonElement>("node => ({role:node.dataset.cfxRole, id:node.dataset.cfxTargetId, tabindex:node.getAttribute('tabindex'), patterns:node.querySelectorAll('[data-cfx-role$=pattern]').length})") });
        Assert.Contains("30", hover); Assert.Equal("true", selected); Assert.Contains("cfx-tooltip--pinned", pinned);
        Assert.Contains("30", keyboard); Assert.Equal("true", keyboardSelected);
        if (kind != ChartSeriesKind.PolarArea) Assert.Contains("Hatched observations", legend);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PatternSwatchWithoutCaptionInkRemainsAPhysicalLegendTarget(bool pointLegend) {
        if (!Enabled) return;
        var chart = Create(ChartSeriesKind.Bar, false).WithTransparentBackground().WithCard(false).WithPlotBackground(false).WithPointLegend(pointLegend)
            .ConfigureLegendStyle(style => style.WithColor("#00000000"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var name = "pattern-only-legend-" + (pointLegend ? "point" : "series"); await NativeAsync(prepared, name);
        var fragment = chart.ToInteractiveHtmlFragment();
        await using var session = await OpenAsync("<!doctype html><html><head><style>.cfx-interactive-chart,.cfx-stage{background:transparent!important}</style></head><body style='margin:0;background:#20344e'>" + fragment + "</body></html>", 800, 560);
        var target = session.Page.Locator(Legend(0)).First;
        await target.HoverAsync();
        var hover = await TooltipTextAsync(session.Page);
        await target.FocusAsync(); var keyboard = await TooltipTextAsync(session.Page);
        await session.Page.Keyboard.PressAsync("Space");
        var muted = await session.Page.Locator(".cfx-series-muted").CountAsync();
        await CaptureAsync(session, name, new { pointLegend, hover, keyboard, muted });
        Assert.NotEmpty(hover); Assert.NotEmpty(keyboard); Assert.True(muted > 0);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PatternInkStillObeysNativeOpacityAndClipping(bool clip, bool ancestor) {
        if (!Enabled) return;
        var chart = Create(ChartSeriesKind.Bar, false);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 800, 560);
        var page = session.Page; var target = page.Locator(Point(0, 0));
        await page.EvaluateAsync("()=>{window.cfxPatternEvents=[];const root=document.querySelector('.cfx-interactive-chart');for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxPatternEvents.push({type,detail:event.detail}));}");
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        var screen = await ScreenAsync(page, chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        await target.EvaluateAsync("(node, options) => { if (options.ancestor) node=node.closest('[data-cfx-role=series]'); if (!options.clip) { node.style.setProperty('opacity','0','important'); return; } const svg=node.ownerSVGElement; const ns=svg.namespaceURI; const defs=document.createElementNS(ns,'defs'); const path=document.createElementNS(ns,'clipPath'); path.id='pattern-native-empty-clip'; path.setAttribute('clipPathUnits','userSpaceOnUse'); const rect=document.createElementNS(ns,'rect'); rect.setAttribute('x','-100'); rect.setAttribute('y','-100'); rect.setAttribute('width','1'); rect.setAttribute('height','1'); path.appendChild(rect); defs.appendChild(path); svg.appendChild(defs); node.setAttribute('clip-path','url(#pattern-native-empty-clip)'); }", new { clip, ancestor });
        var before = await target.EvaluateAsync<JsonElement>("(node, ancestor)=>{const owner=ancestor?node.closest('[data-cfx-role=series]'):node;return {opacity:getComputedStyle(owner).opacity,clip:owner.getAttribute('clip-path')}}", ancestor);
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1]); await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var hover = await TooltipTextAsync(page); var selected = await target.GetAttributeAsync("aria-selected");
        var events = await page.EvaluateAsync<JsonElement>("()=>window.cfxPatternEvents");
        var after = await target.EvaluateAsync<JsonElement>("(node, ancestor)=>{const owner=ancestor?node.closest('[data-cfx-role=series]'):node;return {opacity:getComputedStyle(owner).opacity,clip:owner.getAttribute('clip-path'),tabindex:node.getAttribute('tabindex')}}", ancestor);
        await CaptureAsync(session, "pattern-withdrawn-" + (clip ? "clip" : "opacity") + (ancestor ? "-ancestor" : "-point"), new { clip, ancestor, before, after, screen, hover, selected, events });
        if (!clip) { Assert.Equal("0", before.GetProperty("opacity").GetString()); Assert.Equal("0", after.GetProperty("opacity").GetString()); }
        Assert.Equal("-1", after.GetProperty("tabindex").GetString());
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxPatternEvents.filter(event=>event.detail.target?.targetId===id).length", id));
        Assert.NotEqual("true", selected); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task NumericPatternDoesNotAcquireRetainedZeroOrCollapsedFactsWithAPointer(bool bars, bool collapse) {
        if (!Enabled) return;
        var chart = NumericRadialEncodedGeometryTests.Create(bars);
        if (!collapse) chart.Series[0].Points[0] = new ChartPoint(1, 0);
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        var name = "pattern-retained-" + (bars ? "bar" : "column") + "-" + (collapse ? "collapse" : "zero");
        await NativeAsync(chart.Prepare(VisualExportRequest.ForChart(chart).Context), name);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        await page.EvaluateAsync("()=>{window.cfxPatternEvents=[];const root=document.querySelector('.cfx-interactive-chart');for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxPatternEvents.push({type,detail:event.detail}));}");
        var id = await point.GetAttributeAsync("data-cfx-target-id");
        var screen = await point.EvaluateAsync<float[]>("""
            node=>{const root=node.closest('.cfx-interactive-chart'),region=JSON.parse(root.dataset.cfxPreparedChart).regions.find(region=>region.id===node.dataset.cfxSourceId);
              const p=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());return [p.x,p.y];}
            """);
        await page.Mouse.MoveAsync(screen[0], screen[1]); await page.Mouse.ClickAsync(screen[0], screen[1]);
        var events = await page.EvaluateAsync<JsonElement>("()=>window.cfxPatternEvents");
        var pointerSelected = await point.GetAttributeAsync("aria-selected");
        var reset = page.Locator("[data-cfx-reset]"); if (await reset.IsVisibleAsync()) await reset.ClickAsync(); await MoveAwayAsync(page);
        await point.FocusAsync(); var keyboard = await TooltipTextAsync(page); await page.Keyboard.PressAsync("Space");
        var selected = await point.GetAttributeAsync("aria-selected"); var status = await point.GetAttributeAsync("data-cfx-geometry-status");
        await CaptureAsync(session, name, new { bars, collapse, screen, events, id, pointerSelected, keyboard, selected, status });
        Assert.DoesNotContain(events.EnumerateArray(), item => item.GetProperty("detail").TryGetProperty("target", out var target)
            && target.ValueKind == JsonValueKind.Object && target.TryGetProperty("targetId", out var value) && value.GetString() == id);
        Assert.NotEqual("true", pointerSelected); Assert.Equal(collapse ? "precision-collapse" : "zero", status);
        Assert.Equal(0, await point.Locator("[data-cfx-browser-hit-area],line,path").CountAsync());
        Assert.Contains("Observed", keyboard); Assert.Equal("true", selected);
        Assert.Equal(collapse ? "1" : "0", await point.GetAttributeAsync("data-cfx-y")); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PatternedSharedCollapsedSliceRequiresItsPaintedSeparator(bool donut) {
        if (!Enabled) return;
        var chart = NumericRadialEncodedGeometryTests.SharedStrokedSlice(donut); chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        chart.Series[0].Points.Clear();
        chart.Series[0].Points.AddRange(new[] { new ChartPoint(1, 100000000), new ChartPoint(2, 1), new ChartPoint(3, 70000000) });
        var name = "pattern-collapsed-" + (donut ? "donut" : "pie");
        await NativeAsync(chart.Prepare(VisualExportRequest.ForChart(chart).Context), name);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page; var point = page.Locator("[data-cfx-target-kind='point'][data-cfx-point='1']");
        var path = point.Locator("path").First;
        Assert.Equal("false", await path.GetAttributeAsync("data-cfx-fill-area"));
        await path.EvaluateAsync("node=>node.style.stroke='none'");
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var selected = await point.GetAttributeAsync("aria-selected"); var absent = await point.GetAttributeAsync("tabindex");
        var patterns = await point.Locator("[data-cfx-role='radial-fill-pattern']").CountAsync();
        var reset = page.Locator("[data-cfx-reset]"); if (await reset.IsVisibleAsync()) await reset.ClickAsync(); await MoveAwayAsync(page);
        await path.EvaluateAsync("node=>node.style.stroke=''"); await point.FocusAsync(); var keyboard = await TooltipTextAsync(page);
        await CaptureAsync(session, name, new { donut, selected, absent, patterns, keyboard });
        Assert.NotEqual("true", selected); Assert.Equal("-1", absent); Assert.Equal(0, patterns);
        Assert.Contains("Observed", keyboard); Assert.Equal("1", await point.GetAttributeAsync("data-cfx-value")); AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task EncodedCollapsedSunburstPatternCannotReplaceAnAbsentSeparator() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddSunburst("Observed", new[] { new ChartHierarchyItem("Root", "Root"),
                new ChartHierarchyItem("Large A", "Large A", "Root", 100000000),
                new ChartHierarchyItem("Tiny", "Tiny", "Root", 1),
                new ChartHierarchyItem("Large B", "Large B", "Root", 70000000) });
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch).WithDataLabels(false);
        const string name = "pattern-collapsed-sunburst";
        await NativeAsync(chart.Prepare(VisualExportRequest.ForChart(chart).Context), name);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page; var point = page.Locator("[data-cfx-role='sunburst-segment'][data-cfx-label='Tiny']");
        var path = point.Locator("path").First;
        Assert.Equal("false", await path.GetAttributeAsync("data-cfx-fill-area"));
        await path.EvaluateAsync("node=>node.style.stroke='none'");
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var selected = await point.GetAttributeAsync("aria-selected"); var absent = await point.GetAttributeAsync("tabindex");
        var patterns = await point.Locator("[data-cfx-role='sunburst-pattern']").CountAsync();
        await CaptureAsync(session, name, new { selected, absent, patterns });
        Assert.NotEqual("true", selected); Assert.Equal("-1", absent); Assert.Equal(0, patterns);
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-value")); AssertNoConsoleErrors(session);
    }

    internal static Chart Create(ChartSeriesKind kind, bool dark) {
        var chart = Chart.Create().WithSize(dark ? 360 : 760, dark ? 360 : 440).WithHeader(false).WithLegend().WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithBarStyle(ChartBarStyle.Flat).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 70), new ChartPoint(3, 50) };
        switch (kind) {
            case ChartSeriesKind.Bar: chart.AddBar("Hatched observations", points); break;
            case ChartSeriesKind.Scatter: chart.AddScatter("Hatched observations", points); break;
            case ChartSeriesKind.Area: chart.AddArea("Hatched observations", points); break;
            case ChartSeriesKind.Radar: chart.AddRadar("Hatched observations", points); break;
            case ChartSeriesKind.PolarArea: chart.AddPolarArea("Hatched observations", points); break;
            case ChartSeriesKind.RadialBar: chart.AddRadialBar("Hatched observations", points); break;
            case ChartSeriesKind.RadialColumn: chart.AddRadialColumn("Hatched observations", points); break;
        }
        chart.WithYAxisBounds(0, 100);
        chart.Series[0].WithColor((dark ? ChartColor.FromRgb(0, 0, 0) : ChartColor.FromRgb(255, 255, 255)).WithAlpha(0)).WithFillPattern(ChartFillPattern.Crosshatch);
        if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Area or ChartSeriesKind.Radar)
            chart.Series[0].WithMarkerRadius(kind == ChartSeriesKind.Scatter ? 12 : 0);
        return chart;
    }

    private static async Task<double[]> ScreenAsync(IPage page, PreparedVisual prepared) {
        var slice = prepared.Scene.Nodes.OfType<VisualSceneSlice>().FirstOrDefault();
        double x, y;
        if (slice != null) {
            var angle = slice.Start + slice.Sweep / 2; var radius = (slice.Inner + slice.Outer) / 2;
            x = slice.Cx + Math.Cos(angle) * radius; y = slice.Cy + Math.Sin(angle) * radius;
        } else {
            var region = prepared.Regions.First(item => item.Role == "point" || item.Role == "radar-point");
            x = region.Bounds.Left + region.Bounds.Width / 2; y = region.Bounds.Top + region.Bounds.Height / 2;
        }
        return await page.EvaluateAsync<double[]>("point => {const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y];}", new { x, y });
    }

    private static async Task NativeAsync(PreparedVisual prepared, string name) {
        var directory = DirectoryPath(); if (directory == null) return;
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
    private static async Task CaptureImageAsync(IPage page, string name) {
        var directory = DirectoryPath(); if (directory == null) return;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
    }
    private static async Task CaptureAsync(HtmlTinkerX.HtmlBrowserSession session, string name, object state) {
        var directory = DirectoryPath(); if (directory == null) return;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static string? DirectoryPath() {
        var path = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(path)) return null; Directory.CreateDirectory(path); return path;
    }
}
