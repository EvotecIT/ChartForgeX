using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (!series.ShowInLegend) return Array.Empty<VisualLegendEntry>();
        if (series.Kind == ChartSeriesKind.DottedMap && chart.Options.ShowPointLegend)
            return series.MapPoints.Select((point, index) => {
                var (color, paint) = DottedPaint(series, index, colors);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = "0", ["data-cfx-point"] = N(index), ["data-cfx-source-point"] = N(index),
                    ["data-cfx-label"] = point.Label, ["data-cfx-longitude"] = N(point.Longitude), ["data-cfx-latitude"] = N(point.Latitude)
                };
                if (point.Value.HasValue) metadata["data-cfx-value"] = N(point.Value.Value);
                return new VisualLegendEntry(point.Label, color, "series-0-point-" + N(index), series.Kind,
                    stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey, paint: paint, metadata: metadata,
                    marker: (builder, bounds, context) => {
                        var radius = Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2 - .5);
                        var surface = context.Theme.Resolve(context.ThemeMode).Surface;
                        builder.Ellipse(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, radius, radius,
                            color, surface, 1, "legend-swatch", paint: new VisualScenePaintBinding(paint, SvgPaint.Of(surface, SvgColorRole.Surface)));
                    });
            }).ToArray();
        if (series.Kind == ChartSeriesKind.DottedMap) {
            var (color, paint) = DottedPaint(series, null, colors);
            return new[] { new VisualLegendEntry(series.Name, color, "series-0", series.Kind,
                series.FillPattern, series.StateRole, series.InteractionIdentityKey, paint: paint) };
        }
        return new[] { new VisualLegendEntry(series.Name, ChartSeriesColours.Resolve(series, 0, colors), "series-0",
            series.Kind, series.FillPattern, series.StateRole, series.InteractionIdentityKey) };
    }
}
