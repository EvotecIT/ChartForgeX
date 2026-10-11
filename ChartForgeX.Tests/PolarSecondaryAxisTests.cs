using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PolarSecondaryAxisTests {
    [Fact]
    public void PolarRejectsANegativeMinimumOnTheSelectedSecondaryRadiusAxis() {
        var chart = Chart.Create().AddPolar("Rate", new[] { new ChartPoint(0, 5), new ChartPoint(1, 8) });
        chart.Series[0].UseSecondaryYAxis(); chart.Options.SecondaryYAxis.WithBounds(-10, 10);
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Radar, null, 4)]
    [InlineData(ChartSeriesKind.Polar, null, 4)]
    [InlineData(ChartSeriesKind.Radar, 2.5, 3)]
    [InlineData(ChartSeriesKind.Polar, 2.5, 3)]
    public void RadiusCaptionsUseTheirOwnAxisFormatterWithoutDuplicatingGridRings(ChartSeriesKind kind, double? intervals, int expectedRings) {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(0, 50), new ChartPoint(1, 50), new ChartPoint(2, 50) };
        if (kind == ChartSeriesKind.Radar) chart.AddRadarLine("Counts", points).AddRadarLine("Rate", points);
        else chart.AddPolar("Counts", points).AddPolar("Rate", points);
        chart.Series[1].UseSecondaryYAxis(); chart.Options.YAxis.TickCount = 5; chart.Options.SecondaryYAxis.TickCount = 5;
        chart.Options.YAxis.LabelFormatter = value => "Count " + value;
        chart.Options.SecondaryYAxis.LabelFormatter = value => "Rate " + value;
        chart.Options.PolarGridRingCount = intervals;
        if (intervals.HasValue) chart.WithPolarGridShape(ChartPolarGridShape.Circle);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Regions, region => region.Label!.StartsWith("Count ", StringComparison.Ordinal));
        Assert.Contains(prepared.Regions, region => region.Label!.StartsWith("Rate ", StringComparison.Ordinal));
        var rings = prepared.Scene.Nodes.Where(node => node.Role == (kind == ChartSeriesKind.Radar ? "radar-ring" : "polar-ring")).ToArray();
        Assert.Equal(expectedRings, rings.Length);
        if (intervals.HasValue) Assert.All(rings, node => Assert.IsType<VisualSceneEllipse>(node));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Radar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    [InlineData(ChartSeriesKind.Polar, false)]
    [InlineData(ChartSeriesKind.Polar, true)]
    public void SeriesUseTheirSelectedRadiusDomainAndReversal(ChartSeriesKind kind, bool reversed) {
        var chart = Chart.Create().WithSize(400, 360).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 10);
        var first = new[] { new ChartPoint(0, 75), new ChartPoint(1, 75), new ChartPoint(2, 75) };
        var second = new[] { new ChartPoint(0, reversed ? 2.5 : 7.5), new ChartPoint(1, 5), new ChartPoint(2, 5) };
        if (kind == ChartSeriesKind.Radar) chart.AddRadarLine("Counts", first).AddRadarLine("Rate", second);
        else chart.AddPolar("Counts", first).AddPolar("Rate", second);
        chart.Series[1].UseSecondaryYAxis();
        chart.Options.SecondaryYAxis.WithReversal(reversed);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var primary = Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0").Bounds;
        var secondary = Assert.Single(prepared.Regions, region => region.Id == "series-1-point-0").Bounds;
        Assert.Equal(primary.X, secondary.X, 8); Assert.Equal(primary.Y, secondary.Y, 8);
        Assert.Equal(primary.Width, secondary.Width, 8); Assert.Equal(primary.Height, secondary.Height, 8);
        var fact = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-0");
        Assert.Equal((reversed ? 2.5 : 7.5).ToString(System.Globalization.CultureInfo.InvariantCulture), fact.Metadata["data-cfx-value"]);
    }
}
