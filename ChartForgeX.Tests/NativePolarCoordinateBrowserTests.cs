using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NativePolarCoordinateBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Polar)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Gauge)]
    public async Task CommonPolarProducerRetainsNativeHitAndLeavesNearbyEmptySpaceWithoutCrosshair(ChartSeriesKind kind) {
        var chart = Fixture(kind);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.TryGetValue("data-cfx-coordinate-system", out var system) && system == "polar");
        if (!Enabled) return;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var target = page.Locator("[data-cfx-coordinate-system='polar'] [data-cfx-point]").First;
        var expected = await target.GetAttributeAsync("data-cfx-target-id");
        var painted = await target.EvaluateAsync<double[]>("node => { const box=node.getBoundingClientRect(); for(let y=1;y<20;y++) for(let x=1;x<20;x++) { const px=box.left+box.width*x/20, py=box.top+box.height*y/20; const hit=document.elementFromPoint(px,py); if(hit && hit.closest('[data-cfx-point]')===node && !hit.hasAttribute('data-cfx-browser-hit-area')) return [px,py]; } throw new Error('No native painted observation'); }");
        await page.Mouse.MoveAsync((float)painted[0], (float)painted[1]);
        Assert.Equal(expected, await page.Locator(".cfx-hovered[data-cfx-point]").GetAttributeAsync("data-cfx-target-id"));
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        var empty = await target.EvaluateAsync<double[]>("node => { const box=node.getBoundingClientRect(), svg=node.ownerSVGElement, view=svg.getBoundingClientRect(), cx=box.left+box.width/2, cy=box.top+box.height/2; for(let y=-110;y<=110;y+=10) for(let x=-110;x<=110;x+=10) { if(Math.hypot(x,y)>115) continue; const px=cx+x, py=cy+y; if(px<view.left || px>view.right || py<view.top || py>view.bottom) continue; const hit=document.elementFromPoint(px,py); if(hit && svg.contains(hit) && !hit.closest('[data-cfx-point],[data-cfx-role=legend-item]')) return [px,py]; } throw new Error('No nearby empty polar space'); }");
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await page.Mouse.ClickAsync((float)painted[0], (float)painted[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public void LinearGaugeAndScalarCircleDoNotDeclarePolarObservationGeometry() {
        Assert.DoesNotContain("data-cfx-coordinate-system=\"polar\"", Chart.Create().AddLinearGauge("Linear", 80).ToSvg(), StringComparison.Ordinal);
        var circle = Chart.Create().AddCircle("Scalar", 80).Prepare(VisualExportRequest.ForChart(Chart.Create()).Context).Scene;
        Assert.DoesNotContain(circle.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-point"));
    }

    private static Chart Fixture(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithDataLabels(false).WithXLabels("North", "South", "East", "West");
        var points = new[] { new ChartPoint(1, 60), new ChartPoint(2, 80), new ChartPoint(3, 50), new ChartPoint(4, 70) };
        switch (kind) {
            case ChartSeriesKind.Pie: chart.AddPie("Observed", points); break;
            case ChartSeriesKind.Donut: chart.AddDonut("Observed", points); break;
            case ChartSeriesKind.Radar: chart.AddRadar("Observed", points); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Observed", points); break;
            case ChartSeriesKind.PolarArea: chart.AddPolarArea("Observed", points); break;
            case ChartSeriesKind.ProgressRing: chart.AddProgressRing("Observed", points); break;
            case ChartSeriesKind.Sunburst: chart.AddSunburst("Observed", new[] { new ChartNode("All", "All"), new ChartNode("North", "North"), new ChartNode("South", "South"), new ChartNode("East", "East") }, new[] { new ChartTreeLink("All", "North", 60), new ChartTreeLink("All", "South", 80), new ChartTreeLink("All", "East", 50) }); break;
            case ChartSeriesKind.Gauge: chart.AddGauge("Observed", 80); break;
        }
        return chart;
    }
}
