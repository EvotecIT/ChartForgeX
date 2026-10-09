using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawPoints(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartMapper map,
        ChartStackLayout stacks, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        var points = new List<ChartPoint>(series.Points.Count);
        var lower = new List<ChartPoint>(series.Points.Count);
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            var stack = stacks.Point(index, pointIndex);
            points.Add(new ChartPoint(map.X(point.X), map.Y(stack.End), point.BreakBefore));
            lower.Add(new ChartPoint(map.X(point.X), series.Kind == ChartSeriesKind.StackedArea ? map.YOrBaseline(stack.Base) : map.YBaseline(), point.BreakBefore));
        }
        var color = Color(series, index, colors);
        var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
        if (series.Kind != ChartSeriesKind.Scatter && points.Count > 0) {
            using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null) {
                if (series.Kind == ChartSeriesKind.Area || series.Kind == ChartSeriesKind.StepArea || series.Kind == ChartSeriesKind.StackedArea) {
                    // Each disconnected segment closes independently; smoothing uses the existing path algorithm.
                    var offset = 0;
                    foreach (var segment in ChartPointSegments.Split(points)) {
                        var baseline = lower.GetRange(offset, segment.Count);
                        var upperPath = ChartPathBuilder.FromPoints(segment, series.Interpolation, series.StepPosition).Flatten(12);
                        var lowerPath = ChartPathBuilder.FromPoints(baseline, series.Interpolation, series.StepPosition).Flatten(12);
                        var commands = new List<ChartPathCommand>();
                        if (upperPath.Count > 0) {
                            commands.Add(ChartPathCommand.MoveTo(upperPath[0].X, upperPath[0].Y));
                            for (var point = 1; point < upperPath.Count; point++) commands.Add(ChartPathCommand.LineTo(upperPath[point].X, upperPath[point].Y));
                            for (var point = lowerPath.Count - 1; point >= 0; point--) commands.Add(ChartPathCommand.LineTo(lowerPath[point].X, lowerPath[point].Y));
                            var area = new ChartPath(commands);
                            var fill = ChartColorMath.WithOpacity(color, context.Theme.AreaOpacity);
                            builder.Path(area, fill, role: "area", close: true, paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color).WithOpacity(fill, context.Theme.AreaOpacity)));
                            DrawPattern(builder, area, series.FillPattern, fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "area-pattern");
                        }
                        offset += segment.Count;
                    }
                }
                var linePath = ChartPathBuilder.FromPoints(points, series.Interpolation, series.StepPosition);
                foreach (var layer in ChartLineVisualLayers.Build(color, stroke, chart.Options.ResolvePreparedLineVisualStyle()))
                    if (layer.IsVisible) builder.Path(linePath, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth, role: "line" + layer.RoleSuffix,
                        paint: VisualChartPaint.Stroke(VisualChartPaint.LineLayer(series, color, layer)));
                if (chart.Series.Any(item => item.ShowDataLabels ?? chart.Options.ShowDataLabels)
                    || chart.Annotations.Any(annotation => annotation.ShowLabel && annotation.Label.Length > 0)) {
                    var contours = ChartPointSegments.Split(linePath.Flatten(12)).Select(segment => segment.ToList()).ToArray();
                    obstacles.Add(new LabelObstacle(SeriesId(index) + "-line", new LabelMarkShape(contours, false, stroke, chart.Options.ClipMarksToPlot ? plot : null)));
                }
            }
        }
        var radius = ResolveMarkerRadius(series, context);
        var labelStyle = SeriesLabelStyle(chart, context, series, colors);
        for (var pointIndex = 0; pointIndex < points.Count; pointIndex++) {
            var point = points[pointIndex];
            var bounds = VisualMarkerScene.Bounds(point.X, point.Y, Math.Max(radius, VisualMarkerScene.Extent(series, radius)));
            var inside = point.X >= plot.Left && point.X <= plot.Right && point.Y >= plot.Top && point.Y <= plot.Bottom;
            var visible = radius > 0 && (!chart.Options.ClipMarksToPlot || inside)
                && ShowMarker(chart, series, pointIndex);
            var resolvedLabel = ResolvePointLabel(chart, series, pointIndex, labelStyle);
            using (PointGroup(builder, series, index, pointIndex, bounds, resolvedLabel, stacks.IsStacked(index) ? stacks.Point(index, pointIndex) : null)) {
                if (visible) {
                    var pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
                        ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
                    var pointColor = PointColor(series, index, pointIndex, colors);
                    VisualMarkerScene.Draw(builder, series, pointIndex, point.X, point.Y, radius, pointColor,
                        VisualChartPaint.Series(series, pointColor, pointIndex),
                        series.Kind == ChartSeriesKind.Scatter && !string.IsNullOrWhiteSpace(series.SemanticRole) ? series.SemanticRole! : "marker",
                        pattern: pattern, backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "marker-pattern");
                }
            }
            if (visible && radius > 0) obstacles.Add(new LabelObstacle(PointId(index, pointIndex), bounds));
            AddLabel(chart, context, series, index, pointIndex, point, bounds, resolvedLabel, labels,
                calloutMetrics: series.SemanticRole == "point-callout" ? builder.MeasureText(resolvedLabel.DisplayedText, DisplayedStyle(resolvedLabel.Style)) : null);
        }
    }

    private static void DrawBars(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartBarCoordinateMap coordinates,
        ChartMapper map, ChartStackLayout stacks, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        var layout = ResolveBarLayout(chart, context, plot, map, stacks, index);
        var width = layout.Width;
        var offset = layout.Offset;
        var labelStyle = SeriesLabelStyle(chart, context, series, colors);
        AddHistogramSourceRegions(builder, series, index, plot);
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            var stack = stacks.Point(index, pointIndex);
            var y = map.Y(stack.End);
            var baseY = stacks.IsStacked(index) || series.IsHistogramDensity ? map.YOrBaseline(stack.Base) : map.YBaseline();
            var left = map.X(point.X) + offset - width / 2;
            var barWidth = width;
            if (ChartHistogramBarSlot.TryResolve(chart, coordinates, stacks, index, pointIndex, map, out var histogramLeft, out var histogramWidth)) {
                left = histogramLeft;
                barWidth = histogramWidth;
            }
            var bounds = new ChartRect(left, Math.Min(y, baseY), barWidth, Math.Abs(baseY - y));
            if (!series.IsHistogramDensity) bounds = VisibleSegmentBounds(chart, bounds, stack.Value);
            var resolvedLabel = ResolvePointLabel(chart, series, pointIndex, labelStyle);
            using (PointGroup(builder, series, index, pointIndex, bounds, resolvedLabel, stack)) {
                if (series.IsHistogramDensity) DrawDensityHistogramSurface(chart, context, builder, series, index, pointIndex, bounds, colors);
                else DrawBarSurface(chart, context, builder, series, pointIndex, bounds, PointColor(series, index, pointIndex, colors), colors);
            }
            obstacles.Add(new LabelObstacle(PointId(index, pointIndex), bounds));
            AddLabel(chart, context, series, index, pointIndex, new ChartPoint(left + barWidth / 2, y), bounds, resolvedLabel, labels);
        }
    }

    private static (double Width, double Offset) ResolveBarLayout(Chart chart, VisualRenderContext context, ChartRect plot, ChartMapper map, ChartStackLayout stacks, int index) {
        var barIndices = Enumerable.Range(0, chart.Series.Count).Where(i => chart.Series[i].Kind == ChartSeriesKind.Bar
            && (chart.Series[i].HistogramBinLayout == null || chart.Series[i].HistogramBinLayout!.Minimum == chart.Series[i].HistogramBinLayout!.Maximum)).ToArray();
        var slot = stacks.Slot(barIndices, index);
        var count = slot.Count;
        var position = slot.Position;
        var centers = barIndices.SelectMany(i => chart.Series[i].Points.Select(point => map.X(point.X))).Distinct().OrderBy(value => value).ToArray();
        var sampleSlots = chart.Options.IsSparkline ? chart.Options.SparklineSampleCount : 0;
        var spacing = sampleSlots > 0 ? plot.Width / sampleSlots : plot.Width;
        if (sampleSlots == 0)
            for (var point = 1; point < centers.Length; point++) spacing = Math.Min(spacing, centers[point] - centers[point - 1]);
        var occupied = spacing * .68;
        var gap = count > 1 ? Math.Min(context.Theme.Spacing / 2, occupied / (count * 4)) : 0;
        var width = Math.Max(sampleSlots > 0 ? 0 : .1, (occupied - gap * (count - 1)) / count);
        var offset = (position - (count - 1) / 2d) * (width + gap);
        return (width, offset);
    }

    private static bool ShowMarker(Chart chart, ChartSeries series, int pointIndex) {
        if (series.Markers.Enabled.HasValue) return series.Markers.Enabled.Value;
        if (series.Kind == ChartSeriesKind.Scatter) return true;
        if (chart.Options.IsSparkline) return false;
        var mode = chart.Options.LineMarkerMode ?? ChartLineMarkerMode.Last;
        if (mode == ChartLineMarkerMode.None) return false;
        if (mode == ChartLineMarkerMode.All) return true;
        if (mode == ChartLineMarkerMode.Last) return pointIndex == series.Points.Count - 1;
        return false;
    }

    private static TextStyle SeriesLabelStyle(Chart chart, VisualRenderContext context, ChartSeries series, VisualThemeColors colors) =>
        series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
        }));

    private static ResolvedPointLabel ResolvePointLabel(Chart chart, ChartSeries series, int pointIndex, TextStyle seriesStyle) {
        var value = series.Points[pointIndex].Y;
        var text = pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null
            ? series.PointLabels[pointIndex]! : series.HistogramBinLayout != null && !series.HistogramBins[pointIndex].Value.HasValue
                ? chart.Options.Labels.NoData : ChartNumericFormatter.FormatValue(chart.Options, value);
        var style = seriesStyle;
        if (pointIndex < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[pointIndex] != null)
            style = series.PointDataLabelStyles[pointIndex]!.Resolve(style);
        return new ResolvedPointLabel(text, TextCaseTransformer.Apply(text, style.TextCase, System.Globalization.CultureInfo.InvariantCulture), style);
    }

    private readonly struct ResolvedPointLabel {
        internal ResolvedPointLabel(string text, string displayedText, TextStyle style) { Text = text; DisplayedText = displayedText; Style = style; }
        internal string Text { get; }
        internal string DisplayedText { get; }
        internal TextStyle Style { get; }
    }

    private static void AddLabel(Chart chart, VisualRenderContext context, ChartSeries series, int seriesIndex, int pointIndex,
        ChartPoint anchor, ChartRect mark, ResolvedPointLabel resolvedLabel, List<LabelPlacementRequest> labels, double? observationValue = null, string? associatedId = null,
        TextMetrics? calloutMetrics = null) {
        if (!(series.ShowDataLabels ?? chart.Options.ShowDataLabels) || resolvedLabel.Text.Length == 0) return;
        if (series.HistogramBinLayout != null && !series.HistogramBins[pointIndex].Value.HasValue &&
            (pointIndex >= series.PointLabels.Count || series.PointLabels[pointIndex] == null)) return;
        var value = observationValue ?? series.Points[pointIndex].Y;
        var spacing = context.Theme.Spacing;
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        if (placement == ChartDataLabelPlacement.Auto && series.Kind == ChartSeriesKind.Bar && ChartStackLayout.Participates(chart, series))
            placement = ChartDataLabelPlacement.Inside;
        var candidates = new List<LabelCandidate>();
        var leftOffset = mark.Left - anchor.X - spacing;
        var rightOffset = mark.Right - anchor.X + spacing;
        if (placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
            anchor = new ChartPoint(mark.X + mark.Width / 2, mark.Y + mark.Height / 2);
            candidates.Add(new LabelCandidate(0, 0, .5, .5));
        } else if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var left = placement == ChartDataLabelPlacement.Left;
            candidates.Add(new LabelCandidate(left ? leftOffset : rightOffset, 0, left ? 1 : 0, .5));
            if (series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea) {
                candidates.Add(new LabelCandidate(left ? leftOffset : rightOffset, mark.Top - anchor.Y - spacing, left ? 1 : 0, 1));
                candidates.Add(new LabelCandidate(left ? leftOffset : rightOffset, mark.Bottom - anchor.Y + spacing, left ? 1 : 0, 0));
            }
        }
        else if (placement == ChartDataLabelPlacement.Below || placement == ChartDataLabelPlacement.Above) {
            var above = placement == ChartDataLabelPlacement.Above;
            foreach (var alignment in new[] { .5, 0, 1 }) candidates.Add(new LabelCandidate(0, above ? -spacing : spacing, alignment, above ? 1 : 0));
        }
        else {
            candidates.Add(new LabelCandidate(0, value >= 0 ? -spacing : spacing, .5, value >= 0 ? 1 : 0));
            candidates.Add(new LabelCandidate(rightOffset, 0, 0, .5));
            candidates.Add(new LabelCandidate(leftOffset, 0, 1, .5));
            // A label beside the value edge can fit above a sloping envelope even when a
            // centered side label would intersect it and the top lane lacks a full gap.
            candidates.Add(new LabelCandidate(rightOffset, 0, 0, value >= 0 ? 1 : 0));
            candidates.Add(new LabelCandidate(leftOffset, 0, 1, value >= 0 ? 1 : 0));
            candidates.Add(new LabelCandidate(rightOffset, value >= 0 ? -spacing : spacing, 0, value >= 0 ? 1 : 0));
            candidates.Add(new LabelCandidate(leftOffset, value >= 0 ? -spacing : spacing, 1, value >= 0 ? 1 : 0));
            // Local extrema can have strokes on both sides of the first candidate. Try a
            // second lane before shortening the label, then the opposite vertical side.
            candidates.Add(new LabelCandidate(0, value >= 0 ? -spacing * 2 : spacing * 2, .5, value >= 0 ? 1 : 0));
            candidates.Add(new LabelCandidate(0, value >= 0 ? spacing : -spacing, .5, value >= 0 ? 0 : 1));
            foreach (var alignment in new[] { .5, 0, 1 }) {
                candidates.Add(new LabelCandidate(0, mark.Top - anchor.Y - spacing, alignment, 1));
                candidates.Add(new LabelCandidate(0, mark.Bottom - anchor.Y + spacing, alignment, 0));
            }
        }
        if (series.SemanticRole == "point-callout" && placement != ChartDataLabelPlacement.Auto
            && placement != ChartDataLabelPlacement.Inside && placement != ChartDataLabelPlacement.Center) {
            // Callouts commonly mark a first/last extremum. Preserve the requested side
            // first, then keep the full caption beside its point when that side is outside.
            foreach (var alignment in new[] { .5, 0, 1 }) {
                candidates.Add(new LabelCandidate(0, spacing, alignment, 0));
                candidates.Add(new LabelCandidate(0, -spacing, alignment, 1));
            }
            candidates.Add(new LabelCandidate(leftOffset, 0, 1, .5));
            candidates.Add(new LabelCandidate(rightOffset, 0, 0, .5));
            if (calloutMetrics.HasValue) {
                // A broad caption at an endpoint can cross the adjacent sloping stroke in
                // the first lane. Try half and full measured text heights inward: jumping
                // straight to a full height can skip the clear band between two lines.
                // Placement still rejects every collision and out-of-bounds candidate.
                foreach (var fraction in new[] { .5, 1d }) {
                    var lane = spacing + calloutMetrics.Value.Height * fraction;
                    foreach (var direction in new[] { 1d, -1d }) {
                        var alignment = direction > 0 ? 0 : 1;
                        candidates.Add(new LabelCandidate(leftOffset, lane * direction, 1, alignment));
                        candidates.Add(new LabelCandidate(rightOffset, lane * direction, 0, alignment));
                        candidates.Add(new LabelCandidate(0, lane * direction, 1, alignment));
                        candidates.Add(new LabelCandidate(0, lane * direction, 0, alignment));
                    }
                }
            }
        }
        var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center;
        var autoInside = placement == ChartDataLabelPlacement.Auto && series.Kind == ChartSeriesKind.Bar;
        if (autoInside) candidates.Add(new LabelCandidate(mark.Left + mark.Width / 2 - anchor.X, mark.Top + mark.Height / 2 - anchor.Y, .5, .5));
        var style = resolvedLabel.Style;
        var insideStyle = style;
        Themes.SvgPaint? paint = null;
        Themes.SvgPaint? insidePaint = null;
        if ((inside || autoInside)
            && !chart.Options.DataLabelStyle.Color.HasValue && !series.DataLabelStyle.Color.HasValue
            && !(pointIndex < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[pointIndex]?.Color != null)) {
            var colors = context.Theme.Resolve(context.ThemeMode);
            var fill = PointColor(series, seriesIndex, pointIndex, colors);
            if (series.Kind == ChartSeriesKind.Bar || series.Kind == ChartSeriesKind.HorizontalBar || series.Kind == ChartSeriesKind.RangeBar) {
                var barStyle = chart.Options.ResolvePreparedBarVisualStyle();
                if (barStyle.Kind == ChartBarStyle.SegmentedCapsule) fill = ChartColorMath.WithOpacity(fill, barStyle.BodyOpacity);
            }
            var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
            var ink = ChartMarkText.OnPreparedMark(fill, VisualChartPaint.SeriesRole(series, pointIndex), backdrop);
            insideStyle = style.Clone(); insideStyle.Color = ink.Color;
            insidePaint = ink.Paint;
        }
        if (inside) { style = insideStyle; paint = insidePaint; }
        var request = new LabelPlacementRequest(resolvedLabel.Text, anchor, style, candidates) {
            AssociatedMarkId = associatedId ?? PointId(seriesIndex, pointIndex), Paint = paint
        };
        if (inside) request.Bounds = mark;
        if (autoInside) ((CartesianLabels)labels).ContainedInk.Add(request, (mark, insideStyle, insidePaint));
        if (series.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.Scatter)
            ((CartesianLabels)labels).Outlined.Add(request);
        labels.Add(request);
    }

    private static void DrawDataLabels(VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        foreach (var request in labels) request.MeasuredSize ??= builder.MeasureText(request.Text, request.Style);
        var placed = new LabelPlacementService().Place(labels, plot, obstacles, 2, builder.MeasureText);
        if (placed.Any(label => label.IsDropped || label.IsEllipsized))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.data-label-overflow", "Data labels were shortened or omitted to avoid marks and other labels within the available plot."));
        foreach (var label in placed) {
            if (label.IsDropped) continue;
            var ink = ((CartesianLabels)labels).ContainedInk;
            var contained = ink.TryGetValue(label.Request, out var insideInk) && LabelPlacementService.Contains(insideInk.Bounds, label.Bounds);
            var displayedStyle = DisplayedStyle(contained ? insideInk.Style : label.Request.Style);
            var outlined = ((CartesianLabels)labels).Outlined.Contains(label.Request);
            var surface = context.Theme.Resolve(context.ThemeMode).Surface;
            builder.Text(label.Text, label.Bounds.Left, label.Bounds.Top + builder.TextAscent(displayedStyle), displayedStyle,
                role: label.Request.AssociatedMarkId?.StartsWith("stack-total-", StringComparison.Ordinal) == true ? "stack-total-label" : "data-label",
                id: label.Request.AssociatedMarkId + "-label", paint: (contained ? insideInk.Paint : label.Request.Paint) ?? VisualChartPaint.Text(displayedStyle),
                stroke: outlined ? surface : null, strokeWidth: outlined ? ChartTextHalo.SvgStrokeWidth(displayedStyle.EffectiveFontSize, displayedStyle.Font.Weight >= 700) : 0,
                strokePaint: outlined ? SvgPaint.Of(surface, SvgColorRole.Surface) : null);
        }
    }

    private static TextStyle DisplayedStyle(TextStyle original) {
        var displayed = original.Clone();
        displayed.FontSize = original.EffectiveFontSize;
        displayed.Baseline = TextBaseline.Normal;
        displayed.TextCase = TextCaseTransform.None;
        return displayed;
    }
}
