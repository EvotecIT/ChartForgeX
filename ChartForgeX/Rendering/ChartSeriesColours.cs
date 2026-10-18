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
        return State(series.StateRole, colors, Palette(series, index, colors));
    }

    internal static ChartColor Palette(ChartSeries series, int index, VisualThemeColors colors, int offset = 0) =>
        colors.Palette[(PaletteIndex(series, index, colors.Palette.Count) + offset) % colors.Palette.Count];

    /// <summary>Part-to-whole categories retain their own ordinal palette instead of inheriting one series slot.</summary>
    internal static ChartColor CategoryPoint(ChartSeries series, int point, VisualThemeColors colors) =>
        PointOverride(series, point) ?? series.Color ?? State(series.StateRole, colors, colors.Palette[point % colors.Palette.Count]);

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
        PointOverride(series, pointIndex) ?? Resolve(series, seriesIndex, colors);

    internal static ChartColor Point(ChartSeries series, int pointIndex, VisualThemeColors colors, ChartColor fallback) =>
        PointOverride(series, pointIndex) ?? series.Color ?? State(series.StateRole, colors, fallback);

    private static ChartColor? PointOverride(ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count ? series.PointColors[pointIndex] : null;

    internal static ChartColor Resolve(Chart chart, int index) {
        var series = chart.Series[index]; var t = chart.Options.ResolvedTheme;
        if (series.Color.HasValue) return series.Color.Value;
        return series.StateRole switch {
            ChartSeriesState.Danger => t.Negative,
            ChartSeriesState.Warning => t.Warning,
            ChartSeriesState.Info => t.Info,
            ChartSeriesState.Neutral => t.Neutral,
            ChartSeriesState.Success or ChartSeriesState.Quiet => ChartSeriesKindTraits.UsesOptionalLineMarker(series.Kind) ? t.QuietLine : t.Quiet,
            _ => t.Palette[PaletteIndex(series, index, t.Palette.Length)]
        };
    }

    /// <summary>Explicit identities select deterministic palette slots; chart-local anonymous helpers retain ordinal paint.</summary>
    /// <remarks>Hashing never depends on the current series set, so adding or removing another series cannot recolor an existing ID.
    /// A finite palette can repeat. An explicit series color remains the way to guarantee a distinct authored color.</remarks>
    private static int PaletteIndex(ChartSeries series, int index, int count) {
        if (series.AutomaticIdentityOwner != null || !series.HasIdentity) return index % count;
        // FNV-1a over UTF-16 code units is stable across runtimes and platforms, unlike string.GetHashCode().
        var hash = 2166136261u;
        foreach (var value in series.Id.Value) hash = unchecked((hash ^ value) * 16777619u);
        return (int)(hash % (uint)count);
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
