using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Partial shared radial geometry retains category/value ownership and clips paint without filtering source facts.</summary>
public sealed class MixedRadialPartialSweepTests {
    [Theory]
    [InlineData(180, 360, false)] [InlineData(180, 360, true)] [InlineData(180, 800, false)] [InlineData(180, 800, true)]
    [InlineData(270, 360, false)] [InlineData(270, 360, true)] [InlineData(270, 800, false)] [InlineData(270, 800, true)]
    public void PartialColumnsAndCurvedRadarShareCategoryCentersAndRetainEverySource(int sweep, int width, bool dark) {
        var chart = Create(sweep, width, dark); var prepared = Prepare(chart);
        var columns = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role == "radial-column").ToArray();
        Assert.Equal(6, columns.Length);
        var facts = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radar-point-source").ToArray();
        Assert.Equal(6, facts.Length);
        for (var index = 0; index < columns.Length; index++) {
            var angle = columns[index].Start + columns[index].Sweep / 2;
            var region = prepared.Regions.Single(item => item.Id == "series-1-point-" + index).Bounds;
            Assert.Equal(columns[index].Cx + Math.Cos(angle) * columns[index].Outer, region.Left + region.Width / 2, 7);
            Assert.Equal(columns[index].Cy + Math.Sin(angle) * columns[index].Outer, region.Top + region.Height / 2, 7);
            Assert.Equal("reference-" + index, facts[index].Metadata["data-cfx-observation-id"]);
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), facts[index].Metadata["data-cfx-point"]);
        }
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.PathClip != null);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width); Assert.Equal(420, image.Height);
        Assert.Equal(0, Pixel(image, columns[0].Cx, columns[0].Cy)[3]);
        var outside = -Math.PI / 2 + sweep * Math.PI / 180 + (360 - sweep) * Math.PI / 360;
        Assert.Equal(0, Pixel(image, columns[0].Cx + Math.Cos(outside) * columns[0].Outer * .3,
            columns[0].Cy + Math.Sin(outside) * columns[0].Outer * .3)[3]);
        Assert.Equal(6, XDocument.Parse(svg).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "radar-point-source"));
        Capture("mixed-partial-" + sweep + "-" + width + "-" + (dark ? "dark" : "light"), svg, png);
        chart.Series[1].Points.Clear(); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void GapRunsDoNotJoinAcrossPartialPhysicalEndsWhileFullTurnKeepsItsCyclicSeam() {
        var chart = Create(360, 600, false); var radar = chart.Series[1];
        radar.Points.Clear(); foreach (var category in new[] { 1d, 2d, 5d, 6d }) radar.Points.Add(new ChartPoint(category, 100));
        radar.ConfigureRadar(options => options.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap);
        var full = Prepare(chart); Assert.Single(full.Scene.Nodes, node => node.Role == "radar-outline");
        chart.WithRadialGeometry(new ChartRadialGeometryOptions(-90, 180, innerRadiusRatio: .2));
        var partial = Prepare(chart); var outlines = partial.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-outline").ToArray();
        Assert.Equal(2, outlines.Length); Assert.All(outlines, outline => Assert.False(outline.Close));
        Assert.DoesNotContain(partial.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Equal(4, partial.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "radar-point-source"));
        Assert.DoesNotContain(partial.Regions, region => region.Id.Contains("missing-category", StringComparison.Ordinal));
    }

    [Fact]
    public void PartialCategoryReversalChangesPositionsWithoutReorderingOrRenumberingObservations() {
        var chart = Create(180, 600, false); var before = Prepare(chart);
        chart.Options.XAxis.Reversed = true; var reversed = Prepare(chart);
        var old = before.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        var moved = reversed.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        Assert.NotEqual(old, moved);
        Assert.Equal(Enumerable.Range(0, 6).Select(index => "reference-" + index), reversed.Scene.Nodes.OfType<VisualSceneGroup>()
            .Where(group => group.Role == "radar-point-source").Select(group => group.Metadata["data-cfx-observation-id"]));
        Assert.Equal(Enumerable.Range(1, 6).Select(index => (double)index), chart.Series[1].Points.Select(point => point.X));
    }

    internal static Chart Create(int sweep, int width, bool dark) {
        var categories = Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 1000)).ToArray();
        var reference = new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.Radar,
            categories.Select(point => new ChartPoint(point.X, 100))).UseSecondaryValueAxis()
            .ConfigureRadar(options => { options.Form = ChartLineAreaForm.Area; options.CurveTension = .2; options.FillOpacity = .4; });
        for (var index = 0; index < categories.Length; index++) reference.WithObservationId(index, new ChartObservationId("reference-" + index));
        var chart = Chart.Create().WithSize(width, 420).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithYAxisBounds(0, 1000).WithSecondaryYAxisBounds(0, 100).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithRadialGeometry(new ChartRadialGeometryOptions(-90, -90 + sweep, innerRadiusRatio: .2))
            .AddRadialColumn("Observed", categories).AddSeries(reference);
        chart.Options.TransparentBackground = true; chart.Options.ShowPlotBackground = false; chart.Options.ShowCard = false; return chart;
    }
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static byte[] Pixel(RgbaImage image, double x, double y) => image.Pixels.Skip(((int)y * image.Width + (int)x) * 4).Take(4).ToArray();
    private static void Capture(string name, string svg, byte[] png) {
        var path = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(path, name + ".png"), png);
    }
}
