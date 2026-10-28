using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public directional clipping retains raw observations, detached projection policy and shared export decisions.</summary>
public sealed class PolarViewportContractTests {
    [Fact]
    public void FullTurnAndResetRetainDefaultArtifactsWithoutMutatingAuthoredAxes() {
        var chart = Create(); var context = VisualExportRequest.ForChart(chart).Context;
        var full = chart.Prepare(context);
        var explicitFull = chart.PreparePolar(context, new ChartPolarViewport(angle: new ChartAxisWindow(0, Math.PI * 2)));
        var reset = chart.PreparePolar(context);
        Assert.Equal(full.ToSvg(), explicitFull.ToSvg()); Assert.Equal(full.ToPng(), explicitFull.ToPng());
        Assert.Equal(full.ToSvg(), reset.ToSvg()); Assert.Equal(full.ToPng(), reset.ToPng());
        var fullRadius = chart.PreparePolar(context, new ChartPolarViewport(radius: new ChartAxisWindow(0, 100)));
        Assert.Equal(full.ToSvg(), fullRadius.ToSvg()); Assert.Equal(full.ToPng(), fullRadius.ToPng());
        Assert.NotNull(reset.PolarProjection); Assert.Null(reset.XProjection); Assert.Null(reset.YProjection);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(100, chart.Options.YAxis.Maximum);
        Assert.Null(chart.Options.YAxis.PreparedViewportWindow); Assert.Null(chart.Options.XAxis.Minimum);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SeamCrossingIncludesEveryEquivalentBearingAndRetainsAllOriginalFacts(bool reversed) {
        var points = new[] { new ChartPoint(-.25, 40), new ChartPoint(Math.PI * 2 - .25, 40), new ChartPoint(Math.PI * 4 - .25, 40),
            new ChartPoint(.25, 55), new ChartPoint(3, 70) };
        var chart = Create(points); chart.Options.XAxis.Reversed = reversed;
        var prepared = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(5.5, 6.8)));
        var sources = Sources(prepared); Assert.Equal(points.Length, sources.Length);
        Assert.Equal(points.Select(point => point.X.ToString("R", CultureInfo.InvariantCulture)), sources.Select(group => group.Metadata["data-cfx-angle"]));
        Assert.All(sources.Take(4), group => Assert.False(group.Metadata.ContainsKey("data-cfx-visible")));
        Assert.Equal("false", sources[4].Metadata["data-cfx-visible"]);
        Assert.All(Enumerable.Range(0, points.Length), index => Assert.Equal("reading-" + index, sources[index].Metadata["data-cfx-observation-id"]));
        var projection = prepared.PolarProjection!; Assert.Equal(reversed, projection.AngleReversed);
        foreach (var point in points.Take(4)) Assert.True(projection.Contains(point.X, point.Y));
        Assert.False(projection.Contains(3, 70)); Assert.Equal(points, chart.Series[0].Points);
        for (var index = 1; index < 3; index++) {
            var first = projection.Project(points[0].X, 40); var equivalent = projection.Project(points[index].X, 40);
            Assert.Equal(first.X, equivalent.X, 9); Assert.Equal(first.Y, equivalent.Y, 9);
        }
    }

    [Fact]
    public void WrappedExclusiveEndpointAndNegativeNearZeroBearingRetainTheirDeclaredMembership() {
        var chart = Create(new[] { new ChartPoint(5.5, 50), new ChartPoint(6.8, 50), new ChartPoint(-double.Epsilon, 50), new ChartPoint(0, 50) });
        var wrapped = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(5.5, 6.8, false))).PolarProjection!;
        Assert.True(wrapped.Contains(5.5, 50)); Assert.False(wrapped.Contains(6.8, 50));
        var ordinary = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(0, 1))).PolarProjection!;
        Assert.False(ordinary.Contains(-double.Epsilon, 50)); Assert.True(ordinary.Contains(0, 50));
    }

    [Fact]
    public void ExclusiveAngularEndpointAndRadiusEndpointHideMarksWithoutRemovingFactsOrPaths() {
        var chart = Create(new[] { new ChartPoint(0, 20), new ChartPoint(1, 50), new ChartPoint(2, 80), new ChartPoint(3, 100) });
        var prepared = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(0, 2, false), radius: new ChartAxisWindow(20, 80, false)));
        Assert.Equal(4, Sources(prepared).Length); Assert.Equal(4, prepared.Regions.Count(region => region.Role == "polar-point"));
        Assert.False(Sources(prepared)[0].Metadata.ContainsKey("data-cfx-visible"));
        Assert.False(Sources(prepared)[1].Metadata.ContainsKey("data-cfx-visible"));
        Assert.All(Sources(prepared).Skip(2), group => Assert.Equal("false", group.Metadata["data-cfx-visible"]));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line");
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Count(ellipse => ellipse.Role == "polar-point"));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void PrimaryAndSecondaryRadiusWindowsUseTheirCapturedTransformAndReversal(bool primaryReversed, bool secondaryReversed) {
        var chart = Create(new[] { new ChartPoint(0, 20), new ChartPoint(1, 50), new ChartPoint(2, 80) })
            .WithSecondaryYAxisBounds(1, 1000).AddPolar("Log radius", new[] { new ChartPoint(0, 1), new ChartPoint(1, 10), new ChartPoint(2, 100), new ChartPoint(3, 1000) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.YAxis.Reversed = primaryReversed; chart.Options.SecondaryYAxis.Reversed = secondaryReversed;
        var request = new ChartPolarViewport(radius: new ChartAxisWindow(20, 80), secondaryRadius: new ChartAxisWindow(10, 100));
        var prepared = Prepare(chart, request); var projection = prepared.PolarProjection!;
        Assert.Equal(20, projection.GetRadiusWindow().Minimum); Assert.Equal(0, projection.GetRadiusWindow(fullDomain: true).Minimum);
        Assert.Equal(10, projection.GetRadiusWindow(ChartAxisSide.Secondary).Minimum); Assert.Equal(1, projection.GetRadiusWindow(ChartAxisSide.Secondary, true).Minimum);
        var middleRadius = (projection.GetInnerRadius() + projection.GetOuterRadius()) / 2;
        var logical = projection.Project(0, 50); Assert.Equal(projection.Center.X + middleRadius, logical.X, 8);
        var logMid = Math.Sqrt(10 * 100); var logPoint = projection.Project(0, logMid, ChartAxisSide.Secondary);
        var secondaryMiddle = (projection.GetInnerRadius(ChartAxisSide.Secondary) + projection.GetOuterRadius(ChartAxisSide.Secondary)) / 2;
        Assert.Equal(projection.Center.X + secondaryMiddle, logPoint.X, 8);
        Assert.Equal(logMid, projection.UnprojectRadius(secondaryMiddle, ChartAxisSide.Secondary), 8);
        Assert.Equal(.25 * projection.OuterRadius, projection.GetInnerRadius(), 8);
        Assert.Equal(.5 * projection.OuterRadius, projection.GetInnerRadius(ChartAxisSide.Secondary), 8);
        Assert.Equal(projection.Center.X + (primaryReversed ? projection.GetOuterRadius() : projection.GetInnerRadius()), projection.Project(0, 20).X, 8);
        Assert.Equal(projection.Center.X + (secondaryReversed ? projection.GetOuterRadius(ChartAxisSide.Secondary) : projection.GetInnerRadius(ChartAxisSide.Secondary)), projection.Project(0, 10, ChartAxisSide.Secondary).X, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Project(0, 0, ChartAxisSide.Secondary));
        Assert.False(projection.Contains(0, 0, ChartAxisSide.Secondary));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Project(0, -1));
        Assert.Null(projection.ConstrainRadius(new ChartAxisWindow(-100, 200)));
        var confined = projection.ConstrainRadius(new ChartAxisWindow(70, 110))!; Assert.Equal(60, confined.Minimum); Assert.Equal(100, confined.Maximum);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(1000, chart.Options.SecondaryYAxis.Maximum);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); chart.Options.SecondaryYAxis.Reversed = !secondaryReversed; chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(360, false, false)] [InlineData(360, true, true)] [InlineData(800, false, true)] [InlineData(800, true, false)]
    public void DirectionalSectorMaskPaintsOnlyItsSectorAcrossBothNativeExports(int width, bool dark, bool reversed) {
        var points = Enumerable.Range(0, 12).Select(index => new ChartPoint(index * Math.PI / 6, 80)).ToArray();
        var chart = Create(points).WithSize(width, 380).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Options.TransparentBackground = true; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false;
        chart.Options.XAxis.Reversed = reversed; chart.Options.PolarRotationDegrees = 25;
        chart.Series[0].ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.FillOpacity = 1; options.OutlineStroke = new ChartStrokeStyle(width: 0); });
        chart.Series[0].Markers.Enabled = false;
        var prepared = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(0, Math.PI / 2)));
        var projection = prepared.PolarProjection!; var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 2));
        var inside = projection.Project(Math.PI / 4, 35); var outside = projection.Project(Math.PI, 35);
        Assert.True(Alpha(image, inside) > 0); Assert.Equal(0, Alpha(image, outside));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(image.Pixels, RasterImageDecoder.Decode(png).Pixels);
        Assert.Contains(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName == "clipPath");
        Capture("polar-sector-" + width + "-" + (dark ? "dark" : "light") + (reversed ? "-clockwise" : "-anticlockwise"), svg, png);
    }

    [Fact]
    public void UnsupportedFamiliesUnavailableAxesAndInvalidWindowsRejectBeforeChangingTheModel() {
        Assert.Throws<ArgumentException>(() => new ChartPolarViewport());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPolarViewport(angle: new ChartAxisWindow(0, Math.PI * 2 + .01)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPolarViewport(angle: new ChartAxisWindow(-double.MaxValue, double.MaxValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxisWindow(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxisWindow(double.NaN, 1));
        var chart = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => Prepare(chart, new ChartPolarViewport(radius: new ChartAxisWindow(-1, 50))));
        Assert.Throws<NotSupportedException>(() => Prepare(chart, new ChartPolarViewport(secondaryRadius: new ChartAxisWindow(1, 2))));
        Assert.Throws<ArgumentOutOfRangeException>(() => Prepare(chart, new ChartPolarViewport(radius: new ChartAxisWindow(1e-200, 2e-200))));
        var radar = Chart.Create().AddRadar("Categories", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2), new ChartPoint(2, 3) });
        Assert.Throws<NotSupportedException>(() => radar.PreparePolar(VisualExportRequest.ForChart(radar).Context));
        chart.Series[0].Points.Clear(); Assert.Throws<InvalidOperationException>(() => Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(0, 1))));
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(100, chart.Options.YAxis.Maximum);
    }

    internal static Chart Create(ChartPoint[]? points = null) {
        points ??= new[] { new ChartPoint(0, 20), new ChartPoint(1, 50), new ChartPoint(2, 80), new ChartPoint(3, 100) };
        var series = new ChartSeries(new ChartSeriesId("readings"), "Readings", ChartSeriesKind.Polar, points).WithColor(ChartColor.FromHex("#2563EB"));
        for (var index = 0; index < points.Length; index++) series.WithObservationId(index, new ChartObservationId("reading-" + index));
        series.ConfigureMarkers(options => { options.Enabled = true; options.Radius = 5; });
        return Chart.Create().WithSize(600, 380).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).AddSeries(series);
    }
    internal static PreparedVisual Prepare(Chart chart, ChartPolarViewport? viewport = null) => chart.PreparePolar(VisualExportRequest.ForChart(chart).Context, viewport);
    internal static VisualSceneGroup[] Sources(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "polar-point-source").ToArray();
    private static byte Alpha(RgbaImage image, ChartPoint point) => image.Pixels[((int)Math.Round(point.Y) * image.Width + (int)Math.Round(point.X)) * 4 + 3];
    internal static void Capture(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
}
