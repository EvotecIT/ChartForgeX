using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Applies the authored legend keys, entry formatter and custom items to every family's legend entries.</summary>
internal static class VisualLegendControl {
    /// <summary>The metadata a custom item carries so hosts and the browser runtime can tell it from a series toggle.</summary>
    internal const string CustomAttribute = "data-cfx-legend-custom";

    internal static IReadOnlyList<VisualLegendEntry> Apply(Chart chart, IReadOnlyList<VisualLegendEntry> entries) {
        var legend = chart.Options.Legend;
        if (!legend.HasCustomization) return entries;
        var result = new List<VisualLegendEntry>(entries.Count + legend.Items.Count);
        foreach (var entry in entries) {
            var (series, index, point) = Source(chart, entry);
            var label = Format(chart, entry.Label, series, index, point, null, entry.Value);
            var shape = entry.State != null ? ChartLegendKeyShape.Auto : legend.KeyShape;
            var dash = shape == ChartLegendKeyShape.Line && series != null ? VisualSeriesStroke.Style(series)?.Dash?.ToArray() : null;
            result.Add(entry.WithKey(label, shape, entry.State != null ? null : legend.KeySize, dash,
                shape == ChartLegendKeyShape.Auto ? entry.Marker : SeriesKey(shape, series, entry)));
        }
        for (var index = 0; index < legend.Items.Count; index++) {
            var item = legend.Items[index];
            var label = Format(chart, item.Label, null, -1, null, item, null);
            var entry = new VisualLegendEntry(label, item.Color, "custom-item-" + index.ToString(CultureInfo.InvariantCulture),
                paint: SvgPaint.Literal(item.Color), metadata: new Dictionary<string, string> { [CustomAttribute] = "true" });
            result.Add(entry.WithKey(label, item.Key, item.KeySize ?? legend.KeySize, item.DashArray, ItemKey(item)));
        }
        return result;
    }

    private static string Format(Chart chart, string label, ChartSeries? series, int index, int? point, ChartLegendItem? item, string? value) {
        var formatter = chart.Options.Legend.ItemFormatter;
        if (formatter == null) return label;
        return formatter(new ChartLegendItemContext(label, series, index, point, item, value, chart.Options.ValueFormat)) ?? label;
    }

    /// <summary>Series entries are named series-N or series-N-point-M; other family keys resolve through their series identity.</summary>
    private static (ChartSeries? Series, int Index, int? Point) Source(Chart chart, VisualLegendEntry entry) {
        const string prefix = "series-";
        if (entry.Id.StartsWith(prefix, StringComparison.Ordinal)) {
            var end = prefix.Length;
            while (end < entry.Id.Length && char.IsDigit(entry.Id[end])) end++;
            if (end > prefix.Length && int.TryParse(entry.Id.Substring(prefix.Length, end - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index < chart.Series.Count) {
                int? point = null;
                const string pointPrefix = "-point-";
                if (string.CompareOrdinal(entry.Id, end, pointPrefix, 0, pointPrefix.Length) == 0
                    && int.TryParse(entry.Id.Substring(end + pointPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) point = parsed;
                return (chart.Series[index], index, point);
            }
        }
        if (!string.IsNullOrEmpty(entry.SeriesKey))
            for (var index = 0; index < chart.Series.Count; index++)
                if (chart.Series[index].InteractionIdentityKey == entry.SeriesKey) return (chart.Series[index], index, null);
        return (null, -1, null);
    }

    private static Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? SeriesKey(ChartLegendKeyShape shape, ChartSeries? series, VisualLegendEntry entry) {
        var paint = entry.Paint ?? SvgPaint.Literal(entry.Color);
        return shape switch {
            ChartLegendKeyShape.Square => (builder, bounds, context) => {
                builder.Rect(bounds, entry.Color, radius: VisualFrameLayout.LegendSwatchRadius, role: "legend-swatch", paint: VisualChartPaint.Fill(paint));
                VisualFrameLayout.DrawLegendPattern(builder, bounds, entry.Pattern, context);
            },
            ChartLegendKeyShape.Circle => (builder, bounds, _) => {
                var radius = Math.Min(bounds.Width, bounds.Height) * VisualFrameLayout.LegendDotDiameter / VisualFrameLayout.LegendSwatchSize / 2;
                builder.Ellipse(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, radius, radius, entry.Color, role: "legend-swatch", paint: VisualChartPaint.Fill(paint));
            },
            ChartLegendKeyShape.Line => (builder, bounds, context) => {
                var y = bounds.Top + bounds.Height / 2;
                if (series == null) builder.Line(bounds.Left, y, bounds.Right, y, entry.Color, VisualFrameLayout.LegendLineStroke, role: "legend-line", paint: VisualChartPaint.Stroke(paint));
                else VisualSeriesStroke.DrawLine(builder, series, bounds.Left, y, bounds.Right, y, entry.Color,
                    VisualSeriesStroke.Width(series, VisualFrameLayout.LegendLineStroke), "legend-line", VisualChartPaint.Stroke(paint), maximumWidth: bounds.Height / 2);
            },
            _ => (builder, bounds, _) => {
                var radius = Math.Min(bounds.Width, bounds.Height) * .45;
                var x = bounds.Left + bounds.Width / 2; var y = bounds.Top + bounds.Height / 2;
                var path = series != null ? ChartMarkerGeometry.Path(series.Markers, x, y, radius) : ChartMarkerGeometry.Path(ChartMarkerShape.Circle, x, y, radius);
                builder.Path(path, entry.Color, role: "legend-swatch", close: true, paint: VisualChartPaint.Fill(paint));
            }
        };
    }

    private static Action<VisualSceneBuilder, ChartRect, VisualRenderContext> ItemKey(ChartLegendItem item) {
        var paint = SvgPaint.Literal(item.Color);
        return item.Key switch {
            ChartLegendKeyShape.Line => (builder, bounds, _) => {
                var y = bounds.Top + bounds.Height / 2;
                builder.Line(bounds.Left, y, bounds.Right, y, item.Color, Math.Min(item.StrokeWidth, bounds.Height), role: "legend-line",
                    dash: item.DashArray, paint: VisualChartPaint.Stroke(paint));
            },
            ChartLegendKeyShape.Square => (builder, bounds, _) => {
                var fill = ChartColorMath.WithOpacity(item.Color, item.FillOpacity);
                var outline = item.FillOpacity < 1 ? item.StrokeWidth : 0;
                var inset = outline / 2;
                builder.Rect(new ChartRect(bounds.Left + inset, bounds.Top + inset, bounds.Width - outline, bounds.Height - outline), fill,
                    outline > 0 ? item.Color : null, outline, VisualFrameLayout.LegendSwatchRadius, role: "legend-band",
                    paint: new VisualScenePaintBinding(paint.WithOpacity(fill, item.FillOpacity), outline > 0 ? paint : null));
            },
            _ => (builder, bounds, _) => {
                var radius = Math.Min(bounds.Width, bounds.Height) * .45;
                builder.Path(ChartMarkerGeometry.Path(item.MarkerShape, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, radius),
                    item.Color, role: "legend-swatch", close: true, paint: VisualChartPaint.Fill(paint));
            }
        };
    }
}
