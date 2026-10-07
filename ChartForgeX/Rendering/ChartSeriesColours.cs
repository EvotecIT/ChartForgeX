using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>One colour and ordering owner for the static renderers and their legends.</summary>
internal static class ChartSeriesColours {
    /// <summary>Resolves prepared series paint from canonical context tokens, with explicit paint taking precedence.</summary>
    internal static ChartColor Resolve(Chart chart, int index, VisualThemeColors colors) => Resolve(chart.Series[index], index, colors);

    internal static ChartColor Resolve(ChartSeries series, int index, VisualThemeColors colors) {
        if (series.Color.HasValue) return series.Color.Value;
        return State(series.StateRole, colors, colors.Palette[index % colors.Palette.Count]);
    }

    internal static ChartColor State(ChartSeriesState state, VisualThemeColors colors, ChartColor fallback) {
        if (state == ChartSeriesState.None) return fallback;
        var status = colors.Status;
        return state switch {
            ChartSeriesState.Danger => status.Critical.Fill,
            ChartSeriesState.Warning => status.Medium.Fill,
            ChartSeriesState.Info => status.Info.Fill,
            ChartSeriesState.Neutral => status.Neutral.Fill,
            ChartSeriesState.Success or ChartSeriesState.Quiet => status.Pass.Fill,
            _ => fallback
        };
    }

    internal static ChartColor Point(ChartSeries series, int seriesIndex, int pointIndex, VisualThemeColors colors) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value : Resolve(series, seriesIndex, colors);

    internal static ChartColor Resolve(Chart chart, int index) {
        var series = chart.Series[index]; var t = chart.Options.Theme;
        if (series.Color.HasValue) return series.Color.Value;
        return series.StateRole switch {
            ChartSeriesState.Danger => t.Negative,
            ChartSeriesState.Warning => t.Warning,
            ChartSeriesState.Info => t.Info,
            ChartSeriesState.Neutral => t.Neutral,
            ChartSeriesState.Success or ChartSeriesState.Quiet => ChartSeriesKindTraits.UsesOptionalLineMarker(series.Kind) ? t.QuietLine : t.Quiet,
            _ => t.Palette[index % t.Palette.Length]
        };
    }
    internal static int Priority(ChartSeriesState role) => role switch {
        ChartSeriesState.Success or ChartSeriesState.Quiet => 0,
        ChartSeriesState.Neutral => 1,
        ChartSeriesState.None => 2,
        ChartSeriesState.Info => 3,
        ChartSeriesState.Warning => 4,
        ChartSeriesState.Danger => 5,
        _ => 2
    };
    internal static IEnumerable<int> DrawingOrder(Chart chart) => Enumerable.Range(0, chart.Series.Count).OrderBy(i => Priority(chart.Series[i].StateRole));
}
