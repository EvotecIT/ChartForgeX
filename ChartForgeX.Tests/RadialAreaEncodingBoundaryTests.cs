using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Guards opt-in admission, unchanged default owners and retained empty/refill semantics.</summary>
public sealed class RadialAreaEncodingBoundaryTests {
    [Fact]
    public void ExplicitRadiusRetainsDefaultBytesAndDoesNotChangeTheLegacyPolarAreaLaw() {
        var chart = RadialAreaEncodingTests.Create().AddRadialColumn("Observed", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        var svg = chart.ToSvg(); var png = chart.ToPng();
        chart.WithRadialGeometry(new(valueEncoding: ChartRadialValueEncoding.Radius));
        Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng());
        Assert.DoesNotContain("data-cfx-value-encoding", svg, StringComparison.Ordinal);
        var legacy = RadialAreaEncodingTests.Create().AddPolarArea("Legacy rose", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        var prepared = legacy.Prepare(new VisualRenderContext());
        var wedges = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role == "polar-area-segment").ToArray();
        Assert.Equal(.5, wedges[0].Outer / wedges[1].Outer, 9);
        Assert.DoesNotContain("data-cfx-value-encoding", prepared.ToSvg(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartSeriesKind.RadialBar)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.Radar)]
    public void AreaRejectsOtherCoordinateOwnersInsteadOfChangingTheirMeaning(ChartSeriesKind kind) {
        var chart = RadialAreaEncodingTests.Create().WithRadialGeometry(RadialAreaEncodingTests.Geometry())
            .AddSeries(new ChartSeries(new ChartSeriesId("other"), "Other", kind, new[] { new ChartPoint(1, 25), new ChartPoint(2, 50), new ChartPoint(3, 75) }));
        Assert.Throws<NotSupportedException>(() => chart.ToSvg());
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(ChartScaleKind.Time)]
    public void AreaRejectsNonlinearSelectedRadiusAxes(ChartScaleKind scale) {
        var chart = RadialAreaEncodingTests.Create().WithRadialGeometry(RadialAreaEncodingTests.Geometry()).AddRadialColumn("Observed", new[] { new ChartPoint(1, 25) });
        chart.Options.YAxis.WithBounds(1, 100).WithScale(scale);
        Assert.Throws<NotSupportedException>(() => chart.ToSvg());
    }

    [Fact]
    public void AreaRejectsNegativeSourceOrSelectedBoundsAndMixedRadarWithoutRewritingThem() {
        var chart = RadialAreaEncodingTests.Create().WithRadialGeometry(RadialAreaEncodingTests.Geometry()).AddRadialColumn("Observed", new[] { new ChartPoint(1, 25) });
        chart.Options.SecondaryYAxis.WithBounds(-20, -1); // The unused axis does not participate.
        Assert.NotEmpty(chart.ToPng());
        chart.Series[0].Points[0] = new ChartPoint(1, -1); Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Equal(-1, chart.Series[0].Points[0].Y); chart.Series[0].Points[0] = new ChartPoint(1, 25);
        chart.Options.YAxis.WithBounds(-10, 100); Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        chart.Options.YAxis.WithBounds(0, 100); chart.AddRadar("Reference", new[] { new ChartPoint(1, 25), new ChartPoint(2, 50), new ChartPoint(3, 75) });
        Assert.Throws<NotSupportedException>(() => chart.ToSvg());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(valueEncoding: (ChartRadialValueEncoding)99));
    }

    [Fact]
    public void MissingZeroAllZeroAndClearRefillRetainRealSourceIdentity() {
        var series = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 0), new ChartPoint(3, 25) })
            .WithObservationId(0, new ChartObservationId("north")).WithObservationId(1, new ChartObservationId("east"));
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithRadialGeometry(RadialAreaEncodingTests.Geometry()).AddSeries(series);
        var before = chart.Prepare(new VisualRenderContext());
        Assert.Single(NumericRadialSeriesTests.Marks(before.Scene)); Assert.Equal(2, before.Regions.Count(region => region.Role == "point"));
        Assert.Equal("zero", RadialAreaEncodingTests.Facts(before.Scene, 0, 0)["data-cfx-geometry-status"]);
        Assert.DoesNotContain(before.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-0-point-2");
        series.Points[1] = new ChartPoint(3, 0); var zeros = chart.Prepare(new VisualRenderContext());
        Assert.Empty(NumericRadialSeriesTests.Marks(zeros.Scene));
        var axis = zeros.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "radial-value-axis");
        Assert.Equal("0", axis.Metadata["data-cfx-min"]); Assert.True(double.Parse(axis.Metadata["data-cfx-max"], System.Globalization.CultureInfo.InvariantCulture) > 0);
        series.Points.Clear(); var empty = chart.Prepare(new VisualRenderContext());
        Assert.Contains(empty.Diagnostics, item => item.Code == "numeric-radial.no-data"); Assert.Empty(NumericRadialSeriesTests.Marks(empty.Scene));
        series.Points.Add(new ChartPoint(1, 50)); var refill = chart.Prepare(new VisualRenderContext());
        Assert.Single(NumericRadialSeriesTests.Marks(refill.Scene)); Assert.Equal("observed", RadialAreaEncodingTests.Facts(refill.Scene, 0, 0)["data-cfx-series-key"]);
        chart.ClearSeries(); Assert.NotEmpty(chart.ToSvg()); chart.AddSeries(series); Assert.NotEmpty(chart.ToPng());
        Assert.Single(NumericRadialSeriesTests.Marks(before.Scene)); // The earlier prepared snapshot remains detached.
    }
}
