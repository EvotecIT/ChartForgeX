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
    private const string OtherSliceLabel = "Other";
    private static readonly string OverflowValueText = "> " + double.MaxValue.ToString("G17", CultureInfo.InvariantCulture);

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var entries = new List<VisualLegendEntry>();
        var bands = ChartRadialBandResolver.Resolve(chart);
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (!series.ShowInLegend) continue;
            var slices = GetSlices(chart, colors, seriesIndex); var total = Total(chart, slices, seriesIndex);
            entries.AddRange(slices.Select(slice => new VisualLegendEntry(chart.Series.Count > 1 ? series.Name + ": " + slice.Label : slice.Label,
                slice.Color, SliceId(slice), series.Kind, slice.Pattern, series.StateRole, series.InteractionIdentityKey,
                paint: SlicePaint(chart, slice),
                value: chart.Options.PieLegendContent == ChartPieLegendContent.Label ? null : DisplayValue(chart, slice),
                percentage: chart.Options.PieLegendContent.HasValue ? null : FormatPercentage(Fraction(chart, slice, total)),
                metadata: Metadata(chart, slice, Fraction(chart, slice, total), null, !total.FiniteTotal.HasValue, bands[seriesIndex]))));
        }
        return entries;
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var bands = ChartRadialBandResolver.Resolve(chart);
        var centerSeries = Enumerable.Range(0, chart.Series.Count).OrderBy(index => bands[index]?.InnerRatio
            ?? (chart.Series[index].Kind == ChartSeriesKind.Donut ? chart.Options.DonutInnerRadiusRatio : 0)).First();
        var outerSeries = Enumerable.Range(0, chart.Series.Count).OrderByDescending(index => bands[index]?.OuterRatio ?? 1).First();
        var outerPlacement = chart.Series[outerSeries].DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var frame = ResolveBandFrame(chart, context, plot, bands, outerSeries);
        for (var index = 0; index < chart.Series.Count; index++)
            BuildSeries(chart, context, builder, plot, index, bands[index], index == centerSeries, outerPlacement, frame);
        if (frame != null) using (builder.PushClip(plot)) DrawBandLabels(chart, context, builder, plot, frame);
    }

    private static void BuildSeries(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        int seriesIndex, ChartRadialBand? band, bool ownsCenter, ChartDataLabelPlacement outerPlacement, RadialBandFrame? frame) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var slices = GetSlices(chart, colors, seriesIndex);
        var series = chart.Series[seriesIndex];
        if (slices.Any(slice => slice.PointIndex < 0 && slice.NativePointIndices.Any(index => index < series.PointSliceOffsets.Count && series.PointSliceOffsets[index] > 0)))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-offsets", "An aggregate Other slice uses the default position; its source slice offsets remain available in the original data."));
        if (slices.Any(slice => slice.SourcePatterns.Distinct().Count() > 1))
            builder.AddDiagnostic(new VisualDiagnostic("radial.aggregate-patterns", "The Other slice combines different source patterns and uses a solid fill; source patterns remain in its metadata."));
        var automatic = series.Color.HasValue || series.StateRole != ChartSeriesState.None ? 0
            : slices.Count(slice => slice.PointIndex >= 0 && !ChartMarkSurface.SliceHasPointColor(series, slice.PointIndex));
        if (automatic > colors.Palette.Count) builder.AddDiagnostic(VisualChartCompiler.PaletteReused(automatic, colors.Palette.Count, "slices"));
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
        // Positive-radius source slices contribute to the envelope; Other has no source offset.
        var maximumOffset = slices.Where(slice => slice.Value > 0 && slice.RadiusRatio.GetValueOrDefault(1) > 0 && slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count)
            .Select(slice => series.PointSliceOffsets[slice.PointIndex]).DefaultIfEmpty(0).Max();
        var horizontalBudget = outside ? plot.Width * 0.57 : plot.Width;
        var verticalBudget = frameVertical ? plot.Height * 0.76 : plot.Height;
        var radius = frame?.Radius ?? Math.Max(0, (Math.Min(horizontalBudget, verticalBudget) / 2 - context.Theme.Spacing) / (1 + maximumOffset));
        if (radius < 1) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The frame leaves insufficient space for a radial chart."));
            return;
        }

        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * chart.Options.DonutInnerRadiusRatio : 0;
        if (band != null) { inner = radius * band.InnerRatio; radius *= band.OuterRatio; }
        if (band != null && radius - inner < 1) {
            builder.AddDiagnostic(new VisualDiagnostic("radial.band-insufficient-space", "The " + (chart.Options.RadialBandAllocation == null ? "explicit" : "automatic") + " radial band cannot fit a one-unit painted thickness in this frame."));
            return;
        }
        var start = -Math.PI / 2;
        var labels = frame?.Labels ?? new List<RadialLabel>();
        using (builder.PushClip(plot)) {
            foreach (var slice in slices) {
                if (slice.Value <= 0) continue;
                var percent = Fraction(chart, slice, total);
                var sweep = percent * Math.PI * 2;
                var mid = start + sweep / 2;
                var ratio = slice.RadiusRatio.GetValueOrDefault(1);
                // Preserve exact default arithmetic; a full-radius override is geometrically identical.
                var sliceRadius = ratio == 1 ? radius : inner + (radius - inner) * ratio;
                var painted = ratio > 0 && (ratio == 1 || sliceRadius - inner >= 1);
                if (ratio > 0 && !painted) builder.AddDiagnostic(new VisualDiagnostic("radial.slice-insufficient-space",
                    "Series '" + series.Id.Value + "', source points " + string.Join(",", slice.SourcePointIndices)
                    + ": its radius leaves less than one painted unit of band thickness; source facts remain available."));
                var offset = slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count
                    ? series.PointSliceOffsets[slice.PointIndex] * sliceRadius : 0;
                var sliceX = cx + Math.Cos(mid) * offset;
                var sliceY = cy + Math.Sin(mid) * offset;
                var role = inner > 0 ? "donut-slice" : "pie-slice";
                var formattedValue = DisplayValue(chart, slice);
                var resolvedLabel = (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false)) ? FormatLabel(chart, slice, percent, formattedValue) : null;
                if (painted) using (builder.PushGroup(null, "radial-point", Metadata(chart, slice, percent, resolvedLabel, !total.FiniteTotal.HasValue, band))) {
                    PaintSlice(chart, builder, series, slice, sliceX, sliceY, sliceRadius, inner, start, sweep, role, colors);
                    if (slice.Pattern != ChartFillPattern.None)
                        builder.PatternSlice(sliceX, sliceY, sliceRadius, inner, start, sweep, slice.Pattern,
                            SlicePaintColor(chart, ChartColorMath.AccessibleTextOnBackground(ChartColorMath.Blend(ChartStateMark.Backdrop(chart.Options, colors, context.Frame),
                                ChartColor.FromRgb(slice.Color.R, slice.Color.G, slice.Color.B), slice.Color.A / 255d)).WithAlpha(110)), role: "radial-fill-pattern");
                }
                var bounds = painted ? new ChartRect(sliceX - sliceRadius, sliceY - sliceRadius, sliceRadius * 2, sliceRadius * 2)
                    : new ChartRect(sliceX, sliceY, 0, 0);
                builder.AddRegion(new VisualSemanticRegion(SliceId(slice), role,
                    bounds,
                    slice.Label + ": " + formattedValue));
                if (!string.IsNullOrWhiteSpace(resolvedLabel)) {
                    // The associated slice extent is descriptive, including when its visible label is omitted.
                    builder.AddRegion(new VisualSemanticRegion(SliceId(slice) + "-label", "radial-data-label", bounds, resolvedLabel));
                    if (painted) AddLabel(chart, context, builder, plot, labels, slice, resolvedLabel!, percent, mid, sliceX, sliceY, cx, cy, sliceRadius, inner, frame?.Extent ?? radius, placement);
                }
                start += sweep;
            }
            PaintOuterOutline(chart, builder, cx, cy, radius, colors);
            if (frame == null) {
                if (vertical) DrawVerticalLabels(chart, context, builder, plot, labels, radius, cy, maximumOffset, placement);
                else DrawOutsideLabels(chart, context, builder, plot, labels, radius);
            }
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
        : slice.NativePointIndices.Sum(index => total.Fraction(chart.Series[slice.SeriesIndex].RenderPoints[index].Y));

    private static string DisplayValue(Chart chart, RadialSlice slice) => ChartMath.IsFinite(slice.Value)
        ? ChartNumericFormatter.FormatValue(chart.Options, slice.Value) : OverflowValueText;

    /// <summary>Uses the explicit aggregation budget independently of theme or label placement.</summary>
    private static IReadOnlyList<RadialSlice> GetSlices(Chart chart, VisualThemeColors colors, int seriesIndex) {
        var series = chart.Series[seriesIndex];
        var slices = GetSourceSlices(chart, colors, seriesIndex);
        var positive = slices.Where(slice => slice.Value > 0).ToList();
        if (positive.Count <= chart.Options.MaximumPieSlices)
            return chart.Options.PiePreserveSourceOrder ? slices.ToArray()
                : slices.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex).ToArray();
        var retained = positive.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex)
            .Take(chart.Options.MaximumPieSlices - 1).Select(slice => slice.PointIndex).ToArray();
        var rest = positive.Where(slice => !retained.Contains(slice.PointIndex)).ToArray();
        var retainedSlices = slices.Where(slice => retained.Contains(slice.PointIndex));
        var result = (chart.Options.PiePreserveSourceOrder ? retainedSlices.OrderBy(slice => slice.PointIndex)
            : retainedSlices.OrderByDescending(slice => slice.Value).ThenBy(slice => slice.PointIndex)).ToList();
        var patterns = rest.Select(slice => slice.Pattern).Distinct().ToArray();
        // Other is the neutral aggregate (never a categorical hue) and is always last, also with source order.
        var seriesPaint = series.Color.HasValue || series.StateRole != ChartSeriesState.None;
        result.Add(new RadialSlice(seriesIndex, -1, rest.SelectMany(slice => slice.NativePointIndices).ToArray(), OtherSliceLabel,
            rest.Sum(slice => slice.Value), seriesPaint ? ChartSeriesColours.Resolve(series, seriesIndex, colors) : colors.Other, patterns.Length == 1 ? patterns[0] : ChartFillPattern.None,
            rest.Select(slice => slice.Pattern).ToArray(), AggregateRadius(series, rest),
            series.IsVisibilityPreparation ? rest.SelectMany(slice => slice.SourcePointIndices).ToArray() : null) { Paint = seriesPaint ? null : colors.OtherPaint });
        return result;
    }

    private static List<RadialSlice> GetSourceSlices(Chart chart, VisualThemeColors colors, int seriesIndex) {
        var series = chart.Series[seriesIndex];
        return series.RenderPoints.Select((point, index) => new RadialSlice(seriesIndex, index, new[] { index },
            Label(chart, point, series.IsVisibilityPreparation && series.SourcePointIndices != null ? series.SourcePointIndices[index] : index), point.Y, ChartSeriesColours.CategoryPoint(series, index, colors),
            index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern, radiusRatio: series.SliceRadiusRatio(index), sourceOrdinals: series.IsVisibilityPreparation && series.SourcePointIndices != null ? new[] { series.SourcePointIndices[index] } : null)).ToList();
    }

    private static double? AggregateRadius(ChartSeries series, IReadOnlyList<RadialSlice> contributors) {
        var ratio = contributors[0].RadiusRatio.GetValueOrDefault(1);
        foreach (var slice in contributors) {
            if (Math.Abs(slice.RadiusRatio.GetValueOrDefault(1) - ratio) <= 1e-12) continue;
            throw new InvalidOperationException("radial.aggregate-radius: Series '" + series.Id.Value
                + "' cannot aggregate Other with differing source radius policies (points "
                + string.Join(",", contributors.Select(item => item.PointIndex))
                + "). Increase MaximumPieSlices or use identical contributor radius ratios.");
        }
        return contributors.Any(slice => slice.RadiusRatio.HasValue) ? ratio : (double?)null;
    }

    private static string Label(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string SliceId(RadialSlice slice) => "series-" + slice.SeriesIndex.ToString(CultureInfo.InvariantCulture)
        + (slice.PointIndex < 0 ? "-point-other" : "-point-" + slice.PointIndex.ToString(CultureInfo.InvariantCulture));

    private static IReadOnlyDictionary<string, string> Metadata(Chart chart, RadialSlice slice, double percent, string? resolvedLabel, bool totalOverflow, ChartRadialBand? band) {
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
        if (chart.Series[slice.SeriesIndex].IsVisibilityPreparation && slice.PointIndex >= 0 && slice.SourcePointIndices[0] != slice.PointIndex)
            metadata["data-cfx-observation-index"] = slice.SourcePointIndices[0].ToString(CultureInfo.InvariantCulture);
        if (band != null) {
            metadata["data-cfx-radial-inner-ratio"] = band.InnerRatio.ToString("R", CultureInfo.InvariantCulture);
            metadata["data-cfx-radial-outer-ratio"] = band.OuterRatio.ToString("R", CultureInfo.InvariantCulture);
        }
        if (chart.Options.RadialBandAllocation != null) metadata["data-cfx-radial-band-source"] = "automatic";
        var source = chart.Series[slice.SeriesIndex];
        if (chart.Series.Count > 1 && slice.PointIndex >= 0 && slice.PointIndex < source.PointSliceOffsets.Count && source.PointSliceOffsets[slice.PointIndex] > 0)
            metadata["data-cfx-slice-offset-ratio"] = source.PointSliceOffsets[slice.PointIndex].ToString("R", CultureInfo.InvariantCulture);
        if (slice.RadiusRatio.HasValue) metadata["data-cfx-slice-radius-ratio"] = slice.RadiusRatio.Value.ToString("R", CultureInfo.InvariantCulture);
        if (ChartMath.IsFinite(slice.Value)) {
            metadata.Add("data-cfx-value", slice.Value.ToString("G17", CultureInfo.InvariantCulture));
        } else {
            metadata.Add("data-cfx-value-overflow", "true");
            metadata.Add("data-cfx-formatted-value", OverflowValueText);
            metadata.Add("data-cfx-source-values", string.Join(",", slice.NativePointIndices.Select(index => chart.Series[slice.SeriesIndex].RenderPoints[index].Y.ToString("R", CultureInfo.InvariantCulture))));
        }
        metadata.Add("data-cfx-percent", percent.ToString("G17", CultureInfo.InvariantCulture));
        if (totalOverflow) metadata.Add("data-cfx-total-overflow", "true");
        if (!string.IsNullOrWhiteSpace(resolvedLabel)) metadata.Add("data-cfx-full-label", resolvedLabel!);
        return metadata;
    }

    private static string FormatLabel(Chart chart, RadialSlice slice, double percent, string value) {
        var percentage = FormatPercentage(percent);
        if (chart.Options.PieSliceLabelFormatter != null && ChartMath.IsFinite(slice.Value))
            return chart.Options.PieSliceLabelFormatter(new ChartPieSliceLabelContext(chart.Series[slice.SeriesIndex].Name, slice.Label,
                slice.Value, percent, value, percentage, slice.PointIndex)) ?? string.Empty;
        return FormatLabelContent(chart.Options.PieSliceLabelContent, slice.Label, percentage, value);
    }
    private static string FormatLabelContent(ChartPieSliceLabelContent content, string label, string percentage, string value) => content switch {
        ChartPieSliceLabelContent.Value => value, ChartPieSliceLabelContent.Label => label,
        ChartPieSliceLabelContent.LabelAndPercent => label + " " + percentage,
        ChartPieSliceLabelContent.LabelAndValue => label + " " + value, _ => percentage
    };

    private static TextStyle Style(Chart chart, VisualRenderContext context, int seriesIndex, int pointIndex, ChartColor color, double size, int weight = 400) {
        var fallback = new TextStyle { Font = context.Font.Clone(), FontSize = size, Color = color };
        fallback.Font.Weight = weight;
        var style = chart.Series[seriesIndex].DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        if (pointIndex >= 0 && pointIndex < chart.Series[seriesIndex].PointDataLabelStyles.Count && chart.Series[seriesIndex].PointDataLabelStyles[pointIndex] != null)
            style = chart.Series[seriesIndex].PointDataLabelStyles[pointIndex]!.Resolve(style);
        return style;
    }

    private sealed class RadialSlice {
        internal RadialSlice(int seriesIndex, int pointIndex, int[] sources, string label, double value, ChartColor color, ChartFillPattern pattern, ChartFillPattern[]? sourcePatterns = null, double? radiusRatio = null, int[]? sourceOrdinals = null) {
            SeriesIndex = seriesIndex; PointIndex = pointIndex; NativePointIndices = sources; SourcePointIndices = sourceOrdinals ?? sources; Label = label; Value = value; Color = color; Pattern = pattern;
            SourcePatterns = sourcePatterns ?? new[] { pattern }; RadiusRatio = radiusRatio;
        }
        internal int SeriesIndex { get; }
        internal int PointIndex { get; }
        internal int[] SourcePointIndices { get; }
        internal int[] NativePointIndices { get; }
        internal string Label { get; }
        internal double Value { get; }
        internal double? RadiusRatio { get; }
        internal ChartColor Color { get; }
        internal ChartFillPattern Pattern { get; }
        internal ChartFillPattern[] SourcePatterns { get; }
        /// <summary>The paint of the neutral Other aggregate; null paints <see cref="Color"/> by its series role.</summary>
        internal SvgPaint? Paint { get; set; }
    }

    private static SvgPaint SlicePaint(Chart chart, RadialSlice slice) =>
        slice.Paint ?? VisualChartPaint.Series(chart.Series[slice.SeriesIndex], slice.Color, slice.PointIndex);
}
