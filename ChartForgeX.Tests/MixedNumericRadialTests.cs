using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observable category, domain, paint and source contracts for radial-column/radar composition.</summary>
public sealed class MixedNumericRadialTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherFirstKindRoutesToOneCategoryProjectionAndSharedNumericDomain(bool radarFirst) {
        var chart = Bare();
        var column = new[] { new ChartPoint(1, 60), new ChartPoint(2, 60), new ChartPoint(3, 60) };
        var radar = new[] { new ChartPoint(3, 60), new ChartPoint(1, 60), new ChartPoint(2, 60), new ChartPoint(4, 100) };
        if (radarFirst) chart.AddRadarLine("Target", radar).AddRadialColumn("Observed", column);
        else chart.AddRadialColumn("Observed", column).AddRadarLine("Target", radar);
        chart.Options.YAxis.Maximum = null;
        var prepared = Prepare(chart); var columnIndex = radarFirst ? 1 : 0; var radarIndex = 1 - columnIndex;
        var mark = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        var point = Point(prepared, radarIndex, 1);
        var angle = mark.Start + mark.Sweep / 2;
        Assert.Equal(angle, Number(point["data-cfx-angle"]), 10);
        var bounds = prepared.Regions.Single(region => region.Id == $"series-{radarIndex}-point-1").Bounds;
        Assert.Equal(mark.Cx + Math.Cos(angle) * mark.Outer, bounds.X + bounds.Width / 2, 8);
        Assert.Equal(mark.Cy + Math.Sin(angle) * mark.Outer, bounds.Y + bounds.Height / 2, 8);
        Assert.Equal("100", Group(prepared, $"series-{columnIndex}")["data-cfx-max-value"]);
        Assert.Equal("100", Group(prepared, $"series-{radarIndex}")["data-cfx-max-value"]);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "radar-point"));
        Assert.Equal(3, prepared.Scene.Nodes.Count(node => node.Role == "radial-column"));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "radar-outline" && path.Close);
    }

    [Fact]
    public void RadarOverlaysConsumeNeitherColumnSlotsNorNormalizedStackTotals() {
        var chart = Bare().AddRadialColumn("First", Values(20)).AddRadialColumn("Second", Values(30))
            .AddRadialColumn("Independent", Values(70));
        chart.Series[0].WithStackGroup("work").WithNormalization(100);
        chart.Series[1].WithStackGroup("work").WithNormalization(100);
        chart.Options.ShowStackTotals = true;
        var before = Prepare(chart);
        chart.AddRadarArea("Reference", Values(40)).AddRadarLine("Target", Values(90));
        var after = Prepare(chart);
        var prior = before.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "radial-column").ToArray();
        var current = after.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "radial-column").ToArray();
        for (var index = 0; index < prior.Length; index++) {
            Assert.Equal(prior[index].Start, current[index].Start); Assert.Equal(prior[index].Sweep, current[index].Sweep);
        }
        Assert.Equal("40", Point(after, 1, 0)["data-cfx-base"]);
        Assert.Equal("100", Point(after, 1, 0)["data-cfx-stack-end"]);
        Assert.Equal("30", Point(after, 1, 0)["data-cfx-value"]);
        Assert.Equal("50", Point(after, 1, 0)["data-cfx-stack-source-total"]);
        Assert.Equal("40", Point(after, 3, 0)["data-cfx-value"]);
        Assert.Equal(4, after.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "stack-total-source"));
        Assert.Single(after.Scene.Nodes, node => node.Role == "radar-area");
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, 0, 100, 75, 0, 10, 7.5, false)]
    [InlineData(ChartScaleKind.Linear, 0, 100, 75, 0, 10, 2.5, true)]
    [InlineData(ChartScaleKind.Logarithmic, 1, 100, 10, 1, 1000, 31.622776601683793, false)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, -100, 100, 0, -10, 10, 0, true)]
    public void EachOverlayUsesItsSelectedScaleAndReversal(ChartScaleKind kind, double minimum, double maximum, double primary,
        double secondaryMinimum, double secondaryMaximum, double secondary, bool reversed) {
        var chart = Bare().WithYAxisBounds(minimum, maximum).WithSecondaryYAxisBounds(secondaryMinimum, secondaryMaximum)
            .AddRadialColumn("Counts", Values(primary)).AddRadarLine("Count reference", Values(primary)).AddRadarLine("Rate", Values(secondary));
        chart.Options.YAxis.Scale = kind; chart.Options.SecondaryYAxis.WithScale(kind).WithReversal(reversed);
        chart.Series[2].UseSecondaryYAxis();
        var prepared = Prepare(chart);
        var first = prepared.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        var second = prepared.Regions.Single(region => region.Id == "series-2-point-0").Bounds;
        Assert.Equal(first.X, second.X, 8); Assert.Equal(first.Y, second.Y, 8);
        Assert.Equal("secondary", Point(prepared, 2, 0)["data-cfx-axis"]);
        Assert.Equal(secondary.ToString("G17", CultureInfo.InvariantCulture), Point(prepared, 2, 0)["data-cfx-value"]);
    }

    [Fact]
    public void SparseCategoriesRetainAuthoredZerosSourcePaintAndDetachedSnapshots() {
        var color = ChartColor.FromHex("#AB1234");
        var reference = new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.Radar,
            new[] { new ChartPoint(3, 80), new ChartPoint(1, 0), new ChartPoint(2, 50) });
        var chart = Bare().AddSeries(reference)
            .AddRadialColumn("Observed", new[] { new ChartPoint(1, 0), new ChartPoint(4, 20) });
        reference.WithPointColor(0, color).WithPointLabel(0, "Source caption");
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal("3", Point(prepared, 0, 0)["data-cfx-category"]);
        Assert.Equal("0", Point(prepared, 0, 0)["data-cfx-source-point"]);
        Assert.Equal("reference", Point(prepared, 0, 0)["data-cfx-series-key"]);
        Assert.Equal("Source caption", Point(prepared, 0, 0)["data-cfx-full-label"]);
        Assert.Equal("false", Point(prepared, 0, 1)["data-cfx-missing"]);
        var missing = Group(prepared, "series-0-missing-category-3");
        Assert.Equal("missing-category-zero", missing["data-cfx-derived"]);
        Assert.Equal("0", missing["data-cfx-source-count"]); Assert.Equal("", missing["data-cfx-source-points"]);
        Assert.Equal("zero", Point(prepared, 1, 0)["data-cfx-geometry-status"]);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-2");
        var source = prepared.Regions.Single(region => region.Id == "series-0-point-0").Bounds;
        var pixel = Pixel(RasterImageDecoder.Decode(png), source.X + source.Width / 2, source.Y + source.Height / 2);
        Assert.Equal(new byte[] { color.R, color.G, color.B, color.A }, pixel);
        chart.Series[0].Points.Clear(); chart.Options.YAxis.WithReversal();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartFillPattern.None)]
    [InlineData(ChartFillPattern.DiagonalForward)]
    public void RadarAreaPatternsAndMarkerStrokesLeaveTheAnnularHoleTransparent(ChartFillPattern pattern) {
        var chart = Bare().WithRadialGeometry(new ChartRadialGeometryOptions(innerRadiusRatio: .3))
            .AddRadialColumn("Observed", Values(20)).AddRadarArea("Reference", Values(80), ChartColor.FromHex("#2468AC"));
        chart.Series[1].Radar.FillOpacity = .4; chart.Series[1].FillPattern = pattern;
        var prepared = Prepare(chart); var mark = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(0, Pixel(image, mark.Cx, mark.Cy)[3]);
        Assert.Equal(0, Pixel(image, mark.Cx + mark.Inner * .5, mark.Cy)[3]);
        var angle = mark.Start + mark.Sweep / 2;
        Assert.True(Pixel(image, mark.Cx + Math.Cos(angle) * mark.Inner * 2, mark.Cy + Math.Sin(angle) * mark.Inner * 2)[3] > 0);
        var doc = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(doc.Descendants(), element => element.Name.LocalName == "path" && (string?)element.Attribute("clip-rule") == "evenodd");
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Equal(pattern != ChartFillPattern.None, prepared.Scene.Nodes.Any(node => node.Role == "radar-pattern"));
    }

    [Fact]
    public void MixedProjectionRejectsAmbiguousOrUnsupportedCategoryGeometry() {
        Chart Mixed() => Bare().AddRadialColumn("Observed", Values(40)).AddRadarLine("Target", Values(70));
        var chart = Mixed().WithRadialGeometry(new ChartRadialGeometryOptions(-90, 180));
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
        chart = Mixed(); chart.Series[0].Points.Add(new ChartPoint(1, 10));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart = Mixed(); chart.Series[1].Points.Add(new ChartPoint(1 + 4e-16, 10));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart = Bare().AddRadialColumn("Observed", new[] { new ChartPoint(1, 40) }).AddRadarLine("Target", new[] { new ChartPoint(2, 70) });
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        foreach (var kind in new[] { ChartSeriesKind.RadialBar, ChartSeriesKind.Polar, ChartSeriesKind.PolarArea }) {
            chart = Mixed(); chart.AddSeries(new ChartSeries(new ChartSeriesId("other"), "Other", kind, Values(20)));
            Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        }
        var empty = Bare().AddRadialColumn("Observed", Array.Empty<ChartPoint>()).AddRadarLine("Target", Array.Empty<ChartPoint>());
        Assert.Contains(Prepare(empty).Diagnostics, item => item.Code == "numeric-radial.no-data");
        Assert.NotEmpty(Prepare(Bare().AddRadialColumn("Observed", new[] { new ChartPoint(1, 20) }).AddRadarLine("Target", Array.Empty<ChartPoint>())).ToSvg());
    }

    [Fact]
    public void EmptyOverlayPreservesStandaloneColumnGeometryAndSecondaryOnlyChartsRetainGrid() {
        var chart = Bare().AddRadialColumn("Observed", Values(60));
        var before = Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        chart.AddRadarLine("Missing target", Array.Empty<ChartPoint>());
        var after = Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        Assert.Equal(before.Inner, after.Inner); Assert.Equal(before.Outer, after.Outer);
        Assert.Equal(before.Start, after.Start); Assert.Equal(before.Sweep, after.Sweep);
        chart.Series[0].UseSecondaryYAxis(); chart.Series[1].UseSecondaryYAxis();
        chart.Options.ShowGrid = true;
        Assert.Contains(Prepare(chart).Scene.Nodes, node => node.Role == "radial-value-grid");
    }

    [Fact]
    public void ColumnAndRadarCaptionsShareCollisionPlacementAndRetainOmittedDescriptions() {
        var chart = Bare().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .AddRadialColumn("Observed", Values(40)).AddRadarLine("Target", Values(80));
        chart.Series[1].DataLabelPlacement = ChartDataLabelPlacement.Above;
        var prepared = Prepare(chart);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "radial-data-label" or "radar-data-label")
            .Select(text => new ChartRect(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height)).ToArray();
        Assert.NotEmpty(captions);
        Assert.Equal(8, prepared.Regions.Count(region => region.Role is "radial-data-label" or "radar-data-label"));
        for (var index = 0; index < captions.Length; index++) for (var next = index + 1; next < captions.Length; next++)
            Assert.False(captions[index].Left < captions[next].Right && captions[index].Right > captions[next].Left
                && captions[index].Top < captions[next].Bottom && captions[index].Bottom > captions[next].Top);
        var mark = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        var hole = new LabelMarkShape(new[] { ChartPathBuilder.Ellipse(mark.Cx, mark.Cy, mark.Inner, mark.Inner).Flatten() }, true, 0);
        Assert.All(captions, caption => Assert.False(hole.Intersects(caption)));
    }

    [Theory]
    [InlineData(360, 360, VisualThemeMode.Light)]
    [InlineData(360, 360, VisualThemeMode.Dark)]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    public void GalleryModelExportsAllThreeFormsWithBoundedNativeGeometry(int width, int height, VisualThemeMode mode) {
        var chart = V2GalleryModels.CreateMixedRadial().WithSize(width, height).WithHeader(false).WithLegend();
        var request = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, mode, request.Frame, request.Font));
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "radar-outline"));
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "radial-column"));
        Assert.All(prepared.Scene.Nodes.OfType<VisualScenePath>().SelectMany(path => path.Commands), command => {
            Assert.InRange(command.X, 0, width); Assert.InRange(command.Y, 0, height);
        });
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
        Assert.NotEmpty(XDocument.Parse(prepared.ToSvg()).Descendants());
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(400, 360).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 100);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false; return chart;
    }
    private static ChartPoint[] Values(double value) => Enumerable.Range(1, 4).Select(index => new ChartPoint(index, value)).ToArray();
    private static PreparedVisual Prepare(Chart chart) {
        var request = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, transparentBackground: true), request.Font));
    }
    private static IReadOnlyDictionary<string, string> Group(PreparedVisual prepared, string id) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == id).Metadata;
    private static IReadOnlyDictionary<string, string> Point(PreparedVisual prepared, int series, int point) => Group(prepared, $"series-{series}-point-{point}");
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static byte[] Pixel(RgbaImage image, double x, double y) => image.Pixels.Skip(((int)Math.Round(y) * image.Width + (int)Math.Round(x)) * 4).Take(4).ToArray();
}
