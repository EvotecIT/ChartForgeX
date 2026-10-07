using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles pie and donut data directly into the shared numeric scene.</summary>
internal static partial class VisualRadialCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) =>
        chart.Series[0].ShowInLegend
            ? GetSlices(chart, colors).Select(slice => new VisualLegendEntry(slice.Label, slice.Color, SliceId(slice))).ToArray()
            : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        Validate(chart);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var slices = GetSlices(chart, colors);
        var series = chart.Series[0];
        if (slices.Any(slice => slice.PointIndex < 0 && slice.SourcePointIndices.Any(index => index < series.PointSliceOffsets.Count && series.PointSliceOffsets[index] > 0)))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-offsets", "An aggregate Other slice uses the default position; its source slice offsets remain available in the original data."));
        var total = slices.Sum(slice => slice.Value);
        if (double.IsInfinity(total)) throw new ArgumentException("The sum of pie values must be finite.", nameof(chart));
        if (total <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Pie and donut charts need at least one positive value."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center);
            return;
        }

        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var outside = placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var vertical = placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        var maximumOffset = series.PointSliceOffsets.Count == 0 ? 0 : series.PointSliceOffsets.Max();
        var horizontalBudget = outside ? plot.Width * 0.57 : plot.Width;
        var verticalBudget = vertical ? plot.Height * 0.76 : plot.Height;
        var radius = Math.Max(0, (Math.Min(horizontalBudget, verticalBudget) / 2 - context.Theme.Spacing) / (1 + maximumOffset));
        if (radius < 1) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The frame leaves insufficient space for a radial chart."));
            return;
        }

        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * chart.Options.DonutInnerRadiusRatio : 0;
        var start = -Math.PI / 2;
        var labels = new List<RadialLabel>();
        using (builder.PushClip(plot)) {
            foreach (var slice in slices) {
                if (slice.Value <= 0) continue;
                var percent = slice.Value / total;
                var sweep = percent * Math.PI * 2;
                var mid = start + sweep / 2;
                var offset = slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count
                    ? series.PointSliceOffsets[slice.PointIndex] * radius : 0;
                var sliceX = cx + Math.Cos(mid) * offset;
                var sliceY = cy + Math.Sin(mid) * offset;
                var role = inner > 0 ? "donut-slice" : "pie-slice";
                using (builder.PushGroup(null, "radial-point", Metadata(slice, percent))) {
                    builder.Slice(sliceX, sliceY, radius, inner, start, sweep, slice.Color, colors.Surface, 2, role, SliceId(slice));
                }
                builder.AddRegion(new VisualSemanticRegion(SliceId(slice), role,
                    new ChartRect(sliceX - radius, sliceY - radius, radius * 2, radius * 2),
                    slice.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, slice.Value)));
                if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) {
                    AddLabel(chart, context, builder, plot, labels, slice, total, mid, sliceX, sliceY, cx, cy, radius, inner, placement);
                }
                start += sweep;
            }
            DrawOutsideLabels(chart, context, builder, plot, labels, radius);
            if (inner > 0 && chart.Options.ShowDonutCenterLabel && series.ShowDataLabels != false)
                DrawCenter(chart, context, builder, cx, cy, inner, total);
        }
    }

    private static void Validate(Chart chart) {
        var series = chart.Series[0];
        if (series.FillPattern != ChartFillPattern.None || series.PointFillPatterns.Any(pattern => pattern.HasValue && pattern.Value != ChartFillPattern.None))
            throw new NotSupportedException("The Phase 1 prepared radial compiler does not support patterned fills. The legacy export remains available.");
        if (series.StateRole != ChartSeriesState.None)
            throw new NotSupportedException("The Phase 1 prepared radial compiler requires an explicit color for semantic state treatments. The legacy export remains available.");
    }

    /// <summary>Uses the explicit aggregation budget independently of theme or label placement.</summary>
    private static IReadOnlyList<RadialSlice> GetSlices(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        var slices = series.Points.Select((point, index) => new RadialSlice(index, new[] { index },
            Label(chart, point, index), point.Y, index < series.PointColors.Count && series.PointColors[index].HasValue
                ? series.PointColors[index]!.Value : series.Color ?? colors.Palette[index % colors.Palette.Count])).ToList();
        var positive = slices.Where(slice => slice.Value > 0).ToList();
        if (positive.Count <= chart.Options.MaximumPieSlices) return slices;
        var retained = positive.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex)
            .Take(chart.Options.MaximumPieSlices - 1).Select(slice => slice.PointIndex).ToArray();
        var rest = positive.Where(slice => !retained.Contains(slice.PointIndex)).ToArray();
        var result = slices.Where(slice => retained.Contains(slice.PointIndex)).ToList();
        result.Add(new RadialSlice(-1, rest.SelectMany(slice => slice.SourcePointIndices).ToArray(), "Other",
            rest.Sum(slice => slice.Value), colors.MutedForeground));
        return result;
    }

    private static string Label(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string SliceId(RadialSlice slice) => slice.PointIndex < 0 ? "series-0-point-other" : "series-0-point-" + slice.PointIndex.ToString(CultureInfo.InvariantCulture);

    private static IReadOnlyDictionary<string, string> Metadata(RadialSlice slice, double percent) => new Dictionary<string, string> {
        ["data-cfx-series"] = "0",
        ["data-cfx-point"] = slice.PointIndex.ToString(CultureInfo.InvariantCulture),
        ["data-cfx-source-points"] = string.Join(",", slice.SourcePointIndices),
        ["data-cfx-label"] = slice.Label,
        ["data-cfx-value"] = slice.Value.ToString("G17", CultureInfo.InvariantCulture),
        ["data-cfx-percent"] = percent.ToString("G17", CultureInfo.InvariantCulture)
    };

    private static string FormatLabel(Chart chart, RadialSlice slice, double total) {
        var percent = slice.Value / total;
        var value = ChartNumericFormatter.FormatValue(chart.Options, slice.Value);
        var percentage = percent.ToString("0.#%", CultureInfo.InvariantCulture);
        if (chart.Options.PieSliceLabelFormatter != null)
            return chart.Options.PieSliceLabelFormatter(new ChartPieSliceLabelContext(chart.Series[0].Name, slice.Label,
                slice.Value, percent, value, percentage, slice.PointIndex)) ?? string.Empty;
        return chart.Options.PieSliceLabelContent switch {
            ChartPieSliceLabelContent.Value => value,
            ChartPieSliceLabelContent.Label => slice.Label,
            ChartPieSliceLabelContent.LabelAndPercent => slice.Label + " " + percentage,
            ChartPieSliceLabelContent.LabelAndValue => slice.Label + " " + value,
            _ => percentage
        };
    }

    private static TextStyle Style(Chart chart, VisualRenderContext context, int pointIndex, ChartColor color, double size, int weight = 400) {
        var fallback = new TextStyle { Font = context.Font.Clone(), FontSize = size, Color = color };
        fallback.Font.Weight = weight;
        var style = chart.Series[0].DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        if (pointIndex >= 0 && pointIndex < chart.Series[0].PointDataLabelStyles.Count && chart.Series[0].PointDataLabelStyles[pointIndex] != null)
            style = chart.Series[0].PointDataLabelStyles[pointIndex]!.Resolve(style);
        return style;
    }

    private sealed class RadialSlice {
        internal RadialSlice(int pointIndex, int[] sources, string label, double value, ChartColor color) {
            PointIndex = pointIndex; SourcePointIndices = sources; Label = label; Value = value; Color = color;
        }
        internal int PointIndex { get; }
        internal int[] SourcePointIndices { get; }
        internal string Label { get; }
        internal double Value { get; }
        internal ChartColor Color { get; }
    }
}
