using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // One formatter result per axis/value serves gutter measurement, placement and detached semantics.
    private sealed class AxisLabelCache {
        private readonly ChartAxisValueFormatter.Cache _labels;
        private readonly Dictionary<ChartAxis, IReadOnlyList<double>> _ticks = new();
        private readonly Dictionary<ChartAxis, IReadOnlyList<double>> _fittedTicks = new();
        private readonly Dictionary<ChartAxis, IReadOnlyList<double>> _captionTicks = new();
        private readonly Dictionary<ChartAxis, Func<double, string>> _formatters = new();
        private readonly Dictionary<ChartAxis, Func<double, string>> _readoutFormatters = new();
        private readonly Dictionary<ChartAxis, HashSet<double>> _formattedValues = new();
        private readonly Dictionary<ChartAxis, Dictionary<double, (string Text, bool Shortened)>> _categoryDisplay = new();
        private readonly Dictionary<ChartAxis, (IReadOnlyList<double> Ticks, double TrackLength)> _categoryPlan = new();
        private IReadOnlyList<double>? _regularTimeBarObservations;
        private bool _hasTimeBarObservations;
        internal ChartAxis? HorizontalValueAxis { get; set; }
        internal ChartAxis? HorizontalCategoryAxis { get; set; }
        internal CategoryGroupSource? CategoryGroups { get; set; }
        internal double[]? HorizontalGroupBandSizes { get; set; }
        internal AxisLabelCache(ChartAxisValueFormatter.Cache labels) => _labels = labels;
        internal IReadOnlyList<double>? TimeBarObservations(Chart chart) {
            if (!_hasTimeBarObservations) { _regularTimeBarObservations = RegularTimeBarObservations(chart); _hasTimeBarObservations = true; }
            return _regularTimeBarObservations;
        }
        internal void Set(ChartAxis axis, double value, string text) => _labels.Set(axis, value, text);
        internal void SetCategoryDisplay(ChartAxis axis, Dictionary<double, (string Text, bool Shortened)>? captions) {
            if (captions == null) { _categoryDisplay.Remove(axis); _categoryPlan.Remove(axis); }
            else _categoryDisplay[axis] = captions;
        }
        internal void PlanCategoryTicks(ChartAxis axis, IReadOnlyList<double> ticks, double trackLength) => _categoryPlan[axis] = (ticks, trackLength);
        internal IReadOnlyList<double>? PlannedCategoryTicks(ChartAxis axis) => _categoryPlan.TryGetValue(axis, out var plan) ? plan.Ticks : null;
        internal double PlannedCategoryTrack(ChartAxis axis) => _categoryPlan[axis].TrackLength;
        internal string CategoryDisplay(ChartAxis axis, double value, string source) =>
            _categoryDisplay.TryGetValue(axis, out var captions) && captions.TryGetValue(value, out var display) ? display.Text : source;
        internal bool CategoryShortened(ChartAxis axis, double value) =>
            _categoryDisplay.TryGetValue(axis, out var captions) && captions.TryGetValue(value, out var display) && display.Shortened;
        internal void SetTicks(ChartAxis axis, IReadOnlyList<double> ticks) => _ticks[axis] = ticks;
        internal bool HasAuthoredTicks(ChartAxis axis) => axis.Labels.Count > 0 || _ticks.ContainsKey(axis);
        internal void SetFittedTicks(ChartAxis axis, IReadOnlyList<double> ticks) => _fittedTicks[axis] = ticks;
        internal void ResetFittedTicks(ChartAxis axis) => _fittedTicks.Remove(axis);
        internal void IncludeValueTicks(ChartAxis axis, double minimum, double maximum) {
            if (axis.Labels.Count == 0) return;
            _ticks[axis] = ChartTicks.ForValueAxis(axis, minimum, maximum);
        }
        internal IReadOnlyList<double> Ticks(ChartAxis axis, double minimum, double maximum) {
            var ticks = _fittedTicks.TryGetValue(axis, out var fitted) ? fitted
                : _ticks.TryGetValue(axis, out var authored) ? ChartTicks.PreserveFormatting(authored, authored.Where(value => value >= minimum && value <= maximum).ToArray())
                : ChartTicks.ForAxis(axis, minimum, maximum);
            ticks = ChartTimeScale.UniqueSessionTicks(axis, ticks);
            RefreshFormatter(axis, ticks);
            return ticks;
        }
        private void RefreshFormatter(ChartAxis axis, IReadOnlyList<double> ticks) {
            if (!_captionTicks.TryGetValue(axis, out var previous) || !previous.SequenceEqual(ticks)
                || ChartTicks.IsNumericTimeFallback(previous) != ChartTicks.IsNumericTimeFallback(ticks)
                || !ChartTimeScale.SameFormatting(previous, ticks)) {
                _captionTicks[axis] = ticks;
                _formatters.Remove(axis);
                _readoutFormatters.Remove(axis);
            }
        }
        internal string Format(ChartAxis axis, double value, Func<double, string>? fallback, IReadOnlyList<double> ticks) {
            RefreshFormatter(axis, ticks);
            if (axis.LabelFormatter != null || fallback != null) {
                if (!_formattedValues.TryGetValue(axis, out var values)) _formattedValues[axis] = values = new HashSet<double>();
                values.Add(value);
            }
            if (!_formatters.TryGetValue(axis, out var formatter)) _formatters.Add(axis, formatter = ChartAxisValueFormatter.Create(axis, ticks, fallback));
            return _labels.Format(axis, value, formatter, axis.LabelFormatter != null || fallback != null);
        }
        // Readouts reuse painted axis captions, but must not materialize a data-label fallback
        // for an unpainted tick or an observation whose authored caption kept that callback lazy.
        internal string FormatReadout(ChartAxis axis, double value, IReadOnlyList<double> ticks, string? numericCaption = null) {
            var authoredAxis = axis.LabelFormatter != null || axis.Scale == ChartScaleKind.Time
                || ChartAxisValueFormatter.FindExplicitLabel(axis.Labels, value) != null;
            if (!authoredAxis && numericCaption != null) return numericCaption;
            return FormatDisplay(axis, value, ticks);
        }
        internal string FormatDisplay(ChartAxis axis, double value, IReadOnlyList<double> ticks) {
            RefreshFormatter(axis, ticks);
            if (!_readoutFormatters.TryGetValue(axis, out var formatter))
                _readoutFormatters.Add(axis, formatter = ChartAxisValueFormatter.Create(axis, ticks, retainTimeDateContext: true));
            return _labels.Format(axis, value, formatter);
        }
        internal IReadOnlyDictionary<double, string> CaptureReadoutFacts(ChartAxis axis) => _labels.Capture(axis);
        internal IReadOnlyList<double> SemanticTicks(ChartAxis axis, IReadOnlyList<double> ticks) =>
            _formattedValues.TryGetValue(axis, out var values) ? values.Concat(ticks).Distinct().OrderBy(value => value).ToArray() : ticks;
    }

    private static ChartRect MeasurePlot(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartRange range, ChartRange? secondaryRange, VisualThemeColors colors, AxisLabelCache labels) {
        if (!chart.Options.ShowAxes) return viewport;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground });
        var spacing = context.Theme.Spacing;
        var yMetrics = chart.Options.YAxis.Visible ? TickMetrics(builder, chart.Options.YAxis, range.MinY, range.MaxY, style, chart.Options.ValueFormatter, labels) : default;
        var xMetrics = chart.Options.XAxis.Visible ? TickMetrics(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, null, labels) : default;
        var secondaryMetrics = secondaryRange != null && chart.Options.SecondaryYAxis.Visible
            ? TickMetrics(builder, chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY, style, chart.Options.ValueFormatter, labels) : default;
        var left = chart.Options.YAxis.Visible ? yMetrics.Width + spacing : 0;
        var right = secondaryRange != null && chart.Options.SecondaryYAxis.Visible
            ? secondaryMetrics.Width + spacing : 0;
        var bottom = chart.Options.XAxis.Visible ? xMetrics.Height + spacing : 0;
        var xTitle = XAxisTitle(chart);
        if (chart.Options.XAxis.Visible && xTitle.Length > 0) bottom += builder.MeasureText(xTitle, titleStyle).Height + spacing;
        if (chart.Options.XAxis.Visible) bottom += CategoryGroupBandSizes(builder, labels, style, spacing,
            range.MinX, range.MaxX, viewport.Width, Math.Max(0, viewport.Height * .45 - bottom)).Sum();
        var top = 0d;
        if (chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0) top = builder.MeasureText(chart.YAxisTitle, titleStyle).Height + spacing;
        if (secondaryRange != null && chart.Options.SecondaryYAxis.Visible && chart.SecondaryYAxisTitle.Length > 0)
            top = Math.Max(top, builder.MeasureText(chart.SecondaryYAxisTitle, titleStyle).Height + spacing);
        // Tick text is shortened in its reserved strip; it cannot consume the entire chart width.
        left = Math.Min(left, viewport.Width * .35);
        right = Math.Min(right, viewport.Width * .35);
        bottom = Math.Min(bottom, viewport.Height * .45);
        top = Math.Min(top, viewport.Height * .25);
        // Axis tracks end at the data bounds; their centered captions occupy the
        // surrounding frame instead of moving inward toward a different value.
        var xOverhang = CategoryCaptionOverhang(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, labels);
        left = Math.Max(left, Math.Min(xOverhang?.Left ?? xMetrics.Width / 2, viewport.Width * .35));
        right = Math.Max(right, Math.Min(xOverhang?.Right ?? xMetrics.Width / 2, viewport.Width * .35));
        var halfHeight = Math.Max(yMetrics.Height, secondaryMetrics.Height) / 2;
        top = Math.Max(top, Math.Min(halfHeight, viewport.Height * .25));
        bottom = Math.Max(bottom, Math.Min(halfHeight, viewport.Height * .45));
        return new ChartRect(viewport.Left + left, viewport.Top + top, Math.Max(0, viewport.Width - left - right), Math.Max(0, viewport.Height - top - bottom));
    }

    private static TextMetrics TickMetrics(VisualSceneBuilder builder, ChartAxis axis, double minimum, double maximum, TextStyle style, Func<double, string>? fallback, AxisLabelCache labels) {
        return TickMetrics(builder, axis, labels.Ticks(axis, minimum, maximum), style, fallback, labels);
    }

    private static TextMetrics TickMetrics(VisualSceneBuilder builder, ChartAxis axis, IReadOnlyList<double> ticks, TextStyle style, Func<double, string>? fallback, AxisLabelCache labels) {
        var width = 0d; var height = 0d;
        foreach (var tick in labels.PlannedCategoryTicks(axis) ?? ticks) {
            var text = labels.CategoryDisplay(axis, tick, labels.Format(axis, tick, fallback, ticks));
            var metrics = VisualAxisText.RotatedMetrics(builder.MeasureText(text, style), axis.LabelAngle);
            width = Math.Max(width, metrics.Width); height = Math.Max(height, metrics.Height);
        }
        return new TextMetrics(width, height, height);
    }

    private static string XAxisTitle(Chart chart) {
        var title = chart.XAxisTitle;
        if (chart.Options.XAxis.Scale == ChartScaleKind.Time && chart.Options.XAxis.ShowTimeZone) {
            var zone = chart.Options.XAxis.TimeZoneLabel ?? chart.Options.XAxis.TimeZone?.Id ?? "UTC";
            title = title.Length == 0 ? zone : title + " (" + zone + ")";
        }
        return title;
    }

    private static void DrawAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRange range,
        ChartMapper map, ChartRange? secondaryRange, ChartMapper? secondaryMap, VisualThemeColors colors, ChartRect viewport, AxisLabelCache labels,
        ChartRect labelBounds) {
        var xTicks = labels.Ticks(chart.Options.XAxis, range.MinX, range.MaxX);
        var yTicks = labels.Ticks(chart.Options.YAxis, range.MinY, range.MaxY);
        if (chart.Options.ShowGrid) {
            var grid = chart.Options.ResolvePreparedGridLineStyle();
            var gridWidth = chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth;
            var dash = grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null;
            foreach (var tick in yTicks) {
                var y = map.Y(tick);
                if (grid.ShowHorizontalLines) builder.Line(plot.Left, y, plot.Right, y, ChartColorMath.WithOpacity(colors.Grid, grid.HorizontalOpacity), gridWidth, role: "grid-y", dash: dash,
                    paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Grid, SvgColorRole.Grid).WithOpacity(ChartColorMath.WithOpacity(colors.Grid, grid.HorizontalOpacity), grid.HorizontalOpacity)));
            }
            foreach (var tick in xTicks) {
                var x = map.X(tick);
                if (grid.ShowVerticalLines) builder.Line(x, plot.Top, x, plot.Bottom, ChartColorMath.WithOpacity(colors.Grid, grid.VerticalOpacity), gridWidth, role: "grid-x", dash: dash,
                    paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Grid, SvgColorRole.Grid).WithOpacity(ChartColorMath.WithOpacity(colors.Grid, grid.VerticalOpacity), grid.VerticalOpacity)));
            }
        }
        if (!chart.Options.ShowAxes) {
            DrawCategoryGroups(chart, context, builder, labels, map.X, range.MinX, range.MaxX, default, colors);
            return;
        }
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground
        });
        var spacing = context.Theme.Spacing;
        if (chart.Options.XAxis.Visible && !chart.Options.XAxis.CrossAt.HasValue) {
            if (chart.Options.XAxis.ShowLine) DrawAxisRule(chart, context, builder, plot, range, map, chart.Options.XAxis, true, plot.Bottom, colors, "axis-x");
            var tickHeight = TickMetrics(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, null, labels).Height;
            var title = XAxisTitle(chart);
            var titleHeight = string.IsNullOrEmpty(title) ? 0 : builder.MeasureText(title,
                chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground })).Height + spacing;
            // Outside bar labels and stack totals own the strip between the mapper
            // and labelBounds. Category captions start after that strip, including
            // negative or reversed totals that sit below the plotted marks.
            var available = Math.Max(0, viewport.Bottom - labelBounds.Bottom - spacing);
            var groupHeight = CategoryGroupBandSizes(builder, labels, style, spacing, range.MinX, range.MaxX,
                plot.Width, Math.Max(0, available - tickHeight - titleHeight)).Sum();
            titleHeight = FitAxisTitleReservation(tickHeight + groupHeight, titleHeight, available);
            var bounds = new ChartRect(viewport.Left, labelBounds.Bottom + spacing, viewport.Width,
                Math.Max(0, Math.Min(tickHeight, available - titleHeight - groupHeight)));
            DrawAxisLabels(builder, chart.Options, chart.Options.XAxis, xTicks, map.X, true, false, bounds, style, spacing, null, labels, plot.Width);
            DrawCategoryGroups(chart, context, builder, labels, map.X, range.MinX, range.MaxX,
                new ChartRect(plot.Left, bounds.Bottom, plot.Width, groupHeight), colors);
            if (!string.IsNullOrEmpty(title)) DrawAxisTitle(chart, context, builder, title,
                new ChartRect(plot.Left, bounds.Bottom + groupHeight + spacing, plot.Width, Math.Max(0, viewport.Bottom - bounds.Bottom - groupHeight - spacing)),
                colors, TextAlignment.Center, "axis-x-title");
        } else if (!chart.Options.XAxis.Visible || !chart.Options.XAxis.CrossAt.HasValue)
            DrawCategoryGroups(chart, context, builder, labels, map.X, range.MinX, range.MaxX, default, colors);
        if (chart.Options.YAxis.Visible && !chart.Options.YAxis.CrossAt.HasValue) {
            if (chart.Options.YAxis.ShowLine) DrawAxisRule(chart, context, builder, plot, range, map, chart.Options.YAxis, false, plot.Left, colors, "axis-y");
            var bounds = new ChartRect(viewport.Left, viewport.Top, Math.Max(0, plot.Left - viewport.Left - spacing), viewport.Height);
            DrawAxisLabels(builder, chart.Options, chart.Options.YAxis, yTicks, map.Y, false, false, bounds, style, spacing, chart.Options.ValueFormatter, labels, plot.Height);
            if (!string.IsNullOrEmpty(chart.YAxisTitle)) DrawAxisTitle(chart, context, builder, chart.YAxisTitle,
                new ChartRect(plot.Left, viewport.Top, Math.Max(0, secondaryMap != null && chart.Options.SecondaryYAxis.Visible && chart.SecondaryYAxisTitle.Length > 0 ? plot.Width / 2 - spacing / 2 : plot.Width), Math.Max(0, labelBounds.Top - viewport.Top - spacing)),
                colors, TextAlignment.Left, "axis-y-title");
        }
        if (secondaryMap != null && secondaryRange != null && chart.Options.SecondaryYAxis.Visible && !chart.Options.SecondaryYAxis.CrossAt.HasValue) {
            var ticks = labels.Ticks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
            if (chart.Options.SecondaryYAxis.ShowLine) DrawAxisRule(chart, context, builder, plot, range, map, chart.Options.SecondaryYAxis, false, plot.Right, colors, "axis-secondary-y");
            var bounds = new ChartRect(plot.Right + spacing, viewport.Top, Math.Max(0, viewport.Right - plot.Right - spacing), viewport.Height);
            DrawAxisLabels(builder, chart.Options, chart.Options.SecondaryYAxis, ticks, secondaryMap.Y, false, true, bounds, style, spacing, chart.Options.ValueFormatter, labels, plot.Height);
            if (!string.IsNullOrEmpty(chart.SecondaryYAxisTitle)) {
                var width = chart.YAxisTitle.Length > 0 && chart.Options.YAxis.Visible ? plot.Width / 2 - spacing / 2 : plot.Width;
                DrawAxisTitle(chart, context, builder, chart.SecondaryYAxisTitle,
                    new ChartRect(plot.Right - width, viewport.Top, Math.Max(0, width), Math.Max(0, labelBounds.Top - viewport.Top - spacing)),
                    colors, TextAlignment.Right, "axis-secondary-y-title");
            }
        }
    }

    private static void DrawAxisLabels(VisualSceneBuilder builder, ChartOptions options, ChartAxis axis, IReadOnlyList<double> ticks, Func<double, double> coordinate,
        bool horizontal, bool secondary, ChartRect bounds, TextStyle style, double spacing, Func<double, string>? fallback, AxisLabelCache labels, double trackLength, List<LabelObstacle>? obstacles = null) {
        // An intentionally oversized style still fits its complete footprint.
        // Ordinary captions keep their font size and use a coarser scale/stride.
        var gap = AxisLabelGap(axis, spacing);
        if (style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) {
            var pitch = trackLength;
            for (var tick = 1; tick < ticks.Count; tick++) {
                var distance = Math.Abs(coordinate(ticks[tick]) - coordinate(ticks[tick - 1]));
                if (distance > 0) pitch = Math.Min(pitch, distance);
            }
            pitch = Math.Max(0, pitch - gap * 2);
            style = VisualAxisText.FitOversized(builder, style, new ChartRect(0, 0, horizontal ? Math.Min(bounds.Width, pitch) : bounds.Width,
                horizontal ? bounds.Height : Math.Min(bounds.Height, pitch)), ticks.Select(tick => labels.Format(axis, tick, fallback, ticks)).ToArray(), axis.LabelAngle);
        }
        var shortenedCategories = false;
        string Display(double tick) {
            var text = labels.Format(axis, tick, fallback, ticks);
            if (horizontal) {
                shortenedCategories |= labels.CategoryShortened(axis, tick);
                return labels.CategoryDisplay(axis, tick, text);
            }
            if (!labels.HasAuthoredTicks(axis) || VisualAxisText.Angle(axis.LabelAngle) != 0 || bounds.Width <= 0) return text;
            // Only named horizontal categories use this bounded caption fallback. Numeric
            // and calendar value axes keep their complete-caption scale/stride policy.
            if (ReferenceEquals(axis, labels.HorizontalCategoryAxis) && axis.Labels.Count > 0 && axis.Scale != ChartScaleKind.Time) {
                var displayed = VisualAxisText.FitWrappedCategory(builder, text, style, bounds.Width, out var shortened);
                shortenedCategories |= shortened;
                return displayed;
            }
            return string.Join("\n", ChartLabelWrapping.BalancedTwoLine(text, style.EffectiveFontSize, bounds.Width,
                (value, _) => builder.MeasureText(value, style).Width));
        }
        var visible = labels.PlannedCategoryTicks(axis) is { } planned ? new HashSet<double>(planned)
            : SelectAxisLabelStride(builder, axis, ticks, coordinate, horizontal, style, gap, fallback, labels, Display);
        var requests = new List<LabelPlacementRequest>(); var complete = new List<string>();
        var role = horizontal ? secondary ? "axis-secondary-x-label" : "axis-x-label" : secondary ? "axis-secondary-y-label" : "axis-y-label";
        var index = 0;
        foreach (var tick in labels.SemanticTicks(axis, ticks)) {
            var text = labels.Format(axis, tick, fallback, ticks);
            var value = coordinate(tick);
            var anchor = horizontal ? new ChartPoint(value, bounds.Top) : new ChartPoint(secondary ? bounds.Left : bounds.Right, value);
            // Regions describe source content, including text with no visible placement. They are not hit-test targets.
            var regionBounds = horizontal ? new ChartRect(value, bounds.Top, 0, bounds.Height) : new ChartRect(bounds.Left, value, bounds.Width, 0);
            builder.AddRegion(new VisualSemanticRegion(role + "-" + Number(index++), role, regionBounds,
                TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture) + " (" + Number(tick) + ")"));
            if (text.Length == 0) continue;
            if (!visible.Contains(tick)) continue;
            var tickStyle = style;
            if (horizontal && !secondary && options.TryGetXAxisLabelHighlight(tick, out var highlight)) { tickStyle = style.Clone(); tickStyle.Color = highlight; }
            complete.Add(TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture));
            requests.Add(new LabelPlacementRequest(Display(tick), anchor, tickStyle, horizontal
                ? new[] { new LabelCandidate(0, 0, .5, 0) }
                : new[] { new LabelCandidate(0, 0, secondary ? 0 : 1, .5) }) { Fallback = LabelFallbackRule.Drop });
        }
        // All candidates remain anchored to their tick. The measured frame has
        // already reserved endpoint room and selected a readable label rhythm.
        foreach (var request in requests) {
            request.MeasuredSize = builder.MeasureText(request.Text, request.Style);
            request.RotationDegrees = VisualAxisText.Angle(axis.LabelAngle);
        }
        var placement = new LabelPlacementService();
        // All explicitly permits overlaps; fitting still keeps each individual label inside its axis strip.
        var placed = axis.LabelDensity == ChartLabelDensity.All
            ? requests.Select(request => placement.Place(new[] { request }, bounds, obstacles, 0, builder.MeasureText)[0]).ToArray()
            : placement.Place(requests, bounds, obstacles, gap, builder.MeasureText);
        if (shortenedCategories || placed.Any(label => label.IsDropped))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.axis-label-overflow", "An axis caption was shortened or could not fit at its tick inside the available frame."));
        for (var item = 0; item < placed.Count; item++) {
            var label = placed[item];
            // A shortened caption keeps its complete name as a native tooltip.
            using (label.IsDropped || label.Text.Replace("\n", " ") == complete[item].Replace("\n", " ") ? null : builder.PushTooltip(complete[item]))
                VisualAxisText.Draw(builder, label, role);
            if (!label.IsDropped && obstacles != null) obstacles.Add(new LabelObstacle(role + "-interior-" + obstacles.Count, label.Bounds));
        }
    }

    private static double FitAxisTitleReservation(double ticks, double title, double available) =>
        ticks + title <= available || title == 0 ? title : available * title / (ticks + title);

    private static void DrawAxisTitle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, ChartRect bounds,
        VisualThemeColors colors, TextAlignment alignment, string role, List<LabelObstacle>? obstacles = null) {
        var style = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground, Alignment = alignment });
        style.Alignment = alignment;
        style = VisualAxisText.FitOversized(builder, style, bounds, new[] { text });
        VisualAxisText.Title(builder, text, bounds, style, role, null,
            "cartesian.axis-title-overflow", "An axis title was shortened or omitted within its measured frame.", obstacles);
    }
}
