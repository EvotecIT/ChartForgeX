using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Native navigation owns angular direction, annular transform confinement and immutable source facts.</summary>
public sealed class PolarNavigationTests {
    private const ChartPolarNavigationAxes Both = ChartPolarNavigationAxes.Angle | ChartPolarNavigationAxes.Radius;
    private const ChartPolarNavigationAxes All = Both | ChartPolarNavigationAxes.SecondaryRadius;
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ZoomUsesEachCapturedLinearLogarithmicAndReversedAnnulus(bool primaryReversed, bool secondaryReversed) {
        var chart = Model(); chart.Options.YAxis.Reversed = primaryReversed; chart.Options.SecondaryYAxis.Reversed = secondaryReversed;
        var projection = Prepare(chart, Window()).PolarProjection!; var anchor = Anchor(projection, 6.1, .8);
        var request = projection.Zoom(2, anchor, All)!;
        Assert.Equal(primaryReversed ? 28 : 42, request.Radius!.Minimum, 8);
        Assert.Equal(primaryReversed ? 58 : 72, request.Radius.Maximum, 8);
        Assert.Equal(Math.Pow(10, secondaryReversed ? 1.2 : 1.3), request.SecondaryRadius!.Minimum, 8);
        Assert.Equal(Math.Pow(10, secondaryReversed ? 1.7 : 1.8), request.SecondaryRadius.Maximum, 8);
        Assert.Equal(5.8, request.Angle!.Minimum, 9); Assert.Equal(6.45, request.Angle.Maximum, 9);
        Assert.Equal(20, projection.GetRadiusWindow().Minimum); Assert.Equal(1, chart.Options.SecondaryYAxis.Minimum);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PanUsesPhysicalClockwiseDirectionAndPreservesAnUnselectedLogWindow(bool reversed) {
        var chart = Model(); chart.Options.XAxis.Reversed = reversed;
        var projection = Prepare(chart, Window()).PolarProjection!;
        var request = projection.Pan(.2, projection.OuterRadius * .1, Both)!;
        Assert.Equal(5.5 + (reversed ? .2 : -.2), request.Angle!.Minimum, 9);
        Assert.Equal(6.8 + (reversed ? .2 : -.2), request.Angle.Maximum, 9);
        Assert.Equal(12, request.Radius!.Minimum, 8); Assert.Equal(72, request.Radius.Maximum, 8);
        Assert.Equal(projection.Viewport!.SecondaryRadius!.Minimum, request.SecondaryRadius!.Minimum);
        Assert.Equal(projection.Viewport.SecondaryRadius.Maximum, request.SecondaryRadius.Maximum);
        Assert.Equal(10, projection.GetRadiusWindow(ChartAxisSide.Secondary).Minimum);
    }
    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void AcceptedNativeViewsRetainSourceIdentitiesAndBothExportSurfacesAcrossAllPoses(int width, bool dark) {
        var chart = Model().WithSize(width, 420).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var original = chart.Series.Select(series => series.Points.ToArray()).ToArray();
        var prepared = Prepare(chart, Window()); var projection = prepared.PolarProjection!;
        var zoomed = Prepare(chart, projection.Zoom(1.5, Anchor(projection, 6.1, .8), All));
        var panned = Prepare(chart, zoomed.PolarProjection!.Pan(.15, zoomed.PolarProjection.OuterRadius * .03, Both));
        Assert.Equal(SourceFacts(prepared), SourceFacts(zoomed)); Assert.Equal(SourceFacts(prepared), SourceFacts(panned));
        for (var series = 0; series < original.Length; series++) Assert.Equal(original[series], chart.Series[series].Points);
        foreach (var item in new[] { ("zoom", zoomed), ("pan", panned) }) {
            var svg = item.Item2.ToSvg(); var png = item.Item2.ToPng();
            Assert.Contains("clipPath", svg); Assert.True(png.Length > 0);
            var current = item.Item2.PolarProjection!; var reset = current.Zoom(.001, Anchor(current, 6.1, .8), All);
            Assert.Null(reset); Assert.Equal(Prepare(chart, null).ToSvg(), Prepare(chart, reset).ToSvg());
            PolarViewportContractTests.Capture("polar-native-gesture-" + item.Item1 + "-" + width + "-" + (dark ? "dark" : "light"), svg, png);
        }
    }
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ToolbarZoomUsesTheNativeSectorAndSelectedAnnulusMidpoint(bool angleReversed, bool secondaryReversed) {
        var chart = Model(); chart.Options.XAxis.Reversed = angleReversed; chart.Options.SecondaryYAxis.Reversed = secondaryReversed;
        var p = Prepare(chart, Window()).PolarProjection!;
        foreach (var axes in new[] { ChartPolarNavigationAxes.Angle, ChartPolarNavigationAxes.Radius, ChartPolarNavigationAxes.SecondaryRadius, All }) {
            var inner = Math.Max((axes & ChartPolarNavigationAxes.Radius) != 0 ? p.GetInnerRadius() : 0,
                (axes & ChartPolarNavigationAxes.SecondaryRadius) != 0 ? p.GetInnerRadius(ChartAxisSide.Secondary) : 0);
            var anchor = Anchor(p, p.AngleWindow.Minimum / 2 + p.AngleWindow.Maximum / 2, (inner / 2 + p.OuterRadius / 2) / p.OuterRadius);
            var expected = p.Zoom(2, anchor, axes); var actual = p.Zoom(2, axes);
            Assert.Equal(Prepare(chart, expected).ToSvg(), Prepare(chart, actual).ToSvg());
            Assert.Equal(Prepare(chart, expected).ToPng(), Prepare(chart, actual).ToPng());
            Assert.Equal((axes & ChartPolarNavigationAxes.Angle) != 0 ? 5.825 : 5.5, actual!.Angle!.Minimum, 9);
            Assert.Equal((axes & ChartPolarNavigationAxes.Angle) != 0 ? 6.475 : 6.8, actual.Angle.Maximum, 9);
            var primarySelected = (axes & ChartPolarNavigationAxes.Radius) != 0;
            Assert.Equal(primarySelected ? axes == All ? 40 : 35 : 20, actual.Radius!.Minimum, 8);
            Assert.Equal(primarySelected ? axes == All ? 70 : 65 : 80, actual.Radius.Maximum, 8);
            var secondarySelected = (axes & ChartPolarNavigationAxes.SecondaryRadius) != 0;
            Assert.Equal(secondarySelected ? Math.Pow(10, 1.25) : 10, actual.SecondaryRadius!.Minimum, 8);
            Assert.Equal(secondarySelected ? Math.Pow(10, 1.75) : 100, actual.SecondaryRadius.Maximum, 8);
            Assert.Null(p.Zoom(.001, All));
        }
        foreach (var axes in new[] { ChartPolarNavigationAxes.None, (ChartPolarNavigationAxes)8 })
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Zoom(2, axes));
        foreach (var factor in new[] { 0d, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Zoom(factor, All));
        var primary = Prepare(PolarViewportContractTests.Create(), null).PolarProjection!;
        Assert.Throws<NotSupportedException>(() => primary.Zoom(2, ChartPolarNavigationAxes.SecondaryRadius));
        Assert.Equal(Prepare(chart, Window()).ToSvg(), Prepare(chart, p.Zoom(1, All)).ToSvg());
    }
    [Fact]
    public void ZoomOutReturnsFullViewAndNoOpKeepsExactCapturedBounds() {
        var chart = Model(); var projection = Prepare(chart, Window()).PolarProjection!;
        var anchor = Anchor(projection, 6.1, .8);
        var noOp = projection.Zoom(1, anchor, All)!;
        Assert.Equal(projection.Viewport!.Angle!.Minimum, noOp.Angle!.Minimum); Assert.Equal(projection.Viewport.Radius!.Maximum, noOp.Radius!.Maximum);
        Assert.Equal(projection.Viewport.SecondaryRadius!.Maximum, noOp.SecondaryRadius!.Maximum);
        Assert.Null(projection.Zoom(.01, anchor, All));
        Assert.Equal(Prepare(chart, null).ToSvg(), Prepare(chart, projection.Zoom(.01, anchor, All)).ToSvg());
        var zeroPan = projection.Pan(0, 0, All)!; Assert.Equal(projection.Viewport.Angle.Maximum, zeroPan.Angle!.Maximum);
        var full = Prepare(chart, null).PolarProjection!;
        Assert.Null(full.Pan(.5, 0, ChartPolarNavigationAxes.Angle));
    }
    [Fact]
    public void RejectsUnavailableCoordinatesUndefinedAnchorsAndUnrepresentableRequestsWithoutChangingTheScene() {
        var chart = PolarViewportContractTests.Create(); var prepared = Prepare(chart, new ChartPolarViewport(angle: new ChartAxisWindow(5.5, 6.8), radius: new ChartAxisWindow(20, 80)));
        var p = prepared.PolarProjection!; var before = prepared.ToSvg(); var anchor = Anchor(p, 6.1, .8);
        foreach (var axes in new[] { ChartPolarNavigationAxes.None, (ChartPolarNavigationAxes)8 }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Zoom(2, anchor, axes));
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Pan(.1, 1, axes));
        }
        Assert.Throws<NotSupportedException>(() => p.Zoom(2, anchor, ChartPolarNavigationAxes.SecondaryRadius));
        foreach (var factor in new[] { 0d, -1, double.NaN, double.PositiveInfinity, double.MaxValue })
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Zoom(factor, anchor));
        foreach (var point in new[] { p.Center, Anchor(p, 6.1, .1), Anchor(p, 3, .8), Anchor(p, 6.1, 2) })
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Zoom(2, point));
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Pan(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Pan(0, double.PositiveInfinity));
        Assert.Equal(before, prepared.ToSvg()); Assert.Equal(20, p.GetRadiusWindow().Minimum);
    }
    [Fact]
    public void DetachedNavigationIgnoresLaterSourceAxisMutation() {
        var chart = Model(); var p = Prepare(chart, Window()).PolarProjection!; var anchor = Anchor(p, 6.1, .8);
        var expected = p.Zoom(2, anchor, All)!;
        chart.Options.YAxis.Minimum = -100; chart.Options.YAxis.Reversed = true; chart.Options.SecondaryYAxis.LogarithmBase = 2;
        chart.ClearSeries(); var actual = p.Zoom(2, anchor, All)!;
        Assert.Equal(expected.Radius!.Minimum, actual.Radius!.Minimum); Assert.Equal(expected.SecondaryRadius!.Maximum, actual.SecondaryRadius!.Maximum);
    }
    internal static Chart Model() {
        var values = new[] { new ChartPoint(-.25, 35), new ChartPoint(.1, 60), new ChartPoint(.4, 75), new ChartPoint(3, 50, true), new ChartPoint(6.1, 90) };
        var chart = PolarViewportContractTests.Create(values).WithSecondaryYAxisBounds(1, 1000).AddPolar("Log radius", values.Select(point => new ChartPoint(point.X, 30)).ToArray());
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic; return chart;
    }
    internal static ChartPolarViewport Window() => new(angle: new ChartAxisWindow(5.5, 6.8), radius: new ChartAxisWindow(20, 80), secondaryRadius: new ChartAxisWindow(10, 100));
    internal static PreparedVisual Prepare(Chart chart, ChartPolarViewport? view) => chart.PreparePolar(VisualExportRequest.ForChart(chart).Context, view);
    internal static ChartPoint Anchor(ChartPolarProjection p, double radians, double fraction) => new(p.Center.X + Math.Cos((p.AngleReversed ? radians : -radians) + p.RotationRadians) * p.OuterRadius * fraction,
        p.Center.Y + Math.Sin((p.AngleReversed ? radians : -radians) + p.RotationRadians) * p.OuterRadius * fraction);
    private static string[] SourceFacts(PreparedVisual value) => PolarViewportContractTests.Sources(value).Select(group => string.Join("|",
        new[] { "data-cfx-series", "data-cfx-point", "data-cfx-observation-id", "data-cfx-angle", "data-cfx-value", "data-cfx-break-before", "data-cfx-axis" }
            .Select(key => key + "=" + (group.Metadata.TryGetValue(key, out var raw) ? raw : string.Empty)))).ToArray();
}
