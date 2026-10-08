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
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        if (!chart.Series[0].ShowInLegend) return Array.Empty<VisualLegendEntry>();
        var slices = GetSlices(chart, colors); var total = slices.Sum(slice => slice.Value);
        return slices.Select(slice => new VisualLegendEntry(slice.Label, slice.Color, SliceId(slice),
                chart.Series[0].Kind, slice.Pattern, chart.Series[0].StateRole, chart.Series[0].InteractionIdentityKey,
                paint: VisualChartPaint.Series(chart.Series[0], slice.Color, slice.PointIndex),
                value: (total > 0 ? slice.Value / total : 0).ToString("0.#%", CultureInfo.InvariantCulture))).ToArray();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var slices = GetSlices(chart, colors);
        var series = chart.Series[0];
        if (slices.Any(slice => slice.PointIndex < 0 && slice.SourcePointIndices.Any(index => index < series.PointSliceOffsets.Count && series.PointSliceOffsets[index] > 0)))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-offsets", "An aggregate Other slice uses the default position; its source slice offsets remain available in the original data."));
        if (slices.Any(slice => slice.SourcePatterns.Distinct().Count() > 1))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-patterns", "The Other slice combines different source patterns and uses a solid fill; source patterns remain in its metadata."));
        var total = slices.Sum(slice => slice.Value);
        if (double.IsInfinity(total)) throw new ArgumentException("The sum of pie values must be finite.", nameof(chart));
        if (total <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Pie and donut charts need at least one positive value."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return;
        }

        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var outside = placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var vertical = placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        // Only painted source slices contribute to the envelope; Other has no source offset.
        var maximumOffset = slices.Where(slice => slice.Value > 0 && slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count)
            .Select(slice => series.PointSliceOffsets[slice.PointIndex]).DefaultIfEmpty(0).Max();
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
                var formattedValue = ChartNumericFormatter.FormatValue(chart.Options, slice.Value);
                var resolvedLabel = (series.ShowDataLabels ?? chart.Options.ShowDataLabels) ? FormatLabel(chart, slice, total, formattedValue) : null;
                using (builder.PushGroup(null, "radial-point", Metadata(chart, slice, percent, resolvedLabel))) {
                    builder.Slice(sliceX, sliceY, radius, inner, start, sweep, slice.Color, colors.Surface, 2, role, SliceId(slice),
                        paint: new VisualScenePaintBinding(VisualChartPaint.Series(series, slice.Color, slice.PointIndex), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                    if (slice.Pattern != ChartFillPattern.None)
                        builder.PatternSlice(sliceX, sliceY, radius, inner, start, sweep, slice.Pattern,
                            ChartColorMath.AccessibleTextOnBackground(ChartColorMath.Blend(ChartStateMark.Backdrop(chart.Options, colors, context.Frame),
                                ChartColor.FromRgb(slice.Color.R, slice.Color.G, slice.Color.B), slice.Color.A / 255d)).WithAlpha(110), role: "radial-fill-pattern");
                }
                var bounds = new ChartRect(sliceX - radius, sliceY - radius, radius * 2, radius * 2);
                builder.AddRegion(new VisualSemanticRegion(SliceId(slice), role,
                    bounds,
                    slice.Label + ": " + formattedValue));
                if (!string.IsNullOrWhiteSpace(resolvedLabel)) {
                    // The associated slice extent is descriptive, including when its visible label is omitted.
                    builder.AddRegion(new VisualSemanticRegion(SliceId(slice) + "-label", "radial-data-label", bounds, resolvedLabel));
                    AddLabel(chart, context, builder, plot, labels, slice, resolvedLabel!, total, mid, sliceX, sliceY, cx, cy, radius, inner, placement);
                }
                start += sweep;
            }
            if (vertical) DrawVerticalLabels(chart, context, builder, plot, labels, radius, cy, maximumOffset, placement);
            else DrawOutsideLabels(chart, context, builder, plot, labels, radius);
            if (inner > 0 && chart.Options.ShowDonutCenterLabel && series.ShowDataLabels != false)
                DrawCenter(chart, context, builder, cx, cy, inner, total);
        }
    }

    /// <summary>Uses the explicit aggregation budget independently of theme or label placement.</summary>
    private static IReadOnlyList<RadialSlice> GetSlices(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        var slices = series.Points.Select((point, index) => new RadialSlice(index, new[] { index },
            Label(chart, point, index), point.Y, ChartSeriesColours.Point(series, index, index, colors),
            index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern)).ToList();
        var positive = slices.Where(slice => slice.Value > 0).ToList();
        if (positive.Count <= chart.Options.MaximumPieSlices) return slices;
        var retained = positive.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex)
            .Take(chart.Options.MaximumPieSlices - 1).Select(slice => slice.PointIndex).ToArray();
        var rest = positive.Where(slice => !retained.Contains(slice.PointIndex)).ToArray();
        var result = slices.Where(slice => retained.Contains(slice.PointIndex)).ToList();
        var patterns = rest.Select(slice => slice.Pattern).Distinct().ToArray();
        result.Add(new RadialSlice(-1, rest.SelectMany(slice => slice.SourcePointIndices).ToArray(), "Other",
            rest.Sum(slice => slice.Value), series.Color.HasValue || series.StateRole != ChartSeriesState.None
                ? ChartSeriesColours.Resolve(series, 0, colors) : colors.MutedForeground, patterns.Length == 1 ? patterns[0] : ChartFillPattern.None,
            rest.Select(slice => slice.Pattern).ToArray()));
        return result;
    }

    private static string Label(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string SliceId(RadialSlice slice) => slice.PointIndex < 0 ? "series-0-point-other" : "series-0-point-" + slice.PointIndex.ToString(CultureInfo.InvariantCulture);

    private static IReadOnlyDictionary<string, string> Metadata(Chart chart, RadialSlice slice, double percent, string? resolvedLabel) {
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = "0",
            ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && chart.Series[0].StateRole != ChartSeriesState.None ? "true" : "false",
            ["data-cfx-point"] = slice.PointIndex.ToString(CultureInfo.InvariantCulture),
            ["data-cfx-source-points"] = string.Join(",", slice.SourcePointIndices),
            ["data-cfx-label"] = slice.Label,
            ["data-cfx-pattern"] = slice.Pattern.ToString(),
            ["data-cfx-source-patterns"] = string.Join(",", slice.SourcePatterns),
            ["data-cfx-value"] = slice.Value.ToString("G17", CultureInfo.InvariantCulture),
            ["data-cfx-percent"] = percent.ToString("G17", CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(resolvedLabel)) metadata.Add("data-cfx-full-label", resolvedLabel!);
        return metadata;
    }

    private static string FormatLabel(Chart chart, RadialSlice slice, double total, string value) {
        var percent = slice.Value / total;
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
        internal RadialSlice(int pointIndex, int[] sources, string label, double value, ChartColor color, ChartFillPattern pattern, ChartFillPattern[]? sourcePatterns = null) {
            PointIndex = pointIndex; SourcePointIndices = sources; Label = label; Value = value; Color = color; Pattern = pattern;
            SourcePatterns = sourcePatterns ?? new[] { pattern };
        }
        internal int PointIndex { get; }
        internal int[] SourcePointIndices { get; }
        internal string Label { get; }
        internal double Value { get; }
        internal ChartColor Color { get; }
        internal ChartFillPattern Pattern { get; }
        internal ChartFillPattern[] SourcePatterns { get; }
    }
}
