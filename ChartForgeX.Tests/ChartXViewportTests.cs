using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartXViewportTests {
    private static readonly ChartColor Blue = new(20, 60, 220);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CrossingPathsRemainPaintedWithoutAnyObservationInsideTheWindow(bool step, bool reversed) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) };
        if (step) chart.AddStepLine("Observed", points, Blue); else chart.AddLine("Observed", points, Blue);
        chart.Series[0].Markers.Enabled = false;
        chart.Options.XAxis.Reversed = reversed; chart.Options.ClipMarksToPlot = false;
        var prepared = chart.Prepare(Context(), new ChartXViewport(3, 7));
        var projection = Assert.IsType<ChartXProjection>(prepared.XProjection);
        Assert.Equal(3, projection.Minimum); Assert.Equal(7, projection.Maximum);
        Assert.Equal(0, projection.FullMinimum); Assert.Equal(10, projection.FullMaximum);
        Assert.Equal(reversed, projection.Reversed);
        Assert.Equal(projection.PlotBounds, Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        Assert.Equal(projection.Project(0), line.Commands[0].X);
        Assert.Equal(projection.Project(10), line.Commands[line.Commands.Count - 1].X);
        var pointGroups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "0", "10" }, pointGroups.Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.Equal(new[] { "0", "1" }, pointGroups.Select(node => (string?)node.Attribute("data-cfx-source-point")));
        var image = prepared.ToRgba();
        AssertBlueNear(image, projection.Project(5), projection.PlotBounds.Bottom - projection.PlotBounds.Height * (step ? .2 : .5));
        AssertBlueInside(image, projection.PlotBounds);
        Assert.False(chart.Options.ClipMarksToPlot);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BreakBeforePreventsAnInventedCrossingWhileRetainingSourceIdentity(bool step) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(10, 8, breakBefore: true) };
        if (step) chart.AddStepLine("Disconnected", points, Blue); else chart.AddLine("Disconnected", points, Blue);
        chart.Series[0].Markers.Enabled = false;
        var prepared = chart.Prepare(Context(), new ChartXViewport(3, 7));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        Assert.Equal(2, line.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Empty(BluePixels(prepared.ToRgba()));
    }

    [Fact]
    public void ExactWindowClipsEdgeMarkersWithoutAutomaticXExpansionOrPointColorChanges() {
        var chart = Bare().WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 5), new ChartPoint(3, 5), new ChartPoint(7, 5), new ChartPoint(10, 5) }, Blue);
        chart.Options.ClipMarksToPlot = false;
        chart.Series[0].ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 14; });
        var second = new ChartColor(220, 60, 20);
        chart.Series[0].WithPointColor(2, second);
        var prepared = chart.Prepare(Context(), new ChartXViewport(3, 7));
        var projection = Assert.IsType<ChartXProjection>(prepared.XProjection);
        Assert.Equal(3, projection.Minimum); Assert.Equal(7, projection.Maximum);
        Assert.True(projection.FullMinimum < 0); Assert.True(projection.FullMaximum > 10);
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum);
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(2, markers.Length); Assert.Equal(Blue, markers[0].Fill); Assert.Equal(second, markers[1].Fill);
        Assert.Equal(projection.PlotBounds.Left, markers[0].Cx); Assert.Equal(projection.PlotBounds.Right, markers[1].Cx);
        AssertBlueInside(prepared.ToRgba(), projection.PlotBounds);
    }

    [Fact]
    public void FullDataYAndSecondaryYRemainFixedWhileXTicksAreRecomputed() {
        var chart = Chart.Create().WithLegend(false).WithGrid(false)
            .AddLine("Primary", new[] { new ChartPoint(0, -100), new ChartPoint(5, 4), new ChartPoint(10, 100) })
            .AddStepLine("Secondary", new[] { new ChartPoint(0, 1), new ChartPoint(5, 10), new ChartPoint(10, 1000) });
        chart.Series[1].UseSecondaryYAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        foreach (var series in chart.Series) series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 9; });
        var full = chart.Prepare(Context());
        var view = chart.Prepare(Context(), new ChartXViewport(4, 6));
        Assert.Equal(AxisLabels(full, "axis-y-label"), AxisLabels(view, "axis-y-label"));
        Assert.Equal(AxisLabels(full, "axis-secondary-y-label"), AxisLabels(view, "axis-secondary-y-label"));
        Assert.NotEqual(AxisLabels(full, "axis-x-label"), AxisLabels(view, "axis-x-label"));
        Assert.All(view.Regions.Where(region => region.Role == "axis-x-label"), region => {
            Assert.InRange(region.Bounds.Left, view.XProjection!.PlotBounds.Left, view.XProjection.PlotBounds.Right);
        });
        foreach (var series in new[] { 0, 1 }) {
            var id = "series-" + series + "-point-1";
            Assert.Equal(NormalizedY(full, id), NormalizedY(view, id), 10);
        }
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum);
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.SecondaryYAxis.Maximum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlotRegionsClampToVisibleXAndKeepAuthoredBoundsAndAnnotationFacts(bool reversed) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }, Blue)
            .AddPlotRegion(new(new ChartColor(220, 180, 40), xMinimum: 1, xMaximum: 5, yMinimum: 2, yMaximum: 8, opacity: 1, label: "Authored", showLabel: false))
            .AddPlotRegion(new(ChartColor.Black, xMinimum: 8, xMaximum: 9, label: "Outside", showLabel: false))
            .AddVerticalLine(9, "Reference");
        chart.Series[0].Markers.Enabled = false; chart.Options.XAxis.Reversed = reversed;
        var prepared = chart.Prepare(Context(), new ChartXViewport(3, 7));
        var projection = prepared.XProjection!;
        var region = prepared.Regions.Single(item => item.Id == "plot-region-0");
        Assert.Equal(projection.PlotBounds.Width / 2, region.Bounds.Width, 9);
        Assert.Equal(Math.Min(projection.Project(3), projection.Project(5)), region.Bounds.Left, 9);
        Assert.Equal(projection.PlotBounds.Height * .6, region.Bounds.Height, 9);
        Assert.Equal(0, prepared.Regions.Single(item => item.Id == "plot-region-1").Bounds.Width);
        var image = prepared.ToRgba();
        var pixel = ((int)(projection.PlotBounds.Bottom - projection.PlotBounds.Height * .7) * image.Width + (int)projection.Project(4)) * 4;
        Assert.Equal(220, image.Pixels[pixel]); Assert.Equal(180, image.Pixels[pixel + 1]); Assert.Equal(40, image.Pixels[pixel + 2]);
        var svg = XDocument.Parse(prepared.ToSvg());
        var facts = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "plot-region"
            && (string?)node.Attribute("data-cfx-label") == "Authored");
        Assert.Equal("1", (string?)facts.Attribute("data-cfx-x-minimum")); Assert.Equal("5", (string?)facts.Attribute("data-cfx-x-maximum"));
        var annotation = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation");
        Assert.Equal("9", (string?)annotation.Attribute("data-cfx-value"));
        Assert.Equal(1, chart.PlotRegions[0].XMinimum); Assert.Equal(9, chart.Annotations[0].Value);
    }

    [Fact]
    public void PreparationIsDetachedAndOrdinaryPrepareResetsWithoutChangingTheModel() {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) }, Blue);
        chart.Series[0].Markers.Enabled = false;
        var axis = chart.Options.XAxis;
        var full = chart.Prepare(Context()); var originalSvg = full.ToSvg(); var originalPng = full.ToPng();
        var view = chart.Prepare(Context(), new ChartXViewport(3, 7)); var viewSvg = view.ToSvg(); var viewPng = view.ToPng();
        var reset = chart.Prepare(Context());
        Assert.Equal(originalSvg, reset.ToSvg()); Assert.Equal(originalPng, reset.ToPng());
        var fullWindow = chart.Prepare(Context(), new ChartXViewport(full.XProjection!.FullMinimum, full.XProjection.FullMaximum));
        Assert.Equal(originalPng, fullWindow.ToPng());
        Assert.Equal(full.XProjection.PlotBounds, fullWindow.XProjection!.PlotBounds);
        Assert.Same(axis, chart.Options.XAxis);
        Assert.Equal(0, chart.Options.XAxis.Minimum); Assert.Equal(10, chart.Options.XAxis.Maximum);
        Assert.Equal(3, chart.Series[0].Points.Count);
        var projection = view.XProjection!; var captured = projection.Project(5);
        chart.Options.XAxis.Reversed = true; chart.WithXAxisBounds(-100, 100).WithYAxisBounds(-100, 100);
        chart.Series[0].Points.Clear(); chart.WithTitle("Changed");
        Assert.Equal(captured, projection.Project(5)); Assert.Equal(viewSvg, view.ToSvg()); Assert.Equal(viewPng, view.ToPng());
        Assert.Equal(originalSvg, full.ToSvg()); Assert.Equal(originalPng, full.ToPng());
    }

    [Theory]
    [InlineData(false, 1, null)]
    [InlineData(false, 9, null)]
    [InlineData(true, 1, null)]
    [InlineData(true, 9, null)]
    [InlineData(false, 1, 2d)]
    [InlineData(false, 8, 9d)]
    [InlineData(true, 1, 2d)]
    [InlineData(true, 8, 9d)]
    public void OffWindowReferencesRetainSourceFactsWithoutGuideOrCaptionPaint(bool reversed, double value, double? end) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }, Blue);
        chart.Options.XAxis.Reversed = reversed; chart.Series[0].Markers.Enabled = false;
        var window = new ChartXViewport(3, 7);
        var withoutReference = chart.Prepare(Context(), window).ToPng();
        if (end.HasValue) chart.AddVerticalBand(value, end.Value, "Reference");
        else chart.AddVerticalLine(value, "Reference");
        Assert.Contains(chart.Prepare(Context()).Scene.Nodes, node => node.Role == "annotation-label");
        var prepared = chart.Prepare(Context(), window);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "annotation-line" or "annotation-band" or "annotation-label" or "annotation-label-backplate");
        Assert.Equal(withoutReference, prepared.ToPng());
        Assert.Single(prepared.Regions, region => region.Role == "annotation");
        var facts = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation");
        Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture), (string?)facts.Attribute("data-cfx-value"));
        Assert.Equal(end?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", (string?)facts.Attribute("data-cfx-end-value"));
        Assert.Equal("Reference", (string?)facts.Attribute("data-cfx-label"));
    }

    [Theory]
    [InlineData(false, 1, 5)]
    [InlineData(false, 5, 9)]
    [InlineData(true, 1, 5)]
    [InlineData(true, 5, 9)]
    public void PartiallyVisibleReferenceBandsUseTheirVisibleIntersection(bool reversed, double start, double end) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }, Blue)
            .AddVerticalBand(start, end, "Reference");
        chart.Options.XAxis.Reversed = reversed; chart.Series[0].Markers.Enabled = false;
        var prepared = chart.Prepare(Context(), new ChartXViewport(3, 7));
        var projection = prepared.XProjection!;
        var band = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-band");
        var first = projection.Project(Math.Max(3, start)); var last = projection.Project(Math.Min(7, end));
        Assert.Equal(Math.Min(first, last), band.Bounds.Left, 9);
        Assert.Equal(Math.Abs(last - first), band.Bounds.Width, 9);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "annotation-label");
        Assert.Equal(band.Bounds, Assert.Single(prepared.Regions, region => region.Role == "annotation").Bounds);
    }

    [Fact]
    public void StatefulAxisFormatterResultsAreSharedByFullLayoutVisibleTicksAndPaint() {
        var chart = Chart.Create().WithLegend(false).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) });
        var calls = new Dictionary<double, int>();
        chart.Options.XAxis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return "X " + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        };
        var prepared = chart.Prepare(Context(), new ChartXViewport(2, 6));
        Assert.NotEmpty(calls); Assert.All(calls.Values, count => Assert.Equal(1, count));
        // Full-domain layout can probe the callback outside this window, but its
        // measurement facts must not become captions of the final viewport.
        Assert.Contains(calls.Keys, value => value < 2 || value > 6);
        var projection = prepared.XProjection!;
        Assert.All(prepared.Regions.Where(region => region.Role == "axis-x-label"), region => {
            Assert.StartsWith("X ", region.Label!);
            Assert.InRange(region.Bounds.Left, projection.PlotBounds.Left, projection.PlotBounds.Right);
            var split = region.Label!.LastIndexOf(" (", StringComparison.Ordinal);
            var value = double.Parse(region.Label[(split + 2)..^1], System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(value, 2, 6);
            Assert.Equal(projection.Project(value), region.Bounds.Left, 9);
        });
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "axis-x-label");
    }

    private static Chart Bare() => Chart.Create().WithSize(420, 280).WithAxes(false).WithGrid(false).WithLegend(false);
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(420, 280)),
        VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false));
    private static string?[] AxisLabels(PreparedVisual prepared, string role) => prepared.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
    private static double NormalizedY(PreparedVisual prepared, string id) {
        var point = prepared.Regions.Single(region => region.Id == id).Bounds;
        var plot = prepared.XProjection!.PlotBounds;
        return (point.Top + point.Height / 2 - plot.Top) / plot.Height;
    }
    private static (int X, int Y)[] BluePixels(ChartForgeX.Raster.RgbaImage image) => Enumerable.Range(0, image.Width * image.Height)
        .Where(index => image.Pixels[index * 4 + 3] > 0 && image.Pixels[index * 4 + 2] > image.Pixels[index * 4] + 60
            && image.Pixels[index * 4 + 2] > image.Pixels[index * 4 + 1] + 60)
        .Select(index => (index % image.Width, index / image.Width)).ToArray();
    private static void AssertBlueInside(ChartForgeX.Raster.RgbaImage image, ChartRect plot) {
        var pixels = BluePixels(image); Assert.NotEmpty(pixels);
        var colored = Enumerable.Range(0, image.Width * image.Height).Where(index => image.Pixels[index * 4 + 3] > 0
            && (image.Pixels[index * 4 + 2] > image.Pixels[index * 4 + 1] + 60 || image.Pixels[index * 4] > image.Pixels[index * 4 + 1] + 60));
        Assert.All(colored, index => {
            var pixel = (X: index % image.Width, Y: index / image.Width);
            Assert.InRange(pixel.X, (int)Math.Floor(plot.Left), (int)Math.Ceiling(plot.Right) - 1);
            Assert.InRange(pixel.Y, (int)Math.Floor(plot.Top), (int)Math.Ceiling(plot.Bottom) - 1);
        });
    }
    private static void AssertBlueNear(ChartForgeX.Raster.RgbaImage image, double x, double y) =>
        Assert.Contains(BluePixels(image), pixel => Math.Abs(pixel.X + .5 - x) <= 2 && Math.Abs(pixel.Y + .5 - y) <= 2);
}
