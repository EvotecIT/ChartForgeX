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
    private static readonly string OverflowValueText = "> " + double.MaxValue.ToString("G17", CultureInfo.InvariantCulture);

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var entries = new List<VisualLegendEntry>();
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (!series.ShowInLegend) continue;
            var slices = GetSlices(chart, colors, seriesIndex); var total = Total(chart, slices, seriesIndex);
            entries.AddRange(slices.Select(slice => new VisualLegendEntry(chart.Series.Count > 1 ? series.Name + ": " + slice.Label : slice.Label,
                slice.Color, SliceId(slice), series.Kind, slice.Pattern, series.StateRole, series.InteractionIdentityKey,
                paint: VisualChartPaint.Series(series, slice.Color, slice.PointIndex), value: DisplayValue(chart, slice),
                percentage: Fraction(chart, slice, total).ToString("0.#%", CultureInfo.InvariantCulture),
                metadata: Metadata(chart, slice, Fraction(chart, slice, total), null, !total.FiniteTotal.HasValue))));
        }
        return entries;
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var centerSeries = Enumerable.Range(0, chart.Series.Count).OrderBy(index => chart.Series[index].RadialBand?.InnerRatio
            ?? (chart.Series[index].Kind == ChartSeriesKind.Donut ? chart.Options.DonutInnerRadiusRatio : 0)).First();
        var outerSeries = Enumerable.Range(0, chart.Series.Count).OrderByDescending(index => chart.Series[index].RadialBand?.OuterRatio ?? 1).First();
        var outerPlacement = chart.Series[outerSeries].DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        for (var index = 0; index < chart.Series.Count; index++)
            BuildSeries(chart, context, builder, plot, index, index == centerSeries, outerPlacement);
    }

    private static void BuildSeries(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        int seriesIndex, bool ownsCenter, ChartDataLabelPlacement outerPlacement) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var slices = GetSlices(chart, colors, seriesIndex);
        var series = chart.Series[seriesIndex];
        if (slices.Any(slice => slice.PointIndex < 0 && slice.SourcePointIndices.Any(index => index < series.PointSliceOffsets.Count && series.PointSliceOffsets[index] > 0)))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-offsets", "An aggregate Other slice uses the default position; its source slice offsets remain available in the original data."));
        if (slices.Any(slice => slice.SourcePatterns.Distinct().Count() > 1))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-patterns", "The Other slice combines different source patterns and uses a solid fill; source patterns remain in its metadata."));
        var total = Total(chart, slices, seriesIndex);
        if (!total.FiniteTotal.HasValue)
            builder.AddDiagnostic(new VisualDiagnostic("radial.total-overflow", "The total exceeds the finite numeric range. Proportions and authored source values remain available; the default total caption shows a lower bound."));
        if (slices.Any(slice => !ChartMath.IsFinite(slice.Value)))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-overflow", "The Other aggregate exceeds the finite numeric range. Its numeric aggregate is omitted, its contributors remain exact, and its value and slice formatters are skipped in favor of a lower-bound readout."));
        if (!total.HasPositiveValues) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Pie and donut charts need at least one positive value."));
            if (chart.Series.Count > 1) return; // Retain an empty ring's facts without covering other rings with centered text.
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return;
        }

        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var framePlacement = chart.Series.Count > 1 ? outerPlacement : placement;
        var outside = framePlacement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var vertical = placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        var frameVertical = framePlacement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        // Only painted source slices contribute to the envelope; Other has no source offset.
        var maximumOffset = slices.Where(slice => slice.Value > 0 && slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count)
            .Select(slice => series.PointSliceOffsets[slice.PointIndex]).DefaultIfEmpty(0).Max();
        var horizontalBudget = outside ? plot.Width * 0.57 : plot.Width;
        var verticalBudget = frameVertical ? plot.Height * 0.76 : plot.Height;
        var radius = Math.Max(0, (Math.Min(horizontalBudget, verticalBudget) / 2 - context.Theme.Spacing) / (1 + maximumOffset));
        if (radius < 1) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The frame leaves insufficient space for a radial chart."));
            return;
        }

        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * chart.Options.DonutInnerRadiusRatio : 0;
        if (series.RadialBand is { } band) { inner = radius * band.InnerRatio; radius *= band.OuterRatio; }
        if (series.RadialBand != null && radius - inner < 1) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.band-insufficient-space", "The explicit radial band cannot fit a one-unit painted thickness in this frame."));
            return;
        }
        var start = -Math.PI / 2;
        var labels = new List<RadialLabel>();
        using (builder.PushClip(plot)) {
            foreach (var slice in slices) {
                if (slice.Value <= 0) continue;
                var percent = Fraction(chart, slice, total);
                var sweep = percent * Math.PI * 2;
                var mid = start + sweep / 2;
                var offset = slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count
                    ? series.PointSliceOffsets[slice.PointIndex] * radius : 0;
                var sliceX = cx + Math.Cos(mid) * offset;
                var sliceY = cy + Math.Sin(mid) * offset;
                var role = inner > 0 ? "donut-slice" : "pie-slice";
                var formattedValue = DisplayValue(chart, slice);
                var resolvedLabel = (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false)) ? FormatLabel(chart, slice, percent, formattedValue) : null;
                using (builder.PushGroup(null, "radial-point", Metadata(chart, slice, percent, resolvedLabel, !total.FiniteTotal.HasValue))) {
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
                    AddLabel(chart, context, builder, plot, labels, slice, resolvedLabel!, percent, mid, sliceX, sliceY, cx, cy, radius, inner, placement);
                }
                start += sweep;
            }
            if (vertical) DrawVerticalLabels(chart, context, builder, plot, labels, radius, cy, maximumOffset, placement);
            else DrawOutsideLabels(chart, context, builder, plot, labels, radius);
            if (ownsCenter && inner > 0 && chart.Options.ShowDonutCenterLabel && series.ResolveDataLabels(chart.Options, defaultVisible: true))
                DrawCenter(chart, context, builder, cx, cy, inner, total.FiniteTotal, seriesIndex);
        }
    }

    private static ChartPositiveSum Total(Chart chart, IReadOnlyList<RadialSlice> slices, int seriesIndex) =>
        // Retain the existing sorted/aggregated summation order for ordinary finite output.
        new(slices.All(slice => ChartMath.IsFinite(slice.Value)) ? slices.Select(slice => slice.Value)
            : chart.Series[seriesIndex].RenderPoints.Select(point => point.Y));

    private static double Fraction(Chart chart, RadialSlice slice, ChartPositiveSum total) => ChartMath.IsFinite(slice.Value)
        ? total.Fraction(slice.Value)
        : slice.SourcePointIndices.Sum(index => total.Fraction(chart.Series[slice.SeriesIndex].RenderPoints[index].Y));

    private static string DisplayValue(Chart chart, RadialSlice slice) => ChartMath.IsFinite(slice.Value)
        ? ChartNumericFormatter.FormatValue(chart.Options, slice.Value) : OverflowValueText;

    /// <summary>Uses the explicit aggregation budget independently of theme or label placement.</summary>
    private static IReadOnlyList<RadialSlice> GetSlices(Chart chart, VisualThemeColors colors, int seriesIndex) {
        var series = chart.Series[seriesIndex];
        var slices = series.RenderPoints.Select((point, index) => new RadialSlice(seriesIndex, index, new[] { index },
            Label(chart, point, index), point.Y, ChartSeriesColours.CategoryPoint(series, index, colors),
            index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern)).ToList();
        var positive = slices.Where(slice => slice.Value > 0).ToList();
        if (positive.Count <= chart.Options.MaximumPieSlices)
            return slices.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex).ToArray();
        var retained = positive.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex)
            .Take(chart.Options.MaximumPieSlices - 1).Select(slice => slice.PointIndex).ToArray();
        var rest = positive.Where(slice => !retained.Contains(slice.PointIndex)).ToArray();
        var result = slices.Where(slice => retained.Contains(slice.PointIndex)).OrderByDescending(slice => slice.Value)
            .ThenBy(slice => slice.PointIndex).ToList();
        var patterns = rest.Select(slice => slice.Pattern).Distinct().ToArray();
        result.Add(new RadialSlice(seriesIndex, -1, rest.SelectMany(slice => slice.SourcePointIndices).ToArray(), "Other",
            rest.Sum(slice => slice.Value), series.Color.HasValue || series.StateRole != ChartSeriesState.None
                ? ChartSeriesColours.Resolve(series, seriesIndex, colors) : colors.Neutral, patterns.Length == 1 ? patterns[0] : ChartFillPattern.None,
            rest.Select(slice => slice.Pattern).ToArray()));
        return result;
    }

    private static string Label(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string SliceId(RadialSlice slice) => "series-" + slice.SeriesIndex.ToString(CultureInfo.InvariantCulture)
        + (slice.PointIndex < 0 ? "-point-other" : "-point-" + slice.PointIndex.ToString(CultureInfo.InvariantCulture));

    private static IReadOnlyDictionary<string, string> Metadata(Chart chart, RadialSlice slice, double percent, string? resolvedLabel, bool totalOverflow) {
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = slice.SeriesIndex.ToString(CultureInfo.InvariantCulture),
            ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && chart.Series[slice.SeriesIndex].StateRole != ChartSeriesState.None ? "true" : "false",
            ["data-cfx-point"] = slice.PointIndex.ToString(CultureInfo.InvariantCulture),
            ["data-cfx-derived"] = "radial-slice",
            ["data-cfx-source-points"] = string.Join(",", slice.SourcePointIndices),
            ["data-cfx-label"] = slice.Label,
            ["data-cfx-pattern"] = slice.Pattern.ToString(),
            ["data-cfx-source-patterns"] = string.Join(",", slice.SourcePatterns)
        };
        if (chart.Series[slice.SeriesIndex].RadialBand is { } band) {
            metadata["data-cfx-radial-inner-ratio"] = band.InnerRatio.ToString("R", CultureInfo.InvariantCulture);
            metadata["data-cfx-radial-outer-ratio"] = band.OuterRatio.ToString("R", CultureInfo.InvariantCulture);
        }
        if (ChartMath.IsFinite(slice.Value)) {
            metadata.Add("data-cfx-value", slice.Value.ToString("G17", CultureInfo.InvariantCulture));
        } else {
            metadata.Add("data-cfx-value-overflow", "true");
            metadata.Add("data-cfx-formatted-value", OverflowValueText);
            metadata.Add("data-cfx-source-values", string.Join(",", slice.SourcePointIndices.Select(index => chart.Series[slice.SeriesIndex].RenderPoints[index].Y.ToString("R", CultureInfo.InvariantCulture))));
        }
        metadata.Add("data-cfx-percent", percent.ToString("G17", CultureInfo.InvariantCulture));
        if (totalOverflow) metadata.Add("data-cfx-total-overflow", "true");
        if (!string.IsNullOrWhiteSpace(resolvedLabel)) metadata.Add("data-cfx-full-label", resolvedLabel!);
        return metadata;
    }

    private static string FormatLabel(Chart chart, RadialSlice slice, double percent, string value) {
        var percentage = percent.ToString("0.#%", CultureInfo.InvariantCulture);
        if (chart.Options.PieSliceLabelFormatter != null && ChartMath.IsFinite(slice.Value))
            return chart.Options.PieSliceLabelFormatter(new ChartPieSliceLabelContext(chart.Series[slice.SeriesIndex].Name, slice.Label,
                slice.Value, percent, value, percentage, slice.PointIndex)) ?? string.Empty;
        return chart.Options.PieSliceLabelContent switch {
            ChartPieSliceLabelContent.Value => value,
            ChartPieSliceLabelContent.Label => slice.Label,
            ChartPieSliceLabelContent.LabelAndPercent => slice.Label + " " + percentage,
            ChartPieSliceLabelContent.LabelAndValue => slice.Label + " " + value,
            _ => percentage
        };
    }

    private static TextStyle Style(Chart chart, VisualRenderContext context, int seriesIndex, int pointIndex, ChartColor color, double size, int weight = 400) {
        var fallback = new TextStyle { Font = context.Font.Clone(), FontSize = size, Color = color };
        fallback.Font.Weight = weight;
        var style = chart.Series[seriesIndex].DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        if (pointIndex >= 0 && pointIndex < chart.Series[seriesIndex].PointDataLabelStyles.Count && chart.Series[seriesIndex].PointDataLabelStyles[pointIndex] != null)
            style = chart.Series[seriesIndex].PointDataLabelStyles[pointIndex]!.Resolve(style);
        return style;
    }

    private sealed class RadialSlice {
        internal RadialSlice(int seriesIndex, int pointIndex, int[] sources, string label, double value, ChartColor color, ChartFillPattern pattern, ChartFillPattern[]? sourcePatterns = null) {
            SeriesIndex = seriesIndex; PointIndex = pointIndex; SourcePointIndices = sources; Label = label; Value = value; Color = color; Pattern = pattern;
            SourcePatterns = sourcePatterns ?? new[] { pattern };
        }
        internal int SeriesIndex { get; }
        internal int PointIndex { get; }
        internal int[] SourcePointIndices { get; }
        internal string Label { get; }
        internal double Value { get; }
        internal ChartColor Color { get; }
        internal ChartFillPattern Pattern { get; }
        internal ChartFillPattern[] SourcePatterns { get; }
    }
}
