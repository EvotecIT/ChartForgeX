using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One colour and ordering owner for the static renderers and their legends.</summary>
internal static class ChartSeriesColours {
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
