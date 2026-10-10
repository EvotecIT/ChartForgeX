using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer, true, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, false, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, false)]
    [InlineData(HtmlChartTooltipAnchor.Pointer, false, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, true, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, true)]
    public async Task PointerExitPositionsTheFocusedZeroOrPrecisionFactWithoutAcquiringItByPointer(HtmlChartTooltipAnchor anchor, bool bars, bool precision) {
        if (!Enabled) return;
        var placements = FocusRecoveryPlacements(anchor);
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithHeader(false)
            .WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithTheme(precision ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()), bars, "Observed",
            new ChartPoint(1, precision ? 1 : 0), new ChartPoint(2, 100000000));
        var fragment = chart.ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
            options.Tooltip.Position.Gap = 11.5;
            options.Tooltip.Position.OffsetX = -3.25;
            options.Tooltip.Position.OffsetY = 5.75;
        });
        await using var session = await OpenAsync("<html><body style='margin:20px'>" + fragment + "</body></html>", precision ? 340 : 950, 760);
        var page = session.Page;
        await MountPositionFocusChartAsync(page, precision);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Reading);
        var initial = await AssertPositionAsync(page, anchor, placements, 11.5, -3.25, 5.75, hosted: true);
        var status = await page.EvaluateAsync<string>("selector=>window.positionFocusChart.querySelector(selector).dataset.cfxGeometryStatus", Reading);
        Assert.Equal(precision ? "precision-collapse" : "zero", status);
        var mark = NumericRadialSeriesTests.Marks(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene).Last();
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var point = await page.EvaluateAsync<double[]>("p=>{const node=window.positionFocusChart.querySelector(p.selector),point=new DOMPoint(p.x,p.y).matrixTransform(node.ownerSVGElement.getScreenCTM());return[point.x,point.y];}",
            new { selector = Point(0, 1), x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync((float)point[0], (float)point[1], new MouseMoveOptions { Steps = 4 });
        await page.WaitForFunctionAsync("selector=>window.positionFocusChart.dataset.cfxHoverKey?.includes(window.positionFocusChart.querySelector(selector).dataset.cfxTargetId)", Point(0, 1));
        var pointer = await GeometryAsync(page, Point(0, 1), hosted: true);
        Assert.NotEqual(initial.GetProperty("text").GetString(), pointer.GetProperty("text").GetString());
        await MoveAwayAsync(page);
        var recovered = await GeometryAsync(page, hosted: true);
        var lifetime = await PositionFocusLifetimeAsync(page);
        await CaptureAsync(page, $"position-focus-{(precision ? "precision" : "zero")}-{(bars ? "bar" : "column")}-{anchor}", new { initial, status, point, pointer, recovered, lifetime }, session);
        Assert.Equal(initial.GetProperty("text").GetString(), recovered.GetProperty("text").GetString());
        Assert.Equal("point", lifetime.GetProperty("focusedKind").GetString());
        await AssertPositionAsync(page, anchor, placements, 11.5, -3.25, 5.75, hosted: true);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).blur()", Reading);
        Assert.True(await page.EvaluateAsync<bool>("()=>window.positionFocusChart.querySelector('.cfx-tooltip').hidden"));
        AssertNoConsoleErrors(session);
    }
}
