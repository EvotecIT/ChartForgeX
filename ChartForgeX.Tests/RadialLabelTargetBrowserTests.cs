using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RadialLabelTargetBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie, false)]
    [InlineData(ChartSeriesKind.Pie, true)]
    [InlineData(ChartSeriesKind.Donut, false)]
    [InlineData(ChartSeriesKind.Donut, true)]
    [InlineData(ChartSeriesKind.RadialBar, false)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true)]
    [InlineData(ChartSeriesKind.Polar, false)]
    [InlineData(ChartSeriesKind.Polar, true)]
    [InlineData(ChartSeriesKind.Radar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    [InlineData(ChartSeriesKind.PolarArea, false)]
    [InlineData(ChartSeriesKind.PolarArea, true)]
    public async Task PaintedCaptionUsesItsNativeObservationWithoutAddingAnObservationOrInferringEmptySpace(ChartSeriesKind kind, bool outside) {
        if (!Enabled) return;
        var chart = Create(kind, outside);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), outside ? 720 : 430, 510);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxLabelEvents=[]; const root=document.querySelector('.cfx-interactive-chart'); for(const type of ['cfxhover','cfxselect']) root.addEventListener(type,event=>window.cfxLabelEvents.push({type,detail:event.detail})); }");
        var label = page.Locator("[data-cfx-role='data-label'] text,[data-cfx-role='radial-data-label'] text,[data-cfx-role='polar-data-label'] text,[data-cfx-role='radar-data-label'] text").First;
        var box = await label.BoundingBoxAsync() ?? throw new InvalidOperationException("Caption was not painted.");
        var x = box.X + box.Width * .35; var y = box.Y + box.Height * .45;
        await page.Mouse.MoveAsync((float)x, (float)y, new MouseMoveOptions { Steps = 4 });
        var hover = await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key");
        var tooltip = await TooltipTextAsync(page);
        await CaptureAsync(page, chart, kind, outside, new { hover, tooltip, hit = await page.EvaluateAsync<string>("p => { const hit=document.elementFromPoint(p.x,p.y); return hit?.closest('[data-cfx-role]')?.getAttribute('data-cfx-role') || hit?.tagName || ''; }", new { x, y }) });
        Assert.False(string.IsNullOrEmpty(tooltip));
        var markId = await label.EvaluateAsync<string>("node=>node.closest('[data-cfx-label-for]')?.dataset.cfxLabelFor || ''");
        Assert.Matches("^series-0-point-[0-2]$", markId);
        var sourcePoint = markId["series-0-point-".Length..];
        var native = page.Locator($"[data-cfx-point='{sourcePoint}'][data-cfx-target-kind='point']");
        var value = await native.GetAttributeAsync("data-cfx-y") ?? await native.GetAttributeAsync("data-cfx-value");
        Assert.False(string.IsNullOrEmpty(value));
        Assert.Contains(value!, tooltip.Replace(",", string.Empty, StringComparison.Ordinal));
        var nativeIdentity = await native.GetAttributeAsync("data-cfx-target-id");
        Assert.False(string.IsNullOrEmpty(nativeIdentity));
        Assert.Contains(nativeIdentity!, hover!);
        Assert.False(string.IsNullOrEmpty(hover));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node=>node.hidden"));

        await MoveAwayAsync(page);
        var empty = await page.EvaluateAsync<double[]>("() => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=35; p.y=35; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y]; }");
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));

        await page.Mouse.ClickAsync((float)x, (float)y);
        Assert.Equal("true", await native.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxLabelEvents.filter(event=>event.type==='cfxselect').length"));
        Assert.Equal(nativeIdentity, await page.EvaluateAsync<string>("() => window.cfxLabelEvents.find(event=>event.type==='cfxselect').detail.target.targetId"));
        Assert.Equal(3, await page.Locator("[data-cfx-point]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-label-for][tabindex],[data-cfx-label-for][data-cfx-target-kind]").CountAsync());
        await native.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await native.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxLabelEvents.filter(event=>event.type==='cfxselect').length"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task DashboardCaptionsResolveRepeatedPointIdsWithinTheirOwnScopedChart() {
        if (!Enabled) return;
        var first = Create(ChartSeriesKind.Pie, true).WithTitle("First chart");
        var second = Create(ChartSeriesKind.Pie, true).WithTitle("Second chart");
        second.Series[0].Points[0] = new ChartPoint(1, 850);
        var html = new[] { first, second }.ToInteractiveHtmlDashboardPage(options => {
            options.IdScope = "caption-dashboard";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        });
        await using var session = await OpenAsync(html, 1440, 760);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxLabelEvents=[]; document.querySelectorAll('.cfx-interactive-chart').forEach(root=>{ for(const type of ['cfxhover','cfxselect']) root.addEventListener(type,event=>window.cfxLabelEvents.push({type,chart:root.dataset.cfxChartId,detail:event.detail})); }); }");
        var roots = page.Locator(".cfx-interactive-chart");
        Assert.Equal(2, await roots.CountAsync());
        var ids = await page.EvaluateAsync<string[][]>("() => Array.from(document.querySelectorAll('.cfx-stage svg')).map(svg=>Array.from(svg.querySelectorAll('[id]')).map(node=>node.id))");
        Assert.Empty(ids[0].Intersect(ids[1], StringComparer.Ordinal));
        for (var index = 0; index < 2; index++) {
            await MoveAwayAsync(page);
            var root = roots.Nth(index); var peer = roots.Nth(1 - index);
            var peerSelection = await peer.Locator("[data-cfx-point='0'][data-cfx-target-kind='point']").GetAttributeAsync("aria-selected");
            var peerTip = await peer.Locator(".cfx-tooltip").EvaluateAsync<string>("tip=>JSON.stringify({hidden:tip.hidden,text:tip.innerText})");
            Assert.Equal("caption-dashboard-" + (index + 1), await root.GetAttributeAsync("data-cfx-chart-id"));
            var label = root.Locator("[data-cfx-label-for='series-0-point-0'] text").First;
            var native = root.Locator("[data-cfx-point='0'][data-cfx-target-kind='point']");
            var nativeIdentity = await native.GetAttributeAsync("data-cfx-target-id");
            var box = await label.BoundingBoxAsync() ?? throw new InvalidOperationException("Scoped caption was not painted.");
            var x = box.X + box.Width * .35; var y = box.Y + box.Height * .45;
            await page.Mouse.MoveAsync((float)x, (float)y, new MouseMoveOptions { Steps = 4 });
            var tooltip = await root.EvaluateAsync<string>("root=>{const tip=root.querySelector('.cfx-tooltip');return tip&&!tip.hidden?tip.innerText:'';}");
            Assert.Contains(index == 0 ? "1200" : "850", tooltip.Replace(",", string.Empty, StringComparison.Ordinal));
            Assert.Contains(nativeIdentity!, (await root.GetAttributeAsync("data-cfx-hover-key"))!);
            Assert.Null(await peer.GetAttributeAsync("data-cfx-hover-key"));
            Assert.Equal(peerTip, await peer.Locator(".cfx-tooltip").EvaluateAsync<string>("tip=>JSON.stringify({hidden:tip.hidden,text:tip.innerText})"));
            await page.Mouse.ClickAsync((float)x, (float)y);
            Assert.Equal("true", await native.GetAttributeAsync("aria-selected"));
            Assert.Equal(index + 1, await page.EvaluateAsync<int>("() => window.cfxLabelEvents.filter(event=>event.type==='cfxselect').length"));
            Assert.Equal(3, await root.Locator("[data-cfx-point][data-cfx-target-kind='point']").CountAsync());
            Assert.Equal(0, await root.Locator("[data-cfx-label-for][tabindex]").CountAsync());
            Assert.Equal(peerSelection, await peer.Locator("[data-cfx-point='0'][data-cfx-target-kind='point']").GetAttributeAsync("aria-selected"));
        }
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "scoped-dashboard.png") });
            await File.WriteAllTextAsync(Path.Combine(capture, "scoped-dashboard.json"), JsonSerializer.Serialize(new { ids,
                events = await page.EvaluateAsync<JsonElement>("() => window.cfxLabelEvents") }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    internal static Chart Create(ChartSeriesKind kind, bool outside) {
        var chart = Chart.Create().WithSize(outside ? 640 : 360, outside ? 420 : 360)
            .WithTheme(outside ? ChartTheme.GraphiteLight() : ChartTheme.GraphiteDark())
            .WithFontFamily("Carlito").WithLegend(false).WithXLabels("North", "South", "East")
            .WithDataLabels().WithDataLabelPlacement(outside ? ChartDataLabelPlacement.Outside : ChartDataLabelPlacement.Inside);
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        // A full column's radial end can leave no collision-free room for a centred Outside candidate.
        if (outside && kind == ChartSeriesKind.RadialColumn) chart.WithDataLabelPlacement(ChartDataLabelPlacement.Right);
        var points = new[] { new ChartPoint(1, 1200), new ChartPoint(2, 300), new ChartPoint(3, 750) };
        switch (kind) {
            case ChartSeriesKind.Pie: chart.AddPie("Requests", points); break;
            case ChartSeriesKind.Donut: chart.AddDonut("Requests", points); break;
            case ChartSeriesKind.RadialBar: chart.AddRadialBar("Requests", points); break;
            case ChartSeriesKind.RadialColumn: chart.AddRadialColumn("Requests", points); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Requests", points); break;
            case ChartSeriesKind.Radar: chart.AddRadar("Requests", points); break;
            case ChartSeriesKind.PolarArea: chart.AddPolarArea("Requests", points); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        chart.Series[0].WithInteractionKey("requests");
        return chart.WithYAxisBounds(0, 1500);
    }

    private static async Task CaptureAsync(IPage page, Chart chart, ChartSeriesKind kind, bool outside, object state) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        var stem = Path.Combine(capture, kind + (outside ? "-outside" : "-inside"));
        await File.WriteAllTextAsync(stem + ".svg", chart.ToSvg());
        await File.WriteAllBytesAsync(stem + "-native.png", chart.ToPng());
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png" });
        await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { state,
            events = await page.EvaluateAsync<JsonElement>("() => window.cfxLabelEvents") }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
