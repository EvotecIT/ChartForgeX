using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawWordCloud(RgbaCanvas c, Chart chart, ChartRect plot) {
        var terms = ChartWordCloudLayout.Compute(chart, plot);
        if (terms.Count == 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.WordCloud);
        var t = chart.Options.Theme;
        for (var i = 0; i < terms.Count; i++) {
            var term = terms[i];
            var color = PngWordCloudTermColor(series, t, term.PointIndex, i);
            c.DrawTextRotatedEmphasized(term.X, term.Y, term.Text, color, term.FontSize, term.Angle, term.Width / 2.0, term.Height / 2.0);
        }
    }

    private static bool IsWordCloudChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.WordCloud);

    private static ChartColor PngWordCloudTermColor(ChartSeries series, ChartTheme theme, int pointIndex, int renderIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        return series.Color ?? theme.Palette[renderIndex % theme.Palette.Length];
    }
}
