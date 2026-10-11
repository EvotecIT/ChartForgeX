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
    [Theory]
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
        if (horizontal) chart.AddHorizontalBar(name, values);
        else {
            chart.AddScatter(name, values).AddLine("Secondary", new[] { new ChartPoint(1, 2000), new ChartPoint(9, 8000) });
            chart.Series[1].UseSecondaryYAxis();
            chart.WithSecondaryYAxisBounds(0, 10000);
        }
        chart.Series[0].WithInteractionKey("reading-source");
        await using var session = await OpenAsync(Host(false, layout, 340, 1, false, ChartInteractionFeatures.Crosshair, chart), 950, 760);
        var page = session.Page;
        foreach (var index in new[] { 0, 2 }) {
            // Readable plots may scroll; acquire each real mark without forcing an offscreen edge guide.
            await page.Locator(Point(0, index)).EvaluateAsync("node => { const s=node.closest('.cfx-stage'), m=node.getBoundingClientRect(), b=s.getBoundingClientRect(); s.scrollLeft+=m.x+m.width/2-b.x-b.width/2; }");
            var mark = await BoxAsync(page, Point(0, index));
            var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
            await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
            var guide = await GuideGeometryAsync(page);
            AssertGuideGeometry(guide, native);
            Assert.Equal("reading-source:" + index, guide.GetProperty("targetId").GetString());
            await CaptureAsync(session, "overlay-plot-" + (horizontal ? "horizontal" : "secondary") + "-" + layout + "-" + index, new { native, guide });
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit)]
    [InlineData(HtmlChartResponsiveLayout.Readable)]
    public async Task ProgrammaticViewportAndResetDiscardStaleGuidesAndReprojectTheSameObservation(HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var features = ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.StateBookmarks;
        await using var session = await OpenAsync(Host(false, layout, 340, 0.8, false, features, includeResetButton: true), 950, 760);
        var page = session.Page;
        await PrepareHostAsync(page, layout, 340, false);
        // A longer host transition makes acquisition during motion observable without timing a performance budget.
        await page.Locator(".cfx-stage svg").EvaluateAsync("svg => svg.style.transitionDuration = '500ms'");
        await MoveToAsync(page, Point(0, 1));
        Assert.True(await page.Locator(".cfx-crosshair").IsVisibleAsync());
        await page.Locator(Root).EvaluateAsync("r => r.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{viewport:{zoom:1.35,panX:25,panY:-10}}}}))");
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await MoveToAsync(page, Point(0, 1));
        Assert.True(await page.Locator(".cfx-crosshair").IsVisibleAsync());
        await FinishViewportTransitionAsync(page);
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await MoveToAsync(page, Point(0, 1));
        var mark = await BoxAsync(page, Point(0, 1));
        var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        var changed = await GuideGeometryAsync(page);
        AssertGuideGeometry(changed, native);
        Assert.Equal("reading-source:1", changed.GetProperty("targetId").GetString());
        await CaptureAsync(session, "overlay-plot-viewport-" + layout, new { native, changed });

        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        await FinishViewportTransitionAsync(page);
        await MoveToAsync(page, Point(0, 1));
        mark = await BoxAsync(page, Point(0, 1));
        native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        var reset = await GuideGeometryAsync(page);
        AssertGuideGeometry(reset, native);
        Assert.Equal("reading-source:1", reset.GetProperty("targetId").GetString());
        await CaptureAsync(session, "overlay-plot-reset-" + layout, new { native, reset });
        AssertNoConsoleErrors(session);
    }

    [Theory]
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
        Assert.Contains("Reading", await TooltipTextAsync(session.Page), StringComparison.Ordinal);
        Assert.True(await session.Page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Null(await session.Page.Locator(Root).GetAttributeAsync("data-cfx-crosshair"));
        AssertNoConsoleErrors(session);
    }

    private static Task FinishViewportTransitionAsync(IPage page) => page.Locator(".cfx-stage svg").EvaluateAsync(
        "svg => new Promise(resolve => requestAnimationFrame(resolve)).then(() => Promise.all(svg.getAnimations().map(a => a.finished)))");
}
