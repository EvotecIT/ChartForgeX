using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects angular projection without changing source order, numeric radii or native paint.</summary>
public sealed class RadialAngularReversalTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitGuideShapeSurvivesAngularReversalWithSourceAndNativePaint(bool radar) {
        var color = ChartColor.FromHex("#2468AC");
        var angles = radar ? new[] { 1d, 2, 3, 4 } : new[] { 0d, .55, 2.7, 5.4 };
        var points = angles.Select((angle, index) => new ChartPoint(angle, 25 + index * 10)).ToArray();
        var chart = Bare().WithPolarGridShape(radar ? ChartPolarGridShape.Circle : ChartPolarGridShape.Polygon);
        chart.Options.ShowAxes = false; chart.Options.PolarGridRingCount = 1; chart.Options.XAxis.TickCount = 4;
        if (radar) chart.AddRadarLine("Signal", points); else chart.AddPolar("Signal", points);
        chart.Series[0].WithPointColor(1, color).ConfigureMarkers(markers => markers.Radius = 6);
        var normal = Prepare(chart); chart.Options.XAxis.WithReversal(); var reversed = Prepare(chart);
        var role = radar ? "radar-ring" : "polar-ring";
        var guide = Assert.Single(reversed.Scene.Nodes, node => node.Role == role);
        if (radar) Assert.IsType<VisualSceneEllipse>(guide);
        else Assert.True(Assert.IsType<VisualScenePath>(guide).Close);
        var svgGuide = Assert.Single(XDocument.Parse(reversed.ToSvg()).Descendants(),
            element => (string?)element.Attribute("data-cfx-role") == role);
        Assert.Equal(radar ? "ellipse" : "path", svgGuide.Name.LocalName);
        var spokes = reversed.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == (radar ? "radar-spoke" : "polar-spoke")).ToArray();
        var center = spokes[0].Start; var source = Center(reversed, "series-0-point-1");
        if (radar) AssertSameRay(center, spokes[1].End, source);
        else {
            Assert.True(Center(normal, "series-0-point-1").Y < center.Y && source.Y > center.Y);
            Assert.Equal(points[1].X.ToString("R", CultureInfo.InvariantCulture), Group(reversed, "series-0-point-1")["data-cfx-angle"]);
        }
        foreach (var key in new[] { "data-cfx-point", "data-cfx-category", "data-cfx-value" })
            Assert.Equal(Group(normal, "series-0-point-1")[key], Group(reversed, "series-0-point-1")[key]);
        AssertPixel(reversed, source, color);
        var dx = spokes[0].End.X - center.X; var dy = spokes[0].End.Y - center.Y;
        var radius = Math.Sqrt(dx * dx + dy * dy);
        // With four axis-aligned spokes, the square edge and circle differ away from all spokes and observations.
        var square = new ChartPoint(center.X + radius / 2, center.Y + radius / 2);
        var circle = new ChartPoint(center.X + radius / Math.Sqrt(2), center.Y + radius / Math.Sqrt(2));
        var image = RasterImageDecoder.Decode(reversed.ToPng());
        bool HasPaint(ChartPoint point) => Enumerable.Range(-2, 5).Any(x => Enumerable.Range(-2, 5)
            .Any(y => Pixel(image, new ChartPoint(point.X + x, point.Y + y))[3] > 0));
        Assert.True(HasPaint(radar ? circle : square), "The selected guide shape must paint in native PNG after angular reversal.");
        Assert.False(HasPaint(radar ? square : circle), "The other guide shape must not replace the selected native guide.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RadarFormsReverseCategorySpokesCaptionsAndPointsTogether(bool reverseSecondaryRadius) {
        var color = ChartColor.FromHex("#2468AC");
        var chart = Bare().WithXLabels("North", "East", "South", "West").WithSecondaryYAxisBounds(0, 10)
            .AddRadarArea("Observed", new[] { new ChartPoint(3, 60), new ChartPoint(1, 80), new ChartPoint(2, 50), new ChartPoint(4, 70) })
            .AddRadarLine("Target", new[] { new ChartPoint(4, reverseSecondaryRadius ? 3 : 7), new ChartPoint(1, reverseSecondaryRadius ? 2 : 8), new ChartPoint(2, 5) });
        chart.Series[0].WithPointColor(0, color).WithPointLabel(0, "South source").ConfigureMarkers(markers => markers.Radius = 6);
        chart.Series[1].UseSecondaryYAxis(); chart.Options.SecondaryYAxis.WithReversal(reverseSecondaryRadius);
        var normal = Prepare(chart);
        chart.Options.XAxis.WithReversal();
        var reversed = Prepare(chart);
        var spokes = reversed.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "radar-spoke").ToArray();
        Assert.Equal(4, spokes.Length);
        Assert.True(spokes[0].End.X < spokes[0].Start.X);
        Assert.True(spokes[3].End.Y < spokes[3].Start.Y);
        var center = spokes[0].Start;
        for (var category = 0; category < 4; category++) {
            var caption = Assert.Single(reversed.Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "radar-axis-label-" + category);
            AssertSameRay(center, spokes[category].End, TextCenter(caption));
        }
        var source = Center(reversed, "series-0-point-0");
        AssertSameRay(center, spokes[2].End, source);
        Assert.True(source.X > center.X);
        Assert.Equal(center.Y, source.Y, 8);
        Assert.Equal(Center(reversed, "series-0-point-1").X, Center(reversed, "series-1-point-1").X, 8);
        Assert.Equal(Center(reversed, "series-0-point-1").Y, Center(reversed, "series-1-point-1").Y, 8);
        foreach (var key in new[] { "data-cfx-point", "data-cfx-category", "data-cfx-value", "data-cfx-full-label" })
            Assert.Equal(Group(normal, "series-0-point-0")[key], Group(reversed, "series-0-point-0")[key]);
        var missing = Group(reversed, "series-1-missing-category-2");
        Assert.Equal("missing-category-zero", missing["data-cfx-derived"]);
        Assert.Equal("0", missing["data-cfx-source-count"]); Assert.Equal("", missing["data-cfx-source-points"]);
        Assert.Single(reversed.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Equal(2, reversed.Scene.Nodes.Count(node => node.Role == "radar-outline"));
        AssertPixel(reversed, source, color);
        AssertDetached(chart, reversed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContinuousPolarMirrorsAuthoredRadiansWhileKeepingRawAnglesAndSelectedRadii(bool reverseSecondaryRadius) {
        var color = ChartColor.FromHex("#AB2345");
        var points = new[] { new ChartPoint(0, 80), new ChartPoint(Math.PI / 2, 60), new ChartPoint(-.7, 40), new ChartPoint(Math.PI * 2 + .35, 70) };
        var chart = Bare().WithSecondaryYAxisBounds(0, 10).AddPolar("Observed", points)
            .AddPolar("Target", new[] { new ChartPoint(Math.PI / 2, reverseSecondaryRadius ? 4 : 6) });
        chart.Options.XAxis.TickCount = 8;
        chart.Options.XAxis.LabelFormatter = angle => (angle * 180 / Math.PI).ToString("0", CultureInfo.InvariantCulture) + "°";
        chart.Series[0].WithPointColor(2, color).WithPointLabel(2, "Negative-angle source").ConfigureMarkers(markers => markers.Radius = 6);
        chart.Series[1].UseSecondaryYAxis(); chart.Options.SecondaryYAxis.WithReversal(reverseSecondaryRadius);
        var normal = Prepare(chart); chart.Options.XAxis.WithReversal(); var reversed = Prepare(chart);
        var spokes = reversed.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "polar-spoke").ToArray();
        var center = spokes[0].Start;
        Assert.True(spokes[2].End.Y > center.Y);
        for (var point = 0; point < points.Length; point++) {
            var id = "series-0-point-" + point;
            var before = Center(normal, id); var after = Center(reversed, id);
            Assert.Equal(before.X, after.X, 8); Assert.Equal(center.Y * 2, before.Y + after.Y, 8);
            var facts = Group(reversed, id);
            Assert.Equal(points[point].X.ToString("R", CultureInfo.InvariantCulture), facts["data-cfx-angle"]);
            Assert.Equal(Group(normal, id)["data-cfx-category"], facts["data-cfx-category"]);
            Assert.Equal(Group(normal, id)["data-cfx-value"], facts["data-cfx-value"]);
        }
        var caption = Assert.Single(reversed.Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "polar-axis-label-2");
        AssertSameRay(center, spokes[2].End, TextCenter(caption));
        Assert.Equal(Center(reversed, "series-0-point-1").X, Center(reversed, "series-1-point-0").X, 8);
        Assert.Equal(Center(reversed, "series-0-point-1").Y, Center(reversed, "series-1-point-0").Y, 8);
        AssertPixel(reversed, Center(reversed, "series-0-point-2"), color);
        AssertDetached(chart, reversed);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MixedColumnsAndRadarReverseWholeCategoryCentersWithoutChangingRadiusOrSource(bool radarFirst, bool reverseSecondaryRadius) {
        var chart = Bare().WithSecondaryYAxisBounds(0, 10).WithXLabels("North", "East", "South", "West")
            .WithRadialGeometry(new ChartRadialGeometryOptions(15, 375, .25));
        var columns = new[] { new ChartPoint(1, 60), new ChartPoint(3, 80), new ChartPoint(4, 40) };
        var radar = new[] { new ChartPoint(2, reverseSecondaryRadius ? 3 : 7), new ChartPoint(1, reverseSecondaryRadius ? 4 : 6), new ChartPoint(4, reverseSecondaryRadius ? 6 : 4) };
        if (radarFirst) chart.AddRadarLine("Target", radar).AddRadialColumn("Observed", columns);
        else chart.AddRadialColumn("Observed", columns).AddRadarLine("Target", radar);
        var radarIndex = radarFirst ? 0 : 1;
        chart.Series[radarIndex].UseSecondaryYAxis(); chart.Options.SecondaryYAxis.WithReversal(reverseSecondaryRadius);
        chart.Options.XAxis.WithReversal();
        var prepared = Prepare(chart);
        var column = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(slice => slice.Role == "radial-column");
        var angle = column.Start + column.Sweep / 2;
        Assert.Equal(15 * Math.PI / 180 + 3.5 * Math.PI / 2, angle, 9);
        Assert.Equal(angle, Number(Group(prepared, "series-" + radarIndex + "-point-1")["data-cfx-angle"]), 9);
        var point = Center(prepared, "series-" + radarIndex + "-point-1");
        Assert.Equal(column.Cx + Math.Cos(angle) * column.Outer, point.X, 8);
        Assert.Equal(column.Cy + Math.Sin(angle) * column.Outer, point.Y, 8);
        Assert.Equal("1", Group(prepared, "series-" + radarIndex + "-point-1")["data-cfx-category"]);
        Assert.Equal("missing-category-zero", Group(prepared, "series-" + radarIndex + "-missing-category-2")["data-cfx-derived"]);
        Assert.Equal(0, Pixel(RasterImageDecoder.Decode(prepared.ToPng()), new ChartPoint(column.Cx, column.Cy))[3]);
    }

    [Fact]
    public void PolarAreaReversesSlotsPatternsAndCaptionsWhileRetainingSquareRootRadiiAndZeroIdentity() {
        var color = ChartColor.FromHex("#2468AC");
        var chart = Bare().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside).WithXLabels("North", "East", "South", "West")
            .AddPolarArea("Revenue", new[] { new ChartPoint(4, 100), new ChartPoint(1, 25), new ChartPoint(3, 0), new ChartPoint(2, 64) });
        chart.Series[0].WithPointColor(0, color).WithPointLabel(1, "Small source").WithPointFillPattern(3, ChartFillPattern.Crosshatch);
        var normal = Prepare(chart); chart.Options.XAxis.WithReversal(); var reversed = Prepare(chart);
        var before = normal.Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        var after = reversed.Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(4, after.Length);
        for (var point = 0; point < after.Length; point++) {
            Assert.Equal(before[3 - point].Start, after[point].Start);
            Assert.Equal(before[point].Sweep, after[point].Sweep); Assert.Equal(before[point].Outer, after[point].Outer);
            Assert.Equal(before[point].Inner, after[point].Inner); Assert.Equal(before[point].Fill, after[point].Fill);
            var id = "series-0-point-" + point;
            foreach (var key in new[] { "data-cfx-point", "data-cfx-label", "data-cfx-full-label", "data-cfx-value", "data-cfx-percent" })
                Assert.Equal(Group(normal, id)[key], Group(reversed, id)[key]);
        }
        Assert.Equal(after[0].Outer / 2, after[1].Outer, 8);
        Assert.Equal(after[0].Outer * .8, after[3].Outer, 8);
        Assert.Equal("polar-area-zero-slot", after[2].Role);
        Assert.Contains(reversed.Scene.Nodes, node => node.Role == "polar-area-pattern");
        var patternClip = Assert.Single(reversed.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.PathClip != null).PathClip!;
        Assert.All(patternClip.Commands, command => {
            Assert.True(command.X >= after[3].Cx - 1e-8); Assert.True(command.Y <= after[3].Cy + 1e-8);
        });
        var small = after[1]; var caption = Assert.Single(reversed.Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "series-0-point-1-label");
        AssertSameRay(new ChartPoint(small.Cx, small.Cy), On(small, small.Outer), TextCenter(caption));
        AssertPixel(reversed, On(after[0], after[0].Outer * .35), color);
        Assert.True(Pixel(RasterImageDecoder.Decode(reversed.ToPng()), On(after[2], (after[2].Inner + after[2].Outer) / 2))[3] > 0);
        AssertDetached(chart, reversed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PolarAreaAngularOrderDoesNotRequireRadarCategoryCount(int count) {
        var chart = Bare().AddPolarArea("Revenue", Enumerable.Range(1, count).Select(index => new ChartPoint(index, index * 25)))
            .ConfigureXAxis(axis => axis.WithReversal());
        var slices = Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(count, slices.Length);
        Assert.Equal(-Math.PI / 2 + (count - 1) * Math.PI * 2 / count, slices[0].Start, 9);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PolarAreaRadiusReversalStillRejectsIndependentlyOfAngularOrder(bool reverseAngular) {
        var chart = Bare().AddPolarArea("Revenue", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        chart.Options.XAxis.WithReversal(reverseAngular); chart.Options.YAxis.WithReversal();
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
    }

    private static Chart Bare() => Chart.Create().WithSize(600, 480).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 100);
    private static PreparedVisual Prepare(Chart chart) {
        var context = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, transparentBackground: true), context.Font));
    }
    private static IReadOnlyDictionary<string, string> Group(PreparedVisual prepared, string id) =>
        prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == id).Metadata;
    private static ChartPoint Center(PreparedVisual prepared, string id) {
        var bounds = prepared.Regions.Single(region => region.Id == id).Bounds;
        return new ChartPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }
    private static ChartPoint TextCenter(VisualSceneText text) =>
        new(text.X + text.Text.Metrics.Width / 2, text.Baseline - text.Text.Ascent + text.Text.Metrics.Height / 2);
    private static ChartPoint On(VisualSceneSlice slice, double radius) =>
        new(slice.Cx + Math.Cos(slice.Start + slice.Sweep / 2) * radius, slice.Cy + Math.Sin(slice.Start + slice.Sweep / 2) * radius);
    private static void AssertSameRay(ChartPoint center, ChartPoint first, ChartPoint second) {
        var dx = first.X - center.X; var dy = first.Y - center.Y;
        Assert.True(dx * (second.X - center.X) + dy * (second.Y - center.Y) > 0);
        // Caption ink is aligned to its outward anchor, so allow its half-height/width offset from the spoke.
        Assert.InRange(Math.Abs(dx * (second.Y - center.Y) - dy * (second.X - center.X)) / Math.Sqrt(dx * dx + dy * dy), 0, 16);
    }
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static byte[] Pixel(RgbaImage image, ChartPoint point) =>
        image.Pixels.Skip(((int)Math.Round(point.Y) * image.Width + (int)Math.Round(point.X)) * 4).Take(4).ToArray();
    private static void AssertPixel(PreparedVisual prepared, ChartPoint point, ChartColor color) =>
        Assert.Equal(new[] { color.R, color.G, color.B, color.A }, Pixel(RasterImageDecoder.Decode(prepared.ToPng()), point));
    private static void AssertDetached(Chart chart, PreparedVisual prepared) {
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.XAxis.WithReversal(false); chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }
}
