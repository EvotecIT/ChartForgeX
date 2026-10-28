using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    /// <summary>Observes latent native request policy, including zero/omitted captions and a possible native Other caption; no latent mark is painted.</summary>
    private static void ObserveRawRadialValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var noData = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.MutedForeground };
        builder.MeasureText(chart.Options.Labels.NoData, noData); builder.MeasureText("Mg", noData); builder.TextAscent(noData);
        for (var series = 0; series < chart.Series.Count; series++) {
            foreach (var slice in GetSourceSlices(chart, colors, series)) {
                var style = Style(chart, context, series, slice.PointIndex, colors.Foreground, context.Theme.Typography.DataLabelSize);
                Observe(FormatLabel(chart, slice, 0, DisplayValue(chart, slice)), style);
            }
            var aggregate = Style(chart, context, series, -1, colors.Foreground, context.Theme.Typography.DataLabelSize);
            Observe(OtherSliceLabel, aggregate);
            var total = Total(chart, GetSlices(chart, colors, series), series);
            var center = Style(chart, context, series, -1, colors.Foreground, context.Theme.Typography.CenterValueSize, 700);
            Observe(chart.Options.DonutCenterValue ?? (total.FiniteTotal.HasValue ? ChartNumericFormatter.FormatValue(chart.Options, total.FiniteTotal.Value) : FormatOverflowCenter(false)), center);
            Observe(chart.Options.DonutCenterLabel ?? chart.Series[series].Name, aggregate);
        }
        void Observe(string value, TextStyle style) { builder.MeasureText(value, style); builder.MeasureText("Mg", style); builder.TextAscent(style); }
    }
    /// <summary>Retains actual GetSlices membership separately from actual completed painted groups, including a real unpainted zero/radius-zero slice.</summary>
    private static IReadOnlyList<VisualRawValueSlice> CaptureRawRadialSlices(Chart chart, VisualRenderContext context, PreparedVisual drawing) {
        var rows = new List<VisualRawValueSlice>(); var colors = context.Theme.Resolve(context.ThemeMode);
        var painted = drawing.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-point").ToArray();
        for (var series = 0; series < chart.Series.Count; series++) foreach (var slice in GetSlices(chart, colors, series)) {
            var ordinal = series.ToString(CultureInfo.InvariantCulture); var point = slice.PointIndex.ToString(CultureInfo.InvariantCulture); var contributors = string.Join(",", slice.SourcePointIndices);
            var matches = painted.Where(group => group.Metadata.TryGetValue("data-cfx-series", out var a) && a == ordinal
                && group.Metadata.TryGetValue("data-cfx-point", out var b) && b == point
                && group.Metadata.TryGetValue("data-cfx-source-points", out var c) && c == contributors).ToArray();
            if (matches.Length > 1) throw new NotSupportedException("A raw native slice has more than one completed paint owner.");
            var metadata = matches.Length == 0 ? null : new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(matches[0].Metadata.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            rows.Add(new VisualRawValueSlice(chart.Series[series].Id.Value, slice.PointIndex, Array.AsReadOnly((int[])slice.SourcePointIndices.Clone()), metadata));
        }
        if (rows.Count(row => row.PaintedMetadata != null) != painted.Length) throw new NotSupportedException("Raw native value capture has foreign completed slice paint.");
        return Array.AsReadOnly(rows.ToArray());
    }
}
