using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    [Theory]
    [InlineData(false, 650, 1)]
    [InlineData(true, 340, 0.8)]
    public async Task ExplicitPlotEdgesRetainGuideInkAndRejectOffPlotObservationAnchors(bool graphite, int width, double scale) {
        if (!Enabled) return;
        // Use the public chart path that exposed the three-decimal SVG/full-precision plot mismatch.
        var chart = Chart.Create().WithSize(600, 440).WithTitle("Explicit plot edges").WithLegend(false).WithDataLabels(false)
            .WithTheme(graphite ? ChartTheme.GraphiteDark() : ChartTheme.Light())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Boundary", new[] {
                new ChartPoint(0, 5), new ChartPoint(10, 5), new ChartPoint(5, 0),
                new ChartPoint(5, 10), new ChartPoint(5, 5), new ChartPoint(10.01, 2)
            });
        chart.Series[0].WithInteractionKey("boundary-source").WithPointLabel(5, "Outside plot");
        var features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Crosshair;
        await using var session = await OpenAsync(Host(graphite, HtmlChartResponsiveLayout.Fit, width, scale, false, features, chart), 950, 760);
        var page = session.Page;
        foreach (var customStroke in new[] { false, true }) {
            chart.WithPlotClipping();
            Action<HtmlChartCrosshairOptions>? configure = customStroke ? options => {
                options.Stroke = ChartColor.FromRgb(37, 99, 235);
                options.StrokeWidth = 3;
                options.LineStyle = HtmlChartCrosshairLineStyle.Dotted;
            } : null;
            if (customStroke) {
                await page.GotoAsync("about:blank");
                await page.SetContentAsync(Host(graphite, HtmlChartResponsiveLayout.Fit, width, scale, false, features, chart,
                    configureCrosshair: configure));
            }
            var variant = (graphite ? "graphite-compact-scaled" : "ordinary-wide") + (customStroke ? "-custom" : "-default");
            var edges = new[] { "left", "right", "bottom", "top", "interior" };
            for (var index = 0; index < edges.Length; index++) {
                var mark = await BoxAsync(page, Point(0, index));
                var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
                await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
                var guide = await GuideGeometryAsync(page);
                var ink = await ClippedGuideInkAsync(page);
                await CaptureAsync(session, "overlay-edge-" + variant + "-" + edges[index], new { native, guide, ink });
                AssertGuideGeometry(guide, native);
                Assert.Equal("boundary-source:" + index, guide.GetProperty("targetId").GetString());
                // A visible overlay alone misses the bug: the complete right/bottom stroke could be clipped away.
                foreach (var line in ink.EnumerateArray()) {
                    Assert.True(line.GetProperty("painted").GetBoolean());
                    Assert.True(line.GetProperty("clippedWidth").GetDouble() > 0);
                    Assert.True(line.GetProperty("clippedHeight").GetDouble() > 0);
                }
            }
            // Unclipped marks are supported, but do not enlarge the plot's guide domain.
            // Their native readout remains available without clamping an off-plot centre onto an edge.
            chart.WithPlotClipping(false);
            await page.GotoAsync("about:blank");
            await page.SetContentAsync(Host(graphite, HtmlChartResponsiveLayout.Fit, width, scale, false, features, chart,
                configureCrosshair: configure));
            var outside = await BoxAsync(page, Point(0, 5));
            var pointer = new double[] { outside.X + outside.Width / 2, outside.Y + outside.Height / 2 };
            Assert.True(await page.Locator(Point(0, 5)).EvaluateAsync<bool>(
                "(node,p) => node.contains(document.elementFromPoint(p[0],p[1]))", pointer));
            await page.Mouse.MoveAsync((float)pointer[0], (float)pointer[1]);
            var tooltip = await TooltipTextAsync(page);
            var guideHidden = await page.Locator(".cfx-crosshair").IsHiddenAsync();
            var target = await page.Locator(Root).GetAttributeAsync("data-cfx-crosshair");
            await CaptureAsync(session, "overlay-edge-" + variant + "-outside", new { pointer, tooltip, guideHidden, target });
            Assert.Contains("Outside plot", tooltip, StringComparison.Ordinal);
            Assert.True(guideHidden);
            Assert.Null(target);
        }
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> ClippedGuideInkAsync(IPage page) => page.Locator(".cfx-crosshair").EvaluateAsync<JsonElement>("""
        guide => {
            const clip=guide.getBoundingClientRect();
            return Array.from(guide.querySelectorAll('.cfx-crosshair__line')).map(line=>{
                const box=line.getBoundingClientRect(), style=getComputedStyle(line), horizontal=line.classList.contains('cfx-crosshair__line--x');
                const border=horizontal?style.borderTopWidth:style.borderLeftWidth, pattern=horizontal?style.borderTopStyle:style.borderLeftStyle;
                return {axis:horizontal?'horizontal':'vertical', border, pattern, background:style.backgroundColor,
                    painted:parseFloat(border)>0 && pattern!=='none' || !['transparent','rgba(0, 0, 0, 0)'].includes(style.backgroundColor),
                    clippedWidth:Math.max(0,Math.min(box.right,clip.right)-Math.max(box.left,clip.left)),
                    clippedHeight:Math.max(0,Math.min(box.bottom,clip.bottom)-Math.max(box.top,clip.top))};
            });
        }
        """);
}
