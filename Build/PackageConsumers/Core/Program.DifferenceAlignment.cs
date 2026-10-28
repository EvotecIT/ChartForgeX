using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void AlignedDifferenceContracts() {
        var actual = ChartSeries.CreateLine(new ChartSeriesId("actual"), "Actual", new[] { new ChartPoint(1, 2), new ChartPoint(2, 4), new ChartPoint(4, 80), new ChartPoint(8, 5) });
        var forecast = ChartSeries.CreateLine(new ChartSeriesId("forecast"), "Forecast", new[] { new ChartPoint(1, 20), new ChartPoint(3, 15), new ChartPoint(4, 10), new ChartPoint(8, 50) });
        var captured = ChartDifferencePoints.Align(actual, forecast);
        PackageAssertions.Require(captured.FirstPoints.Count == 4 && captured.Points.Count == 3 && captured.Points[1].BreakBefore,
            "Packed exact-X alignment fabricated pairs or lost a missing union gap.");
        var series = ChartSeries.CreateAlignedDifferenceArea(new ChartSeriesId("comparison"), "Comparison", captured);
        var chart = Chart.Create().WithSize(640, 420).AddSeries(series); chart.Options.YAxis.WithLogarithmicScale();
        var prepared = chart.Prepare(DepthContext(chart, showLegend: false)); var svg = prepared.ToSvg();
        PackageAssertions.Require(PackageAssertions.Contains(svg, "data-cfx-first-source-series-id=\"actual\"")
            && PackageAssertions.Contains(svg, "difference-positive") && PackageAssertions.Contains(svg, "difference-negative"),
            "Packed nonlinear alignment lost source provenance or crossing fills.");
        var png = prepared.ToPng(); actual.Points.Clear(); forecast.WithName("Changed");
        PackageAssertions.Require(svg == prepared.ToSvg() && png.SequenceEqual(prepared.ToPng()), "Packed aligned preparation is not detached.");
        PackageAssertions.Png(png);
    }
}
