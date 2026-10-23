using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects explicit logarithm policies through native geometry, detached projections and HTML source facts.</summary>
public sealed class LogarithmicDomainTests {
    [Theory]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    [InlineData(10, true, false)]
    [InlineData(10, true, true)]
    public void AuthoredBaseAndSignKeepEqualLogStepsSourceIdentityAndNativeExports(double logarithmBase, bool negative, bool reversed) {
        var values = negative ? new[] { -logarithmBase * logarithmBase, -logarithmBase, -1d } : new[] { 1d, logarithmBase, logarithmBase * logarithmBase };
        var chart = Line(values);
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("middle"), new ChartObservationId("last") });
        chart.Options.YAxis.WithLogarithmicScale(logarithmBase, negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive)
            .WithBounds(values[0], values[2]).WithReversal(reversed);
        var prepared = Prepare(chart); var markers = Markers(prepared);
        Assert.Equal(3, markers.Length);
        Assert.Equal((markers[0].Cy + markers[2].Cy) / 2, markers[1].Cy, 8);
        Assert.Equal(reversed, markers[0].Cy < markers[2].Cy);
        var y = Assert.IsType<ChartAxisProjection>(prepared.YProjection);
        Assert.Equal(logarithmBase, y.LogarithmBase);
        Assert.Equal(negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive, y.LogarithmicDomain);
        foreach (var value in values) Assert.Equal(value, y.Unproject(y.Project(value)), 8);
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "first", "middle", "last" }, groups.Select(group => (string?)group.Attribute("data-cfx-observation-id")));
        Assert.Equal(values, groups.Select(group => double.Parse(group.Attribute("data-cfx-y")!.Value, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal((int)prepared.Scene.Size.Width, prepared.ToRgba().Width);
        var svg = prepared.ToSvg(); var pixels = prepared.ToRgba().Pixels;
        chart.Options.YAxis.LogarithmBase = 3; chart.Options.YAxis.Reversed = !reversed;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(pixels, prepared.ToRgba().Pixels);
        Assert.Equal(values[1], y.Unproject(y.Project(values[1])), 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitDefaultBasePreservesExistingSvgPngAndMetadata(bool symmetric) {
        var chart = Line(symmetric ? new[] { -9d, 0, 9 } : new[] { 1d, 10, 100 });
        chart.Options.YAxis.WithScale(symmetric ? ChartScaleKind.SymmetricLogarithmic : ChartScaleKind.Logarithmic);
        var prior = Prepare(chart); var priorHtml = chart.ToInteractiveHtmlFragmentWithoutAssets();
        chart.Options.YAxis.LogarithmBase = 10; chart.Options.YAxis.LogarithmicDomain = ChartLogarithmicDomain.Positive;
        var current = Prepare(chart);
        Assert.Equal(prior.ToSvg(), current.ToSvg()); Assert.Equal(prior.ToPng(), current.ToPng());
        Assert.Equal(priorHtml, chart.ToInteractiveHtmlFragmentWithoutAssets());
    }

    [Fact]
    public void SymmetricLogarithmUsesTheSameAuthoredBaseAndThreshold() {
        var chart = Line(new[] { -14d, 0, 14 });
        chart.Options.YAxis.WithScale(ChartScaleKind.SymmetricLogarithmic).WithBounds(-14, 14);
        chart.Options.YAxis.SymmetricLogarithmThreshold = 2; chart.Options.YAxis.LogarithmBase = 2;
        var prepared = Prepare(chart); var projection = prepared.YProjection!;
        Assert.Equal(-3, ChartScaleTransform.Forward(-14, chart.Options.YAxis), 10);
        Assert.Equal(3, ChartScaleTransform.Forward(14, chart.Options.YAxis), 10);
        Assert.Equal(0, projection.Unproject((projection.PlotBounds.Top + projection.PlotBounds.Bottom) / 2), 10);
        Assert.Equal(-14, projection.Unproject(projection.Project(-14)), 8);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.5)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidConfigurationCannotPartiallyReplaceAnAxis(double invalidBase) {
        var axis = new ChartAxis().WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-16, -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.LogarithmBase = invalidBase);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.WithLogarithmicScale(invalidBase));
        Assert.Throws<InvalidOperationException>(() => axis.LogarithmicDomain = ChartLogarithmicDomain.Positive);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.LogarithmicDomain = (ChartLogarithmicDomain)9);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Maximum = 0);
        Assert.Equal(2, axis.LogarithmBase); Assert.Equal(ChartLogarithmicDomain.Negative, axis.LogarithmicDomain);
        Assert.Equal(-16, axis.Minimum); Assert.Equal(-1, axis.Maximum);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void WrongSignObservationsRejectEvenOutsideExplicitPaintBounds(double wrongSign) {
        var chart = Line(new[] { -16d, -4, wrongSign });
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-16, -1);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }

    [Fact]
    public void EmptySingletonRefillAndOneSidedBoundsStayInTheNegativeDomain() {
        var chart = Line(Array.Empty<double>());
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative);
        Assert.Contains("no-data", Prepare(chart).ToSvg(), StringComparison.Ordinal);
        chart.Series[0].Points.Add(new ChartPoint(1, -8));
        var singleton = Prepare(chart); var projection = singleton.YProjection!;
        Assert.True(projection.FullMinimum < projection.FullMaximum && projection.FullMaximum < 0);
        Assert.Single(Markers(singleton)); Assert.NotEmpty(singleton.ToPng());
        chart.Options.YAxis.Minimum = -.5;
        var clipped = Prepare(chart).YProjection!;
        Assert.Equal(-.5, clipped.Minimum); Assert.True(clipped.Maximum < 0 && clipped.Maximum > clipped.Minimum);
        chart.Series[0].Points.Clear(); Assert.Contains("no-data", Prepare(chart).ToSvg(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NegativeBarBaselinesStayNearestZeroForEachPhysicalValueAxis(bool horizontal) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithBarStyle(ChartBarStyle.Flat);
        var points = new[] { new ChartPoint(1, -4), new ChartPoint(2, -16) };
        if (horizontal) chart.AddHorizontalBar("Losses", points); else chart.AddBar("Losses", points);
        var axis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        axis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-32, -1);
        var prepared = Prepare(chart);
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == (horizontal ? "horizontal-bar" : "bar")).ToArray();
        Assert.Equal(2, bars.Length);
        Assert.Equal(horizontal ? bars[0].Bounds.Right : bars[0].Bounds.Top, horizontal ? bars[1].Bounds.Right : bars[1].Bounds.Top, 8);
        Assert.True(horizontal ? bars[1].Bounds.Width > bars[0].Bounds.Width : bars[1].Bounds.Height > bars[0].Bounds.Height);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NegativeProjectionConfinementAndHtmlMetadataRetainTheCommittedPolicy() {
        var chart = Line(new[] { -64d, -8, -1 });
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-64, -1);
        var projection = Prepare(chart).YProjection!;
        var window = projection.ConstrainViewport(new ChartAxisWindow(-256, -16))!;
        Assert.Equal(-64, window.Minimum, 8); Assert.Equal(-4, window.Maximum, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Project(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Unproject(-1e308));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.ConstrainViewport(new ChartAxisWindow(-4, 1)));
        var match = Regex.Match(chart.ToInteractiveHtmlFragmentWithoutAssets(), "data-cfx-prepared-chart=\"([^\"]+)\"");
        using var json = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
        foreach (var axis in new[] { json.RootElement.GetProperty("dataProjection").GetProperty("y"), json.RootElement.GetProperty("yProjection") }) {
            Assert.Equal(2, axis.GetProperty("logarithmBase").GetDouble());
            Assert.Equal("negative", axis.GetProperty("logarithmicDomain").GetString());
        }
    }

    [Fact]
    public void NegativeXScaleAndSourceAnnotationsShareTheSelectedDomain() {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.All)
            .AddLine("Negative X", new[] { new ChartPoint(-16, -16), new ChartPoint(-4, -4), new ChartPoint(-1, -1) });
        chart.Options.XAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-16, -1);
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-16, -1);
        chart.AddLineSegment(new ChartPoint(-16, -16), new ChartPoint(-1, -1), "Trend")
            .AddPlotRegion(new ChartPlotRegion(0, xMinimum:-8, xMaximum:-2, yMinimum:-8, yMaximum:-2));
        var prepared = Prepare(chart); var markers = Markers(prepared);
        Assert.Equal((markers[0].Cx + markers[2].Cx) / 2, markers[1].Cx, 8);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "annotation-source-line");
        Assert.NotEmpty(prepared.ToPng());
        chart.AddNote(new ChartPoint(-4, 0), "Invalid");
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart.ClearAnnotations(); chart.PlotRegions.Add(new ChartPlotRegion(0, yMinimum:-2, yMaximum:1));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RadialValueOwnersUseNegativeLogarithmsWithoutChangingSourceMagnitudes(bool radar) {
        var chart = Chart.Create().WithSize(600, 440).WithLegend(false);
        var points = new[] { new ChartPoint(1, -16), new ChartPoint(2, -4), new ChartPoint(3, -1) };
        if (radar) chart.AddRadarLine("Losses", points); else chart.AddRadialColumn("Losses", points);
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-32, -.5);
        var prepared = Prepare(chart);
        Assert.Contains("-16", prepared.ToSvg(), StringComparison.Ordinal); Assert.NotEmpty(prepared.ToPng());
        chart.Options.YAxis.LogarithmicDomain = ChartLogarithmicDomain.Negative;
        chart.Series[0].Points[1] = new ChartPoint(2, 0);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void PolarRadiusKeepsItsExistingNonnegativeSourceConstraint() {
        var chart = Chart.Create().AddPolar("Polar", new[] { new ChartPoint(0, -8), new ChartPoint(1, -1) });
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1.0000000000000002)]
    [InlineData(1e300)]
    public void ExtremeBaseAndMagnitudeTickWorkRemainsFiniteAndWithinTheAuthoredBudget(double logarithmBase) {
        var axis = new ChartAxis().WithLogarithmicScale(logarithmBase, ChartLogarithmicDomain.Negative);
        axis.TickCount = 6;
        var ticks = ChartTicks.GenerateInside(axis, -1e300, -1e-300);
        Assert.InRange(ticks.Count, 1, axis.TickCount);
        Assert.All(ticks, tick => Assert.True(double.IsFinite(tick) && tick >= -1e300 && tick <= -1e-300));
        Assert.Equal(ticks.OrderBy(value => value), ticks);
    }

    private static Chart Line(IEnumerable<double> values) => Chart.Create().WithSize(600, 360).WithLegend(false)
        .WithLineMarkers(ChartLineMarkerMode.All).WithXAxisBounds(0, 2)
        .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("source"), "Values", values.Select((value, index) => new ChartPoint(index, value))));
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneEllipse[] Markers(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
}
