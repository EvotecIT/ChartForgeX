using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Counts authored entries without evaluating point captions or numeric color formatters at the export boundary.</summary>
    internal static int LegendEntryCount(Chart chart) => chart.Series.Where(series => series.ShowInLegend)
        .Sum(series => series.Kind == ChartSeriesKind.DottedMap && chart.Options.ShowPointLegend ? series.MapPoints.Count : 1);

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors, ChartColorPresentationSet? colorScales = null) {
        colorScales ??= new ChartColorPresentationSet(chart);
        return chart.Series.SelectMany((series, index) => DatasetLegendEntries(chart, colors, series, index, colorScales)).ToArray();
    }

    private static IReadOnlyList<VisualLegendEntry> DatasetLegendEntries(Chart chart, VisualThemeColors colors, ChartSeries series, int seriesIndex, ChartColorPresentationSet colorScales) {
        var prefix = "series-" + N(seriesIndex);
        if (!series.ShowInLegend) return Array.Empty<VisualLegendEntry>();
        if (series.Kind == ChartSeriesKind.DottedMap && chart.Options.ShowPointLegend) {
            var data = colorScales.MapDataset(seriesIndex);
            return series.MapPoints.Select((point, index) => {
                var (color, paint) = data.PointPaint(index, colors);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = N(seriesIndex), ["data-cfx-point"] = N(index), ["data-cfx-source-point"] = N(index),
                    ["data-cfx-label"] = point.Label, ["data-cfx-longitude"] = N(point.Longitude), ["data-cfx-latitude"] = N(point.Latitude)
                };
                if (point.Value.HasValue) metadata["data-cfx-value"] = N(point.Value.Value);
                data.AddColorFacts(metadata, index);
                return new VisualLegendEntry(point.Label, color, prefix + "-point-" + N(index), series.Kind,
                    stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey, paint: paint, metadata: metadata,
                    marker: (builder, bounds, context) => {
                        var radius = Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2 - .5);
                        var surface = context.Theme.Resolve(context.ThemeMode).Surface;
                        builder.Ellipse(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, radius, radius,
                            color, surface, 1, "legend-swatch", paint: new VisualScenePaintBinding(paint, SvgPaint.Of(surface, SvgColorRole.Surface)));
                    });
            }).ToArray();
        }
        if (series.Kind == ChartSeriesKind.DottedMap) {
            var data = colorScales.MapDataset(seriesIndex);
            var scale = data.ResolveKeyScale(colors);
            var (color, paint) = scale == null ? DottedPaint(series, null, colors)
                : (scale.ColorFor(data.Maximum, data.Minimum, data.Maximum), SvgPaint.Literal(scale.ColorFor(data.Maximum, data.Minimum, data.Maximum)));
            return new[] { new VisualLegendEntry(series.Name, color, prefix, series.Kind,
                series.FillPattern, series.StateRole, series.InteractionIdentityKey, paint: paint) };
        }
        var regionScale = series.Kind == ChartSeriesKind.RegionMap ? colorScales.MapDataset(seriesIndex).ResolveKeyScale(colors) : null;
        var regionColor = regionScale == null ? ChartSeriesColours.Resolve(series, seriesIndex, colors)
            : regionScale.ColorFor(colorScales.MapDataset(seriesIndex).Maximum, colorScales.MapDataset(seriesIndex).Minimum, colorScales.MapDataset(seriesIndex).Maximum);
        return new[] { new VisualLegendEntry(series.Name, regionColor, prefix,
            series.Kind, series.FillPattern, series.StateRole, series.InteractionIdentityKey) };
    }
}
