using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianStyledAnnotationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MarkersAndReferenceStrokesShareTheSameAutomaticBounds(bool horizontal) {
        var chart = Chart.Create().WithSize(640, 640).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithLineMarkers(ChartLineMarkerMode.All)
            .AddLine("Observed", new[] { new ChartPoint(0, 0), new ChartPoint(10, 10) });
        chart.Series[0].WithMarkerRadius(0);
        if (horizontal) chart.AddHorizontalLine(10, ""); else chart.AddVerticalLine(10, "");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 0);
        var plot = Plot(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        var pixels = horizontal ? plot.Height : plot.Width;
        chart.Series[0].WithMarkerRadius(pixels * .3);
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), pixels * .8, .15);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        foreach (var marker in actual.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker")) {
            Assert.InRange(horizontal ? marker.Cy - marker.Ry : marker.Cx - marker.Rx,
                (horizontal ? plot.Top : plot.Left) - .001, horizontal ? plot.Bottom : plot.Right);
            Assert.InRange(horizontal ? marker.Cy + marker.Ry : marker.Cx + marker.Rx,
                horizontal ? plot.Top : plot.Left, (horizontal ? plot.Bottom : plot.Right) + .001);
        }
        var guide = Assert.Single(actual.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        Assert.InRange(horizontal ? guide.Commands[0].Y : guide.Commands[0].X,
            (horizontal ? plot.Top : plot.Left) + pixels * .4 - .001,
            (horizontal ? plot.Bottom : plot.Right) - pixels * .4 + .001);
        await Capture(actual, "cartesian-joint-marker-guide-" + (horizontal ? "horizontal" : "vertical"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnOversizedGuideDoesNotCancelBoundsForAContainableGuide(bool horizontal) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).AddLine("Observed", new[] { new ChartPoint(0, 0), new ChartPoint(10, 10) });
        chart.Series[0].WithMarkerRadius(0);
        if (horizontal) { chart.AddHorizontalLine(0, ""); chart.AddHorizontalLine(5, ""); }
        else { chart.AddVerticalLine(0, ""); chart.AddVerticalLine(5, ""); }
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 40);
        chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: 0);
        var control = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var before = control.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "annotation-line");
        chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 1024, .15);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var after = actual.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "annotation-line");
        Assert.Equal(horizontal ? before.Commands[0].Y : before.Commands[0].X, horizontal ? after.Commands[0].Y : after.Commands[0].X, 6);
        await Capture(actual, "cartesian-oversized-guide-" + (horizontal ? "horizontal" : "vertical"));
    }

    [Fact]
    public void MinimumAcceptedReferenceDashesRenderThroughBothPublicRasterPathsOnAWideCanvas() {
        var chart = Chart.Create().WithSize(30000, 100).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }).AddHorizontalLine(5, "");
        chart.Series[0].WithMarkerRadius(0);
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 1, dash: new[] { .25, .25 });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.NotEmpty(prepared.ToPng());
        var image = ChartForgeX.SvgRaster.SvgRasterizer.ToImage(prepared.ToSvg());
        Assert.Equal(30000, image.Width);
        Assert.Equal(100, image.Height);
        Assert.Contains(image.Pixels, value => value > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CenteredWideGuideDoesNotAssignItsWidthToTheNarrowEdgeGuide(bool horizontal) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).AddLine("Observed", new[] { new ChartPoint(0, 0), new ChartPoint(10, 10) });
        chart.Series[0].WithMarkerRadius(0);
        if (horizontal) { chart.AddHorizontalLine(0, ""); chart.AddHorizontalLine(5, ""); }
        else { chart.AddVerticalLine(0, ""); chart.AddVerticalLine(5, ""); }
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 2);
        chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: 0);
        var control = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = Plot(control);
        var before = control.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "annotation-line");
        chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: (horizontal ? plot.Height : plot.Width) * .8);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var after = actual.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "annotation-line");
        Assert.Equal(horizontal ? before.Commands[0].Y : before.Commands[0].X, horizontal ? after.Commands[0].Y : after.Commands[0].X, 6);
    }

    [Theory]
    [InlineData(false, false, ChartScaleKind.Linear)]
    [InlineData(false, true, ChartScaleKind.Linear)]
    [InlineData(true, false, ChartScaleKind.Linear)]
    [InlineData(true, true, ChartScaleKind.Linear)]
    [InlineData(false, false, ChartScaleKind.Logarithmic)]
    [InlineData(false, true, ChartScaleKind.Logarithmic)]
    [InlineData(true, false, ChartScaleKind.Logarithmic)]
    [InlineData(true, true, ChartScaleKind.Logarithmic)]
    [InlineData(false, false, ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(false, true, ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(true, false, ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(true, true, ChartScaleKind.SymmetricLogarithmic)]
    public async Task AutomaticAxesKeepTheCompleteReferenceStroke(bool horizontal, bool maximum, ChartScaleKind scale) {
        var chart = Create(horizontal, maximum, scale);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "annotation-line");
        var plot = Plot(prepared);
        var coordinate = horizontal ? line.Commands[0].Y : line.Commands[0].X;
        Assert.InRange(coordinate, (horizontal ? plot.Top : plot.Left) + 20 - .001,
            (horizontal ? plot.Bottom : plot.Right) - 20 + .001);
        var pixels = prepared.ToRgba();
        var sample = (int)Math.Round(horizontal ? plot.Left + plot.Width / 2 : plot.Top + plot.Height / 2);
        var count = 0;
        for (var position = 0; position < (horizontal ? pixels.Height : pixels.Width); position++) {
            var offset = horizontal ? (position * pixels.Width + sample) * 4 : (sample * pixels.Width + position) * 4;
            if (pixels.Pixels[offset] > 240 && pixels.Pixels[offset + 1] < 20 && pixels.Pixels[offset + 2] > 240) count++;
        }
        Assert.InRange(count, 39, 41);
        if (!InteractiveChartBrowser.Enabled || scale != ChartScaleKind.Linear) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 380);
        Assert.Equal("40px", await session.Page.Locator("[data-cfx-role='annotation-line']").EvaluateAsync<string>("element => getComputedStyle(element).strokeWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "cartesian-" + (horizontal ? "horizontal-" : "vertical-") + (maximum ? "maximum" : "minimum");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AuthoredAxisEndsRetainTheirExplicitClippingBoundary(bool horizontal, bool maximum) {
        var chart = Create(horizontal, maximum, ChartScaleKind.Linear);
        if (horizontal) chart.WithYAxisBounds(1, 10); else chart.WithXAxisBounds(1, 10);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "annotation-line");
        var plot = Plot(prepared);
        Assert.Equal(horizontal ? maximum ? plot.Top : plot.Bottom : maximum ? plot.Right : plot.Left,
            horizontal ? line.Commands[0].Y : line.Commands[0].X, 6);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OneAuthoredEndLeavesTheOtherEndAvailableForTheStroke(bool horizontal, bool maximum) {
        var chart = Create(horizontal, maximum, ChartScaleKind.Linear);
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        if (maximum) axis.Minimum = 1; else axis.Maximum = 10;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "annotation-line");
        var plot = Plot(prepared);
        var coordinate = horizontal ? line.Commands[0].Y : line.Commands[0].X;
        Assert.InRange(coordinate, (horizontal ? plot.Top : plot.Left) + 20 - .001,
            (horizontal ? plot.Bottom : plot.Right) - 20 + .001);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HorizontalBarsReserveValueAndCategoryReferenceStrokes(bool horizontal, bool maximum) {
        var chart = Create(horizontal, maximum, ChartScaleKind.Linear);
        chart.Series.Clear();
        chart.AddHorizontalBar("Observed", new[] { new ChartPoint(2, 2), new ChartPoint(8, 8) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "annotation-line");
        var plot = Plot(prepared);
        Assert.InRange(horizontal ? line.Commands[0].Y : line.Commands[0].X,
            (horizontal ? plot.Top : plot.Left) + 20 - .001, (horizontal ? plot.Bottom : plot.Right) - 20 + .001);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnpaintedStrokesDoNotCompressTheCartesianDomain(int hiddenStyle) {
        var chart = Create(false, true, ChartScaleKind.Linear);
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 0);
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Annotations[0].StrokeStyle = hiddenStyle switch {
            0 => new ChartStrokeStyle(ChartColors.Transparent, 1024),
            1 => new ChartStrokeStyle(width: 1024, opacity: 0),
            _ => new ChartStrokeStyle(width: 0)
        };
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(before.ToPng(), after.ToPng());
    }

    private static async Task Capture(PreparedVisual prepared, string name) {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 640);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
    }

    private static Chart Create(bool horizontal, bool maximum, ChartScaleKind scale) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .AddLine("Observed", new[] { new ChartPoint(2, 2), new ChartPoint(8, 8) });
        chart.Series[0].WithMarkerRadius(0);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).Scale = scale;
        if (horizontal) chart.AddHorizontalLine(maximum ? 10 : 1, ""); else chart.AddVerticalLine(maximum ? 10 : 1, "");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 40, dash: Array.Empty<double>());
        return chart;
    }

    private static ChartRect Plot(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(group => group.Clip.HasValue).Select(group => group.Clip!.Value).OrderBy(bounds => bounds.Width * bounds.Height).First();
}
