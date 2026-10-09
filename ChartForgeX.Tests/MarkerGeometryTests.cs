using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored marker geometry, shared legend ink and native export behavior.</summary>
public sealed class MarkerGeometryTests {
    private static readonly ChartColor Blue = ChartColor.FromHex("#2468AC");
    private static readonly ChartColor Ink = ChartColor.FromHex("#182638");

    [Fact]
    public void RadiusAliasesShareOneOwnerAndInvalidMarkerOptionsFailAtTheirPublicBoundary() {
        var series = Chart.Create().AddScatter("Samples", new[] { new ChartPoint(1, 2) }).Series[0];
        Assert.Equal(ChartMarkerShape.Circle, series.Markers.Shape);
        Assert.Null(series.Markers.Enabled); Assert.Null(series.Markers.Radius);
        series.WithMarkerRadius(12);
        Assert.Equal(12, series.Markers.Radius);
        series.Markers.Radius = 7;
        Assert.Equal(7, series.MarkerRadius);
        series.UseThemeMarkerRadius();
        Assert.Null(series.Markers.Radius);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.Shape = (ChartMarkerShape)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.Radius = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.Radius = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.StrokeWidth = double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.StrokeWidth = -1);
        Assert.Throws<ArgumentNullException>(() => series.WithMarkers(null!));
    }

    [Fact]
    public void UnsupportedMarkerOptionsFailInsteadOfBeingSilentlyIgnoredAndExistingMapRadiusRemainsSupported() {
        var bar = Bare().AddBar("Samples", new[] { new ChartPoint(1, 2) });
        bar.Series[0].Markers.Enabled = false;
        Assert.Throws<InvalidOperationException>(() => Prepare(bar));
        var map = Bare().AddDottedMap("Places", new[] { new ChartMapPoint("Origin", 0, 0) });
        map.Series[0].WithMarkerRadius(9);
        Assert.NotEmpty(Prepare(map).ToPng());
        map.Series[0].Markers.Shape = ChartMarkerShape.Diamond;
        Assert.Throws<InvalidOperationException>(() => Prepare(map));
    }

    [Theory]
    [InlineData(ChartMarkerShape.Circle, 0, 0, .85, .85)]
    [InlineData(ChartMarkerShape.Square, .7, .7, 1.3, 0)]
    [InlineData(ChartMarkerShape.Diamond, 0, 0, .7, .7)]
    [InlineData(ChartMarkerShape.Triangle, 0, .6, .7, -.6)]
    [InlineData(ChartMarkerShape.Plus, .7, 0, .7, .7)]
    [InlineData(ChartMarkerShape.Cross, .6, .6, .85, 0)]
    [InlineData(ChartMarkerShape.Heart, 0, .4, 0, -.5)]
    [InlineData(ChartMarkerShape.Pin, 0, .55, 0, -.25)]
    [InlineData(ChartMarkerShape.Star, 0, 0, .8, 0)]
    public void BuiltInShapesHaveDistinctNativeSilhouettesAndMatchingFixedSizeLegendContours(ChartMarkerShape shape,
        double inkX, double inkY, double emptyX, double emptyY) {
        const double radius = 20;
        var chart = Bare().WithLegend().AddScatter("Sample", new[] { new ChartPoint(1, 1) }, Blue);
        chart.Series[0].WithMarkers(marker => { marker.Shape = shape; marker.Radius = radius; marker.StrokeWidth = 0; });
        var prepared = Prepare(chart);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), node => node.Role == "marker");
        var legend = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), node => node.Role == "legend-swatch");
        var region = Assert.Single(prepared.Regions, item => item.Id == "series-0-point-0");
        var centre = new ChartPoint(region.Bounds.Left + region.Bounds.Width / 2, region.Bounds.Top + region.Bounds.Height / 2);
        Assert.Equal(Blue, mark.Fill); Assert.Equal(Blue, legend.Fill);
        var swatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Clip is { Width: 10, Height: 10 }).Clip!.Value;
        var legendCentre = new ChartPoint(swatch.Left + swatch.Width / 2, swatch.Top + swatch.Height / 2);
        if (shape == ChartMarkerShape.Circle) {
            Assert.Equal(radius, Assert.IsType<VisualSceneEllipse>(mark).Rx);
            Assert.Equal(4, Assert.IsType<VisualSceneEllipse>(legend).Rx);
        } else {
            var sourcePath = Assert.IsType<VisualScenePath>(mark); var legendPath = Assert.IsType<VisualScenePath>(legend);
            Assert.Equal(sourcePath.Commands.Count, legendPath.Commands.Count);
            for (var index = 0; index < sourcePath.Commands.Count; index++)
                SameNormalizedCommand(sourcePath.Commands[index], centre, radius, legendPath.Commands[index], legendCentre, 4);
        }
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(new[] { Blue.R, Blue.G, Blue.B, Blue.A }, Pixel(image, centre.X + inkX * radius, centre.Y + inkY * radius));
        Assert.Equal(0, Pixel(image, centre.X + emptyX * radius, centre.Y + emptyY * radius)[3]);
        var svg = XDocument.Parse(prepared.ToSvg());
        var expectedElement = shape == ChartMarkerShape.Circle ? "ellipse" : "path";
        Assert.Equal(expectedElement, Assert.Single(Roles(svg, "marker")).Name.LocalName);
        Assert.Equal(expectedElement, Assert.Single(Roles(svg, "legend-swatch")).Name.LocalName);
        if (shape == ChartMarkerShape.Pin) Assert.Equal("evenodd", (string?)Assert.Single(Roles(svg, "marker")).Attribute("fill-rule"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line, "marker")]
    [InlineData(ChartSeriesKind.StepLine, "marker")]
    [InlineData(ChartSeriesKind.Area, "marker")]
    [InlineData(ChartSeriesKind.StepArea, "marker")]
    [InlineData(ChartSeriesKind.StackedArea, "marker")]
    [InlineData(ChartSeriesKind.Scatter, "marker")]
    [InlineData(ChartSeriesKind.Bubble, "bubble")]
    [InlineData(ChartSeriesKind.ErrorBar, "error-marker")]
    [InlineData(ChartSeriesKind.Dumbbell, "dumbbell-end")]
    [InlineData(ChartSeriesKind.Lollipop, "lollipop-marker")]
    [InlineData(ChartSeriesKind.Slope, "slope-marker")]
    [InlineData(ChartSeriesKind.RangeBand, "range-marker")]
    [InlineData(ChartSeriesKind.RangeArea, "range-marker")]
    [InlineData(ChartSeriesKind.Radar, "radar-point")]
    [InlineData(ChartSeriesKind.Polar, "polar-point")]
    public void EverySupportedProducerUsesAuthoredGeometryAndIndependentMarkerPaint(ChartSeriesKind kind, string role) {
        var chart = Family(kind);
        chart.Series[0].WithMarkers(marker => {
            marker.Shape = ChartMarkerShape.Diamond; marker.Radius = 7; marker.Enabled = true;
            marker.Fill = Blue; marker.Stroke = Ink; marker.StrokeWidth = 2;
        });
        var prepared = Prepare(chart);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == role).ToArray();
        Assert.NotEmpty(marks);
        Assert.All(marks, mark => {
            Assert.IsType<VisualScenePath>(mark); Assert.Equal(Blue, mark.Fill);
            Assert.Equal(Ink, mark.Stroke); Assert.Equal(2, mark.StrokeWidth);
        });
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-point-0");
        Assert.All(Roles(XDocument.Parse(prepared.ToSvg()), role), element => Assert.Equal("path", element.Name.LocalName));
        Assert.Contains(RasterImageDecoder.Decode(prepared.ToPng()).Pixels.Where((_, index) => index % 4 == 3), value => value > 0);
    }

    [Theory]
    [InlineData(ChartSeriesKind.ErrorBar)]
    [InlineData(ChartSeriesKind.Dumbbell)]
    [InlineData(ChartSeriesKind.Lollipop)]
    public void ConnectingMarkLegendsRetainTheLineHintAlongsideTheConfiguredGlyph(ChartSeriesKind kind) {
        var chart = Family(kind).WithLegend();
        chart.Series[0].WithMarkers(marker => { marker.Shape = ChartMarkerShape.Square; marker.Fill = Blue; marker.Stroke = Ink; marker.StrokeWidth = 2; });
        var prepared = Prepare(chart);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "legend-line");
        var glyph = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "legend-swatch");
        Assert.Equal(Blue, glyph.Fill); Assert.Equal(Ink, glyph.Stroke); Assert.Equal(2, glyph.StrokeWidth);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("line", Assert.Single(Roles(svg, "legend-line")).Name.LocalName);
        Assert.Equal("path", Assert.Single(Roles(svg, "legend-swatch")).Name.LocalName);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void PointColorPatternAndDecimatedSourceIdsSurviveShapeVisibilityChangesAndPreparation() {
        var chart = Bare().AddDecimatedLine("Signal", Enumerable.Range(0, 20).Select(index => new ChartPoint(index, index % 5)), 6, color: Blue);
        var series = chart.Series[0];
        series.WithMarkers(marker => { marker.Shape = ChartMarkerShape.Square; marker.Enabled = true; marker.Fill = Ink; marker.Radius = 8; });
        series.WithPointColor(0, Blue).WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        var prepared = Prepare(chart);
        var sourceGroups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(series.SourcePointIndices.Select(index => index.ToString(CultureInfo.InvariantCulture)), sourceGroups.Select(group => group.Metadata["data-cfx-source-point"]));
        Assert.Equal(Blue, prepared.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "marker").Fill);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "marker-pattern");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.Markers.Enabled = false;
        var hidden = Prepare(chart);
        Assert.DoesNotContain(hidden.Scene.Nodes, node => node.Role is "marker" or "marker-pattern");
        Assert.Equal(sourceGroups.Select(group => (group.Id, group.Metadata["data-cfx-source-point"])),
            hidden.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").Select(group => (group.Id, group.Metadata["data-cfx-source-point"])));
        series.Points.Clear(); series.Markers.Shape = ChartMarkerShape.Pin;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void BubbleSourceSizeControlsTheSameRadiusScaleForDifferentShapes() {
        var samples = new[] { new ChartBubble(1, 20, 2), new ChartBubble(3, 30, 10), new ChartBubble(5, 40, 40) };
        var chart = Bare().AddBubble("Circles", samples, Blue).AddBubble("Diamonds", samples, Ink);
        chart.Series[1].Markers.Shape = ChartMarkerShape.Diamond;
        var prepared = Prepare(chart);
        var circles = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble").ToArray();
        var diamonds = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "bubble").ToArray();
        Assert.Equal(3, circles.Length); Assert.Equal(3, diamonds.Length);
        for (var index = 0; index < circles.Length; index++) {
            Assert.Equal(circles[index].Rx, (diamonds[index].Commands.Max(command => command.X) - diamonds[index].Commands.Min(command => command.X)) / 2, 7);
            var source = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == "series-1-point-" + index);
            Assert.Equal(samples[index].Size.ToString("R", CultureInfo.InvariantCulture), source.Metadata["data-cfx-size"]);
        }
        Assert.True(circles[0].Rx < circles[1].Rx && circles[1].Rx < circles[2].Rx);
        var themeRadius = VisualExportRequest.ForChart(chart).Context.Theme.MarkerRadius;
        chart.Series[0].Markers.Radius = themeRadius * 1.5;
        var scaled = Prepare(chart).Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble").ToArray();
        for (var index = 0; index < circles.Length; index++) Assert.Equal(circles[index].Rx * 1.5, scaled[index].Rx, 7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void BubbleMarkerWidthOverridesTheFamilyOutlineInSourceLegendAndBounds(double markerWidth) {
        var chart = Bare().WithLegend().AddBubble("Samples", new[] { new ChartBubble(1, 20, 2) }, Blue);
        chart.Series[0].WithStrokeWidth(5).WithMarkers(marker => { marker.Stroke = Ink; marker.StrokeWidth = markerWidth; });
        var prepared = Prepare(chart);
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "bubble");
        var legend = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "legend-swatch");
        Assert.Equal(markerWidth, source.StrokeWidth); Assert.Equal(markerWidth, legend.StrokeWidth);
        Assert.Equal(markerWidth == 0 ? (ChartColor?)null : Ink, source.Stroke); Assert.Equal(source.Stroke, legend.Stroke);
        Assert.Equal(source.Rx * 2 + markerWidth, Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0").Bounds.Width, 7);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal((string?)Assert.Single(Roles(svg, "bubble")).Attribute("stroke"), (string?)Assert.Single(Roles(svg, "legend-swatch")).Attribute("stroke"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, false)]
    [InlineData(ChartScaleKind.Logarithmic, true)]
    public void AutomaticBoundsContainSquareCornersAndAuthoredOutlines(ChartScaleKind scale, bool secondary) {
        var chart = Family(ChartSeriesKind.Scatter);
        chart.Options.ShowAxes = true;
        chart.Options.XAxis.Scale = scale;
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).Scale = scale;
        chart.Series[0].WithMarkers(marker => { marker.Shape = ChartMarkerShape.Square; marker.Radius = 12; marker.Stroke = Ink; marker.StrokeWidth = 8; });
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        var prepared = Prepare(chart);
        var xAxis = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "axis-x");
        var yAxis = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "axis-y");
        foreach (var marker in prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "marker")) {
            Assert.True(marker.Commands.Min(command => command.X) - 4 >= xAxis.Start.X - .000001);
            Assert.True(marker.Commands.Max(command => command.X) + 4 <= xAxis.End.X + .000001);
            Assert.True(marker.Commands.Min(command => command.Y) - 4 >= yAxis.Start.Y - .000001);
            Assert.True(marker.Commands.Max(command => command.Y) + 4 <= yAxis.End.Y + .000001);
        }
    }

    private static Chart Family(ChartSeriesKind kind) {
        var chart = Bare(); var points = new[] { new ChartPoint(1, 20), new ChartPoint(3, 30), new ChartPoint(5, 40) };
        return kind switch {
            ChartSeriesKind.Line => chart.AddLine("Samples", points),
            ChartSeriesKind.StepLine => chart.AddStepLine("Samples", points),
            ChartSeriesKind.Area => chart.AddArea("Samples", points),
            ChartSeriesKind.StepArea => chart.AddStepArea("Samples", points),
            ChartSeriesKind.StackedArea => chart.AddStackedArea("Samples", points),
            ChartSeriesKind.Scatter => chart.AddScatter("Samples", points),
            ChartSeriesKind.Bubble => chart.AddBubble("Samples", new[] { new ChartBubble(1, 20, 2), new ChartBubble(3, 30, 10) }),
            ChartSeriesKind.ErrorBar => chart.AddErrorBar("Samples", new[] { new ChartErrorBar(1, 20, 10, 30) }),
            ChartSeriesKind.Dumbbell => chart.AddDumbbell("Samples", new[] { new ChartDumbbell(1, 10, 30) }),
            ChartSeriesKind.Lollipop => chart.AddLollipop("Samples", points),
            ChartSeriesKind.Slope => chart.AddSlope("Samples", 20, 40),
            ChartSeriesKind.RangeBand => chart.AddRangeBand("Samples", new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(3, 20, 40) }),
            ChartSeriesKind.RangeArea => chart.AddRangeArea("Samples", new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(3, 20, 40) }, smooth: false),
            ChartSeriesKind.Radar => chart.AddRadar("Samples", points),
            ChartSeriesKind.Polar => chart.AddPolar("Samples", points),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(320, 220).WithHeader(false).WithLegend(false).WithDataLabels(false);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false; return chart;
    }

    private static PreparedVisual Prepare(Chart chart) {
        var request = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, transparentBackground: true), request.Font));
    }

    private static void SameNormalizedCommand(ChartPathCommand source, ChartPoint centre, double radius, ChartPathCommand legend, ChartPoint legendCentre, double legendRadius) {
        Assert.Equal(source.Kind, legend.Kind);
        Assert.Equal((source.X - centre.X) / radius, (legend.X - legendCentre.X) / legendRadius, 7);
        Assert.Equal((source.Y - centre.Y) / radius, (legend.Y - legendCentre.Y) / legendRadius, 7);
        if (source.Kind != ChartPathCommandKind.CubicTo) return;
        Assert.Equal((source.Control1X - centre.X) / radius, (legend.Control1X - legendCentre.X) / legendRadius, 7);
        Assert.Equal((source.Control1Y - centre.Y) / radius, (legend.Control1Y - legendCentre.Y) / legendRadius, 7);
        Assert.Equal((source.Control2X - centre.X) / radius, (legend.Control2X - legendCentre.X) / legendRadius, 7);
        Assert.Equal((source.Control2Y - centre.Y) / radius, (legend.Control2Y - legendCentre.Y) / legendRadius, 7);
    }

    private static byte[] Pixel(RgbaImage image, double x, double y) => image.Pixels.Skip(((int)y * image.Width + (int)x) * 4).Take(4).ToArray();
    private static XElement[] Roles(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
