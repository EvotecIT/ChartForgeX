using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ComboHelpersRenderMixedSeries() {
        var areaCombo = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(640, 360)
            .WithXLabels("Q1", "Q2", "Q3")
            .AddColumnAreaCombo(
                "Actual",
                Points(42, 64, 58),
                "Projected",
                Points(46, 68, 62),
                ChartColor.FromRgb(14, 165, 233),
                ChartColor.FromRgb(37, 99, 235));

        var areaSvg = areaCombo.ToSvg();
        Assert(areaCombo.Series.Count == 2, "Column-area combos should add exactly two series.");
        Assert(areaCombo.Series[0].Kind == ChartSeriesKind.Bar, "Column-area combos should add columns as bar series.");
        Assert(areaCombo.Series[1].Kind == ChartSeriesKind.Area && areaCombo.Series[1].Smooth, "Column-area combos should add a smooth area series.");
        Assert(CountOccurrences(areaSvg, "data-cfx-role=\"bar\"") == 3, "Column-area combos should render one column per point.");
        Assert(areaSvg.Contains("data-cfx-role=\"area\"", StringComparison.Ordinal), "Column-area combos should render an area path.");
        Assert(areaCombo.ToPng().Length > 64, "Column-area combos should render PNG output.");

        var scatterCombo = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(640, 360)
            .AddScatterLineCombo(
                "Observed",
                Points(12, 18, 17),
                "Target",
                Points(14, 16, 20),
                ChartColor.FromRgb(37, 99, 235),
                ChartColor.FromRgb(245, 158, 11));

        var scatterSvg = scatterCombo.ToSvg();
        Assert(scatterCombo.Series.Count == 2, "Scatter-line combos should add exactly two series.");
        Assert(scatterCombo.Series[0].Kind == ChartSeriesKind.Scatter, "Scatter-line combos should add scatter points first.");
        Assert(scatterCombo.Series[1].Kind == ChartSeriesKind.Line, "Scatter-line combos should add a line overlay.");
        Assert(CountOccurrences(scatterSvg, "data-cfx-role=\"scatter-point\"") == 3, "Scatter-line combos should render one marker per scatter point.");
        Assert(CountOccurrences(scatterSvg, "data-cfx-role=\"line\"") == 1, "Scatter-line combos should render one line overlay.");
        Assert(scatterCombo.ToPng().Length > 64, "Scatter-line combos should render PNG output.");
    }
}
