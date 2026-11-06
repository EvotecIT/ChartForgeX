using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    [BrowserTheory]
    [InlineData(false, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, HtmlChartResponsiveLayout.Fit)]
    [InlineData(false, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable)]
    public async Task CompactHorizontalAndSecondaryAxisGuidesExcludeTheTitleAndContainLongLabels(bool horizontal, HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Guides stay below this heading")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var values = new[] { new ChartPoint(1, 5), new ChartPoint(5, 5), new ChartPoint(9, 9) };
        const string name = "Reading with a long observation label that must fit the plot";
        chart.WithXLabels(values.Select(point => new ChartAxisLabel(point.X, name + " " + point.X)));
        if (horizontal) chart.AddSeries(new ChartSeries(new ChartSeriesId("reading-source"), name, ChartSeriesKind.HorizontalBar, values));
        else {
            chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("reading-source"), name, values)).AddLine("Secondary", new[] { new ChartPoint(1, 2000), new ChartPoint(9, 8000) });
            chart.Series[1].UseSecondaryValueAxis();
            chart.WithSecondaryYAxisBounds(0, 10000);
        }
        await using var session = await OpenAsync(Host(false, layout, 340, 1, false, ChartInteractionFeatures.Crosshair, chart), 950, 760);
        var page = session.Page;
        foreach (var index in new[] { 0, 2 }) {
            // Readable plots may scroll; acquire each real mark without forcing an offscreen edge guide.
            await page.Locator(Point(0, index)).EvaluateAsync("node => { const s=node.closest('.cfx-stage'), m=node.getBoundingClientRect(), b=s.getBoundingClientRect(); s.scrollLeft+=m.x+m.width/2-b.x-b.width/2; }");
            // Let scroll handlers discard the previous guide before acquiring the new mark.
            await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
            var mark = await BoxAsync(page, Point(0, index));
            var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
            await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
            var guide = await GuideGeometryAsync(page);
            AssertGuideGeometry(guide, native);
            Assert.Contains(guide.GetProperty("axisLabels").EnumerateArray(), label => label.GetProperty("text").GetString()!.Contains(name, StringComparison.Ordinal));
            Assert.Equal("reading-source:" + index, guide.GetProperty("targetId").GetString());
            await CaptureAsync(session, "overlay-plot-" + (horizontal ? "horizontal" : "secondary") + "-" + layout + "-" + index, new { native, guide });
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlChartResponsiveLayout.Fit)]
    [InlineData(HtmlChartResponsiveLayout.Readable)]
    public async Task ProgrammaticViewportAndResetDiscardStaleGuidesAndReprojectTheSameObservation(HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var features = ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.StateBookmarks;
        await using var session = await OpenAsync(Host(false, layout, 340, 0.8, false, features, includeResetButton: true), 950, 760);
        var page = session.Page;
        await PrepareHostAsync(page, layout, 340, false);
        var originalMark = await BoxAsync(page, PointMark(0, 1));
        var frame = await BoxAsync(page, ".cfx-stage svg");
        await MoveToAsync(page, Point(0, 1));
        Assert.True(await page.Locator(".cfx-crosshair").IsVisibleAsync());
        await page.Locator(Root).EvaluateAsync("r => r.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{nativeViewport:{x:{minimum:1,maximum:7},y:{minimum:2,maximum:9}}}}}))");
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        // Reacquire only after the owned transition settles; automation latency is not a motion budget.
        await FinishViewportTransitionAsync(page);
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        var window = await ViewportAsync(page);
        AssertWindow(window.GetProperty("x"), 1, 7);
        AssertWindow(window.GetProperty("y"), 2, 9);
        var projectedMark = await BoxAsync(page, PointMark(0, 1));
        Assert.True(Math.Abs(projectedMark.X - originalMark.X) > 1);
        var changedFrame = await BoxAsync(page, ".cfx-stage svg");
        Assert.Equal(frame.X, changedFrame.X, 3);
        Assert.Equal(frame.Y, changedFrame.Y, 3);
        Assert.Equal(frame.Width, changedFrame.Width, 3);
        Assert.Equal(frame.Height, changedFrame.Height, 3);
        await MoveToAsync(page, Point(0, 1));
        Assert.True(await page.Locator(".cfx-crosshair").IsVisibleAsync());
        var mark = await BoxAsync(page, Point(0, 1));
        var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        var changed = await GuideGeometryAsync(page);
        AssertGuideGeometry(changed, native);
        Assert.Equal("reading-source:1", changed.GetProperty("targetId").GetString());
        await CaptureAsync(session, "overlay-plot-viewport-" + layout, new { native, changed, window, originalMark, projectedMark, frame, changedFrame });

        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await FinishViewportTransitionAsync(page);
        var resetWindow = await ViewportAsync(page);
        AssertWindow(resetWindow.GetProperty("x"), 0, 10);
        AssertWindow(resetWindow.GetProperty("y"), 0, 10);
        var resetMark = await BoxAsync(page, PointMark(0, 1));
        Assert.InRange(resetMark.X - originalMark.X, -1, 1);
        Assert.InRange(resetMark.Y - originalMark.Y, -1, 1);
        await MoveToAsync(page, Point(0, 1));
        mark = await BoxAsync(page, Point(0, 1));
        native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        var reset = await GuideGeometryAsync(page);
        AssertGuideGeometry(reset, native);
        Assert.Equal("reading-source:1", reset.GetProperty("targetId").GetString());
        await CaptureAsync(session, "overlay-plot-reset-" + layout, new { native, reset, resetWindow, resetMark });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrAmbiguousPlotMetadataRetainsNativeTooltipsWithoutGuessingAGuide(bool ambiguous) {
        if (!Enabled) return;
        var html = Host(false, HtmlChartResponsiveLayout.Fit, 650, 1, false, ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Crosshair);
        // Exercise the serialized host boundary before initialization, not an adapter-private test hook.
        html = Regex.Replace(html, "data-cfx-prepared-chart=\"([^\"]+)\"", match => {
            var metadata = JsonNode.Parse(WebUtility.HtmlDecode(match.Groups[1].Value))!;
            var regions = metadata["regions"]!.AsArray();
            var plot = regions.Single(item => (string?)item!["role"] == "cartesian-plot")!;
            if (ambiguous) regions.Add(plot.DeepClone()); else regions.Remove(plot);
            return "data-cfx-prepared-chart=\"" + WebUtility.HtmlEncode(metadata.ToJsonString()) + "\"";
        });
        await using var session = await OpenAsync(html, 950, 760);
        await MoveToAsync(session.Page, Point(0, 1));
        Assert.Equal("Observation 5", await session.Page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Contains("5", await session.Page.Locator(".cfx-tooltip__meta dd").AllTextContentsAsync());
        Assert.True(await session.Page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Null(await session.Page.Locator(Root).GetAttributeAsync("data-cfx-crosshair"));
        AssertNoConsoleErrors(session);
    }

    private static Task FinishViewportTransitionAsync(IPage page) => page.Locator(".cfx-stage svg").EvaluateAsync(
        "async svg => { await new Promise(resolve => requestAnimationFrame(resolve)); await Promise.all(svg.closest('.cfx-interactive-chart').getAnimations({ subtree: true }).map(a => a.finished)); await new Promise(resolve => requestAnimationFrame(resolve)); }");
}
