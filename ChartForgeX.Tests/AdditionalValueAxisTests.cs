using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Value axes stacked outward on one side: binding, column layout, compact fallback and declined viewports.</summary>
public sealed class AdditionalValueAxisTests {
    [Fact]
    public void AnAdditionalAxisStacksOutsideTheSecondaryAndScalesOnlyItsOwnSeries() {
        var (chart, humidity) = Climate(800);
        var prepared = Prepare(chart);
        var plot = prepared.Regions.Single(region => region.Role == "cartesian-plot").Bounds;

        var secondary = Texts(prepared, "axis-secondary-y-label");
        var additional = Texts(prepared, "axis-additional-y-label");
        Assert.NotEmpty(secondary);
        // The authored step decides the captions, and the column starts after the secondary captions with a gap.
        Assert.Equal(new[] { "30", "40", "50", "60", "70" }, additional.OrderByDescending(text => text.Baseline).Select(Caption));
        Assert.True(additional.Min(text => text.X) > secondary.Max(text => text.X + text.Text.Metrics.Width), "The additional column overlaps the secondary captions.");
        Assert.All(additional, text => Assert.True(text.X + text.Text.Metrics.Width <= chart.Options.Size.Width));
        // Captions take the bound series colour, so the reader can match axis and line.
        var context = VisualExportRequest.ForChart(chart).Context;
        var humidityColor = ChartSeriesColours.Resolve(chart.Series[2], 2, context.Theme.Resolve(context.ThemeMode));
        Assert.All(additional, text => Assert.Equal(humidityColor, text.Color));
        Assert.Single(Texts(prepared, "axis-additional-y-title"));

        // 70 % is the authored top of the humidity axis and 30 % its bottom; the primary and secondary domains are far away.
        Assert.Equal(plot.Top, PointCenter(prepared, 2, 0), 3);
        Assert.Equal(plot.Bottom, PointCenter(prepared, 2, 1), 3);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "grid-additional-y" or "grid-additional-y-minor");

        humidity.Configure(axis => axis.WithMinorTicks(1).ConfigureGrid(grid => grid.WithLines(true)));
        var gridded = Prepare(chart);
        Assert.Equal(5, gridded.Scene.Nodes.Count(node => node.Role == "grid-additional-y"));
        Assert.Contains(gridded.Scene.Nodes, node => node.Role == "grid-additional-y-minor");
    }

    [Fact]
    public void ACompactChartOmitsAnAdditionalColumnWithoutRoomInsteadOfOverlapping() {
        var (chart, _) = Climate(300);
        var pressure = chart.AddValueAxis(ChartAxisPlacement.End, "Pressure (Pa)",
            axis => axis.WithBounds(0, 12000).WithLabelFormatter(value => value.ToString("N0", CultureInfo.InvariantCulture) + " Pa"));
        chart.AddLine("Pressure", new[] { new ChartPoint(0, 9000), new ChartPoint(1, 11000) });
        chart.Series[3].UseValueAxis(pressure);
        var prepared = Prepare(chart);

        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.additional-axis-omitted");
        var plot = prepared.Regions.Single(region => region.Role == "cartesian-plot").Bounds;
        Assert.True(plot.Width >= chart.Options.Size.Width * .4, "Stacked columns should not take more than half of the chart width.");
        var columns = new[] { "axis-y-label", "axis-secondary-y-label", "axis-additional-y-label" }.SelectMany(role => Texts(prepared, role)).ToArray();
        foreach (var text in columns)
            Assert.DoesNotContain(columns, other => !ReferenceEquals(text, other) && Overlaps(text, other));
        Assert.DoesNotContain(Texts(prepared, "axis-additional-y-label"), text => Caption(text).Contains("000", StringComparison.Ordinal));
    }

    [Fact]
    public void ViewportsDeclineChartsWithAdditionalAxes() {
        var (chart, _) = Climate(800);
        var prepared = Prepare(chart);
        Assert.Null(prepared.XProjection);
        Assert.Null(prepared.Scene.DataProjection?.Y);
        Assert.Throws<NotSupportedException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context,
            new ChartCartesianViewport(new ChartAxisWindow(6, 18))));
    }

    private static (Chart Chart, ChartAdditionalValueAxis Humidity) Climate(int width) {
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false).WithYAxis("Load (kW)").WithSecondaryYAxis("Supply air (°C)")
            .AddArea("Load", new[] { new ChartPoint(0, 250), new ChartPoint(1, 380) })
            .AddLine("Supply air", new[] { new ChartPoint(0, 19.5), new ChartPoint(1, 24) });
        chart.Series[1].UseSecondaryValueAxis();
        var humidity = chart.AddValueAxis(ChartAxisPlacement.End, "Humidity (%)", axis => axis.WithBounds(30, 70).WithTickStep(10));
        chart.AddLine("Humidity", new[] { new ChartPoint(0, 70), new ChartPoint(1, 30) });
        chart.Series[2].UseValueAxis(humidity);
        return (chart, humidity);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Texts(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == role).ToArray();
    private static string Caption(VisualSceneText text) => string.Join(" ", text.Text.Lines.Select(line => line.Text));
    private static double PointCenter(PreparedVisual prepared, int series, int point) {
        var bounds = prepared.Regions.Single(region => region.Id == "series-" + series.ToString(CultureInfo.InvariantCulture) + "-point-" + point.ToString(CultureInfo.InvariantCulture)).Bounds;
        return bounds.Top + bounds.Height / 2;
    }
    private static bool Overlaps(VisualSceneText first, VisualSceneText second) {
        var a = Bounds(first); var b = Bounds(second);
        return a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
    }
    private static ChartRect Bounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Metrics.Height, text.Text.Metrics.Width, text.Text.Metrics.Height);
}
