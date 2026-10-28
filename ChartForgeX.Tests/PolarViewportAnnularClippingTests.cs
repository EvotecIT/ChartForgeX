using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Radius windows crop original typed curves and areas instead of bending their vertices onto a requested boundary.</summary>
public sealed class PolarViewportAnnularClippingTests {
    [Theory]
    [InlineData(ChartLineAreaForm.Line, 0)] [InlineData(ChartLineAreaForm.Line, .6)]
    [InlineData(ChartLineAreaForm.Area, 0)] [InlineData(ChartLineAreaForm.Area, .6)]
    public void WholeOriginalCurveCommandsScaleUniformlyWithoutClampedVerticesOrInventedRunJoins(ChartLineAreaForm form, double tension) {
        var points = new[] { new ChartPoint(0, 5), new ChartPoint(1, 50), new ChartPoint(2, 95), new ChartPoint(3, 50, true), new ChartPoint(4, 5) };
        var chart = PolarViewportContractTests.Create(points); var series = chart.Series[0];
        series.ConfigurePolar(options => { options.Form = form; options.CurveTension = tension; });
        var full = PolarViewportContractTests.Prepare(chart); var prepared = PolarViewportContractTests.Prepare(chart,
            new ChartPolarViewport(radius: new ChartAxisWindow(20, 80)));
        var projection = prepared.PolarProjection!; Assert.Equal(full.PolarProjection!.Center, projection.Center);
        Assert.Equal(full.PolarProjection.OuterRadius, projection.OuterRadius);
        var fullPaths = full.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "polar-line").ToArray();
        var actual = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "polar-line").ToArray();
        Assert.Equal(fullPaths.Length, actual.Length);
        for (var index = 0; index < actual.Length; index++) {
            var expected = new ChartPath(fullPaths[index].Commands).UniformScale(projection.Center, 1.25);
            Assert.Equal(expected.Commands, actual[index].Commands); Assert.Equal(fullPaths[index].Close, actual[index].Close);
        }
        Assert.Equal(projection.OuterRadius * .25, projection.GetInnerRadius(), 9);
        var below = projection.Project(0, 5); Assert.True(below.X > projection.Center.X);
        Assert.InRange(below.X - projection.Center.X, 0, projection.GetInnerRadius());
        Assert.True(Distance(projection.Project(2, 95), projection.Center) > projection.GetOuterRadius());
        var sources = PolarViewportContractTests.Sources(prepared);
        Assert.Equal("false", sources[0].Metadata["data-cfx-visible"]); Assert.Equal("false", sources[2].Metadata["data-cfx-visible"]);
        Assert.Equal("false", sources[4].Metadata["data-cfx-visible"]); Assert.Equal("true", sources[3].Metadata["data-cfx-break-before"]);
        Assert.Equal(points, series.Points); Assert.Equal(5, prepared.Regions.Count(region => region.Role == "polar-point"));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.PathClip != null);
        PolarViewportContractTests.Capture("polar-annular-original-" + form + "-" + tension.ToString(System.Globalization.CultureInfo.InvariantCulture), prepared.ToSvg(), prepared.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ALineCrossingBothBoundariesRemainsVisibleWhenNeitherSourceObservationIsInside(bool reversed) {
        var chart = Bare(new[] { new ChartPoint(0, 0), new ChartPoint(0, 100) }); chart.Options.YAxis.Reversed = reversed;
        var prepared = PolarViewportContractTests.Prepare(chart, new ChartPolarViewport(radius: new ChartAxisWindow(40, 80)));
        Assert.All(PolarViewportContractTests.Sources(prepared), group => Assert.Equal("false", group.Metadata["data-cfx-visible"]));
        var projection = prepared.PolarProjection!; var pixels = prepared.ToRgba();
        Assert.Equal(0, Alpha(pixels, projection.Center));
        Assert.True(AlphaNear(pixels, projection.Project(0, 60)) > 0);
        Assert.Equal(0, Alpha(pixels, new ChartPoint(projection.Center.X - projection.OuterRadius * .7, projection.Center.Y)));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "polar-point"));
        var svg = prepared.ToSvg(); var strict = SvgRasterizer.Rasterize(svg, strict: true);
        Assert.Empty(strict.Diagnostics); Assert.Equal(0, Alpha(strict.Image, projection.Center));
        Assert.True(AlphaNear(strict.Image, projection.Project(0, 60)) > 0);
        PolarViewportContractTests.Capture("polar-annular-crossing-" + reversed, svg, prepared.ToPng());
    }

    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void FilledOriginalAreaUsesTheSharedAnnularStencilWithoutInventingBoundaryPolygons(int width, bool dark) {
        var points = Enumerable.Range(0, 4).Select(index => new ChartPoint(Math.PI / 4 + index * Math.PI / 2, 100)).ToArray();
        var chart = Bare(points).WithSize(width, 380).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Series[0].ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.FillOpacity = 1; options.OutlineStroke = new ChartStrokeStyle(width: 0); });
        var full = PolarViewportContractTests.Prepare(chart);
        var prepared = PolarViewportContractTests.Prepare(chart, new ChartPolarViewport(radius: new ChartAxisWindow(40, 80)));
        var projection = prepared.PolarProjection!; var actual = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-area");
        var original = Assert.Single(full.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-area");
        Assert.Equal(new ChartPath(original.Commands).UniformScale(projection.Center, 1.25).Commands, actual.Commands);
        Assert.All(PolarViewportContractTests.Sources(prepared), source => Assert.Equal("false", source.Metadata["data-cfx-visible"]));
        var pixels = prepared.ToRgba(); var insideHole = new ChartPoint(projection.Center.X + projection.OuterRadius * .25, projection.Center.Y);
        var band = new ChartPoint(projection.Center.X + projection.OuterRadius * .7, projection.Center.Y);
        var outside = new ChartPoint(projection.Center.X + projection.OuterRadius * 1.1, projection.Center.Y);
        Assert.Equal(0, Alpha(pixels, projection.Center)); Assert.Equal(0, Alpha(pixels, insideHole)); Assert.True(Alpha(pixels, band) > 0); Assert.Equal(0, Alpha(pixels, outside));
        var svg = prepared.ToSvg(); var strict = SvgRasterizer.Rasterize(svg, strict: true); Assert.Empty(strict.Diagnostics);
        Assert.Equal(0, Alpha(strict.Image, projection.Center)); Assert.Equal(0, Alpha(strict.Image, insideHole)); Assert.True(Alpha(strict.Image, band) > 0);
        var xml = XDocument.Parse(svg); Assert.Contains(xml.Descendants(), node => node.Name.LocalName == "path" && (string?)node.Attribute("clip-rule") == "evenodd");
        PolarViewportContractTests.Capture("polar-annular-area-" + width + "-" + (dark ? "dark" : "light"), svg, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SmoothTangentToTheInnerApertureRetainsItsExactCubicControls(bool reversed) {
        var xy = new[] { (-100d, 40d), (-50d, 40d), (50d, 40d), (100d, 40d) };
        var points = xy.Select(point => new ChartPoint(-Math.Atan2(point.Item2, point.Item1), Math.Sqrt(point.Item1 * point.Item1 + point.Item2 * point.Item2))).ToArray();
        var chart = Bare(points).WithYAxisBounds(0, 200); chart.Options.XAxis.Reversed = reversed;
        chart.Series[0].ConfigurePolar(options => options.CurveTension = .25);
        var full = PolarViewportContractTests.Prepare(chart); var prepared = PolarViewportContractTests.Prepare(chart,
            new ChartPolarViewport(radius: new ChartAxisWindow(40, 100)));
        var projection = prepared.PolarProjection!; var source = Assert.Single(full.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line");
        var actual = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line");
        Assert.Equal(new ChartPath(source.Commands).UniformScale(projection.Center, 2).Commands, actual.Commands);
        var tangentY = projection.Center.Y + (reversed ? -1 : 1) * projection.GetInnerRadius();
        foreach (var command in actual.Commands) {
            Assert.Equal(tangentY, command.Y, 8);
            if (command.Kind == ChartPathCommandKind.CubicTo) { Assert.Equal(tangentY, command.Control1Y, 8); Assert.Equal(tangentY, command.Control2Y, 8); }
        }
        var pixels = prepared.ToRgba(); Assert.Equal(0, Alpha(pixels, projection.Center));
        Assert.True(AlphaNear(pixels, new ChartPoint(projection.Center.X, tangentY)) > 0);
        PolarViewportContractTests.Capture("polar-annular-tangent-" + reversed, prepared.ToSvg(), prepared.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RadiusTicksAndGuidesUseTheSameCapturedApertureAndPreserveTheReversedSourceDirection(bool reversed) {
        var chart = Bare(new[] { new ChartPoint(0, 10), new ChartPoint(1, 50), new ChartPoint(2, 90) }).WithAxes().WithGrid();
        chart.Options.XAxis.Visible = false; chart.Options.YAxis.Reversed = reversed;
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(20, "Inner")); chart.Options.YAxis.Labels.Add(new ChartAxisLabel(50, "Middle")); chart.Options.YAxis.Labels.Add(new ChartAxisLabel(80, "Outer"));
        var prepared = PolarViewportContractTests.Prepare(chart, new ChartPolarViewport(radius: new ChartAxisWindow(20, 80)));
        var projection = prepared.PolarProjection!;
        var rings = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "polar-ring").ToArray();
        Assert.True(rings.Length >= 3);
        foreach (var value in new[] { 20d, 50d, 80d }) {
            var expected = Distance(projection.Project(0, value), projection.Center);
            Assert.Contains(rings, ring => Math.Round(ring.Rx, 8) == Math.Round(expected, 8));
        }
        var fractions = rings.Select(ring => Math.Round(ring.Rx / projection.OuterRadius, 8)).ToArray();
        Assert.Contains(.25, fractions); Assert.Contains(.625, fractions); Assert.Contains(1, fractions);
        Assert.All(fractions, radius => Assert.InRange(radius, .25, 1));
        Assert.Contains(prepared.Regions, region => region.Role == "polar-radius-label" && region.Label == "Middle");
        Assert.Equal(0, Alpha(prepared.ToRgba(), projection.Center));
        PolarViewportContractTests.Capture("polar-annular-guides-" + reversed, prepared.ToSvg(), prepared.ToPng());
    }

    private static Chart Bare(ChartPoint[] points) {
        var chart = PolarViewportContractTests.Create(points); chart.Options.TransparentBackground = true;
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Series[0].Markers.Enabled = false; return chart;
    }
    private static double Distance(ChartPoint first, ChartPoint second) => Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));
    private static byte Alpha(RgbaImage image, ChartPoint point) => image.Pixels[((int)Math.Round(point.Y) * image.Width + (int)Math.Round(point.X)) * 4 + 3];
    private static byte AlphaNear(RgbaImage image, ChartPoint point) {
        byte maximum = 0; var x = (int)Math.Round(point.X); var y = (int)Math.Round(point.Y);
        for (var yy = Math.Max(0, y - 2); yy <= Math.Min(image.Height - 1, y + 2); yy++) for (var xx = Math.Max(0, x - 2); xx <= Math.Min(image.Width - 1, x + 2); xx++)
            maximum = Math.Max(maximum, image.Pixels[(yy * image.Width + xx) * 4 + 3]);
        return maximum;
    }
}
