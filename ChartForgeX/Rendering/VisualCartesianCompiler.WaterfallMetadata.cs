using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static string WaterfallKind(ChartWaterfallStep step) => step.Kind.ToString().ToLowerInvariant();
    private static string WaterfallRole(ChartWaterfallStep step) => step.IsCheckpoint ? "waterfall-" + WaterfallKind(step) : "point";
    private static string WaterfallId(int seriesIndex, ChartWaterfallStep step) => step.IsAppendedTotal ? SeriesId(seriesIndex) + "-total" : PointId(seriesIndex, step.ItemIndex);
    private static string WaterfallValueName(Chart chart, ChartWaterfallStep step) => step.Kind == ChartWaterfallItemKind.Total ? chart.Options.Labels.Total
        : step.Kind == ChartWaterfallItemKind.Subtotal ? chart.Options.Labels.Subtotal : chart.Options.Labels.Change;

    private static ChartColor WaterfallColor(ChartSeries series, int seriesIndex, ChartWaterfallStep step, VisualThemeColors colors) =>
        HasSeriesPaint(series, step.ItemIndex) ? PointColor(series, seriesIndex, step.ItemIndex, colors)
        : step.IsCheckpoint ? colors.Status.Medium.Fill : step.Value >= 0 ? colors.Status.Pass.Fill : colors.Status.Critical.Fill;

    private static Dictionary<string, string> WaterfallMetadata(Chart chart, int seriesIndex, ChartWaterfallStep step) {
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(step.ItemIndex),
            ["data-cfx-x"] = Number(step.X), ["data-cfx-start"] = Number(step.Start), ["data-cfx-end"] = Number(step.End),
            ["data-cfx-value"] = Number(step.Value), ["data-cfx-waterfall-kind"] = WaterfallKind(step),
            ["data-cfx-label-waterfall-value"] = WaterfallValueName(chart, step),
            ["data-cfx-derived-total"] = step.Kind == ChartWaterfallItemKind.Total ? "true" : "false",
            ["data-cfx-source-count"] = Number(step.SourceIndices.Count)
        };
        if (step.IsCheckpoint) {
            var sources = string.Join(",", step.SourceIndices.Select(Number));
            metadata["data-cfx-derived"] = "waterfall-" + WaterfallKind(step);
            metadata["data-cfx-source-points"] = sources;
            metadata["data-cfx-derived-identity"] = WaterfallKind(step) + ":" + Number(step.X) + ":sources:" + sources;
        } else {
            metadata["data-cfx-source-point"] = Number(step.SourceIndex);
            metadata["data-cfx-delta"] = Number(step.Value);
        }
        return metadata;
    }

    private static IReadOnlyList<VisualLegendEntry> WaterfallLegendEntries(Chart chart, ChartSeries series, VisualThemeColors colors) {
        var entries = new List<VisualLegendEntry>();
        foreach (var step in ChartWaterfallSteps.Create(series).Where(step => !step.IsAppendedTotal)) {
            var label = ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxis.Labels, step.X)
                ?? (step.IsCheckpoint ? WaterfallValueName(chart, step) : "Item " + Number(step.ItemIndex + 1));
            var point = step.ItemIndex;
            var pattern = point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
            var color = WaterfallColor(series, 0, step, colors); var paint = SvgPaint.Of(color, SemanticMarkPaintRole(series, point));
            entries.Add(new VisualLegendEntry(label, color, WaterfallId(0, step), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                marker: VisualMarkerScene.Legend(chart, series, color, paint, point, pattern), paint: paint, metadata: WaterfallMetadata(chart, 0, step)));
        }
        return entries;
    }
}
