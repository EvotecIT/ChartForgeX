using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects least-squares source facts when callers change coordinate origin or units.</summary>
public sealed class TrendLineNumericalTests {
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1_000_000_000, 1)]
    [InlineData(1_000_000_000_000, 1)]
    [InlineData(1_700_000_000_000, 1)]
    [InlineData(0, .0001)]
    public void TranslationAndSmallUnitsPreserveTheFittedEndpointsAndRegressionFacts(double origin, double unit) {
        var source = new[] { new ChartPoint(origin, 2), new ChartPoint(origin + unit, 5), new ChartPoint(origin + 2 * unit, 7) };
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("measured-trend"), "Trend", source);
        Assert.Equal(source, series.TrendLineSourcePoints);
        // The independently calculated fit for X = 0, 1, 2 has slope 5/2 and intercept 13/6.
        Assert.Equal(13d / 6, series.Points[0].Y, 10);
        Assert.Equal(43d / 6, series.Points[1].Y, 10);
        Assert.Equal(source[0].X, series.Points[0].X);
        Assert.Equal(source[2].X, series.Points[1].X);

        var chart = Chart.Create().WithSize(400, 260).WithAxes(false).WithGrid(false).WithLegend(false).AddSeries(series);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regression = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "regression");
        var slope = double.Parse(regression.Metadata["data-cfx-slope"], CultureInfo.InvariantCulture);
        var intercept = double.Parse(regression.Metadata["data-cfx-intercept"], CultureInfo.InvariantCulture);
        Assert.Equal(5, slope * (source[2].X - source[0].X), 10);
        Assert.InRange(Math.Abs(intercept - (13d / 6 - 2.5 / unit * origin)), 0, Math.Max(1e-10, Math.Abs(intercept) * 1e-14));
        Assert.Equal("3", regression.Metadata["data-cfx-source-count"]);
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "trend-line");
        Assert.True(path.Commands[0].X < path.Commands[^1].X);
        Assert.True(path.Commands[0].Y > path.Commands[^1].Y);
    }

    [Fact]
    public void DateTimeObservationsOneSecondApartRemainDistinct() {
        var start = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        var source = new[] { new ChartPoint(start, 2), new ChartPoint(start.AddSeconds(1), 7) };
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("dated-trend"), "Trend", source);
        Assert.Equal(source, series.TrendLineSourcePoints);
        Assert.Equal(2, series.Points[0].Y);
        Assert.Equal(7, series.Points[1].Y);
    }

    [Fact]
    public void ConstantXStillRejectsARegressionWithoutChangingTheChart() {
        var chart = Chart.Create().AddLine("Current", new[] { new ChartPoint(1, 2) });
        Assert.Throws<ArgumentException>(() => chart.AddTrendLine("Invalid", new[] { new ChartPoint(1e12, 2), new ChartPoint(1e12, 7) }));
        Assert.Single(chart.Series);
    }
}
