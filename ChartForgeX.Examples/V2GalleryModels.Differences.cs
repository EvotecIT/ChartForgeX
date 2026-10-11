using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart? Differences(ChartSeriesKind kind, string variant) {
        if (kind != ChartSeriesKind.DifferenceArea) return null;
        var dated = variant is "dated-gaps" or "compact-dated-gaps";
        var day = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var first = new[] { 42d, 68, 48, 60, 74, 46, 62 };
        var second = new[] { 56d, 54, 60, 60, 58, 64, 50 };
        var pairs = new ChartDifferencePoint[first.Length];
        for (var item = 0; item < pairs.Length; item++) pairs[item] = dated
            ? new ChartDifferencePoint(day.AddDays(item + (item >= 4 ? 2 : 0)), first[item], second[item], item == 4)
            : new ChartDifferencePoint(item + 1, first[item], second[item]);
        var chart = Chart.Create().WithXAxis(dated ? "Observation date (UTC)" : "Observation")
            .WithYAxis(dated ? "Primary reference" : "Completed requests");
        if (dated) {
            chart.ConfigureXAxis(axis => axis.WithTimeScale());
            chart.AddLine("Primary reference", new[] { new ChartPoint(day, 1), new ChartPoint(day.AddDays(8), 2) });
        }
        chart.AddDifferenceArea("Actual and forecast", pairs);
        var series = chart.Series[dated ? 1 : 0];
        series.ConfigureDifferenceArea(options => {
            options.FirstName = "Actual"; options.SecondName = "Forecast";
            options.PositiveFill = ChartColor.FromHex("#267EAB"); options.NegativeFill = ChartColor.FromHex("#D47A34");
            options.PositiveFillOpacity = .48; options.NegativeFillOpacity = .48;
        }).WithMarkerRadius(3);
        if (dated) {
            series.UseSecondaryYAxis().WithFillPattern(ChartFillPattern.DiagonalForward);
            chart.WithSecondaryYAxis("Completed requests");
        }
        return chart;
    }
}
