using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePolarHitBrowserTests {
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    public async Task NativeSectorHitRetainsObservationAndEmptyPolarSpaceHasNoInferredTarget(bool donut, bool dark, bool compact) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(compact ? 320 : 600, 340)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithLegend(false).WithDataLabels(false).WithXLabels("North", "South");
        var points = new[] { new ChartPoint(1, 1200), new ChartPoint(2, 300) };
        if (donut) chart.AddDonut("Requests", points); else chart.AddPie("Requests", points);
        chart.Series[0].WithInteractionKey("requests");
        var mark = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneSlice>().First();
        // Existing producers supply the fixture; the explicit coordinate contract keeps this adapter test independent of new Core APIs.
        await using var session = await OpenAsync(PolarContract(chart.ToInteractiveHtmlPage()), compact ? 360 : 680, 480);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var angle = mark.Start + mark.Sweep * .985;
        var radius = (mark.Inner + mark.Outer) / 2;
        var painted = await ScreenAsync(page, mark.Cx + Math.Cos(angle) * radius, mark.Cy + Math.Sin(angle) * radius);
        await page.Mouse.MoveAsync((float)painted[0], (float)painted[1]);
        Assert.Matches("1,?200", await TooltipTextAsync(page));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        var emptyAngle = mark.Start + mark.Sweep + (Math.PI * 2 - mark.Sweep) / 2;
        var empty = donut ? await ScreenAsync(page, mark.Cx, mark.Cy)
            : await ScreenAsync(page, mark.Cx + Math.Cos(emptyAngle) * (mark.Outer + 8), mark.Cy + Math.Sin(emptyAngle) * (mark.Outer + 8));
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await page.Mouse.ClickAsync((float)painted[0], (float)painted[1]);
        Assert.Equal("requests:0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetId"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task NestedPolarObservationInheritsAuthoredSeriesIdentity() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithDataLabels(false)
            .AddPolar("First", new[] { new ChartPoint(0, 40), new ChartPoint(1, 50) })
            .AddPolar("Second", new[] { new ChartPoint(2, 80), new ChartPoint(3, 70) }).WithYAxisBounds(0, 100);
        chart.Series[0].WithInteractionKey("first"); chart.Series[1].WithInteractionKey("second");
        await using var session = await OpenAsync(PolarContract(chart.ToInteractiveHtmlPage()));
        var page = session.Page;
        var target = page.Locator("[data-cfx-role='polar-series'][data-cfx-series='1'] [data-cfx-point='0']");
        Assert.Equal("second:0", await target.GetAttributeAsync("data-cfx-target-id"));
        var screen = await target.EvaluateAsync<double[]>("node => { const mark=node.querySelector('ellipse'); const point=node.ownerSVGElement.createSVGPoint(); point.x=mark.cx.baseVal.value; point.y=mark.cy.baseVal.value; const screen=point.matrixTransform(mark.getScreenCTM()); return [screen.x,screen.y]; }");
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1]);
        Assert.Contains("Second", await TooltipTextAsync(page));
        Assert.Contains("80", await TooltipTextAsync(page));
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    private static string PolarContract(string html) => html.Insert(html.IndexOf("<svg ", StringComparison.Ordinal) + 5, "data-cfx-coordinate-system=\"polar\" ");
    private static Task<double[]> ScreenAsync(IPage page, double x, double y) => page.EvaluateAsync<double[]>(
        "point => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y]; }", new { x, y });
}
