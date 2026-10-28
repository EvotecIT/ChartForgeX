using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMarkerScene {
    internal static Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? Legend(Chart chart, ChartSeries series,
        ChartColor color, SvgPaint paint, int point = -1, ChartFillPattern pattern = ChartFillPattern.None, ChartBubbleSizeScale? bubbleScale = null) {
        if (!ChartSeriesKindTraits.SupportsMarkers(series.Kind)) return null;
        var markers = series.ResolvePointMarkers(point);
        if (series.Kind == ChartSeriesKind.Dumbbell && series.Dumbbell.IsConfigured) return DumbbellLegend(chart, series, color, paint, point, pattern);
        if (series.Kind == ChartSeriesKind.DifferenceArea) return (builder, bounds, context) => {
            var colors = context.Theme.Resolve(context.ThemeMode);
            foreach (var positive in new[] { true, false }) {
                var half = new ChartRect(bounds.Left + (positive ? 0 : bounds.Width / 2), bounds.Top, bounds.Width / 2, bounds.Height);
                var (fill, fillPaint) = VisualCartesianCompiler.DifferenceFill(series, chart.IndexOfSeries(series), positive, colors, context);
                builder.Rect(half, fill, role: positive ? "legend-difference-positive" : "legend-difference-negative", paint: VisualChartPaint.Fill(fillPaint));
                if (fill.A > 0) VisualFrameLayout.DrawLegendPattern(builder, half, pattern, context, role: "legend-area-pattern");
            }
            var y = bounds.Top + bounds.Height / 2;
            builder.Line(bounds.Left, y, bounds.Right, y, color, series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth,
                role: "legend-line", paint: VisualChartPaint.Stroke(paint));
            if (Enabled(series, series.MarkerRadius.HasValue || series.PointColors.Any(value => value.HasValue)
                || series.PointFillPatterns.Any(value => value.HasValue)) && Radius(series, context) > 0)
                Draw(builder, series, point, bounds.Left + bounds.Width / 2, y, Math.Min(bounds.Width, bounds.Height) * .4,
                    color, paint, "legend-swatch", pattern: pattern, backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame));
        };
        var connected = series.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area
            or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBand;
        var radarArea = series.Kind == ChartSeriesKind.Radar && series.Radar.Form == ChartLineAreaForm.Area
            && ChartRadarGeometry.HasClosedArea(chart, series);
        var polarArea = series.Kind == ChartSeriesKind.Polar && series.Polar.Form == ChartLineAreaForm.Area
            && ChartPolarPathGeometry.HasClosedArea(series);
        // Retain ordinary area/line swatches until their marker or radar paint is explicitly configured.
        if (!series.Markers.IsConfigured && (connected || radarArea && !series.Radar.FillOpacity.HasValue && series.Radar.OutlineStroke == null)) return null;
        return (builder, bounds, context) => {
            var colors = context.Theme.Resolve(context.ThemeMode);
            var x = bounds.Left + bounds.Width / 2; var y = bounds.Top + bounds.Height / 2;
            var area = radarArea || polarArea || series.Kind is ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea
                or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBand;
            var line = ChartSeriesKindTraits.IsLineLikeLegendKind(series.Kind);
            if (area) {
                var opacity = radarArea ? series.Radar.FillOpacity ?? context.Theme.AreaOpacity
                    : polarArea ? series.Polar.FillOpacity ?? context.Theme.AreaOpacity : context.Theme.AreaOpacity;
                var fill = ChartColorMath.WithOpacity(color, opacity);
                builder.Rect(bounds, fill, role: "legend-area", paint: VisualChartPaint.Fill(paint.WithOpacity(fill, opacity)));
                var hatchOpacity = radarArea || polarArea ? Math.Min(1, opacity / Math.Max(.000001, context.Theme.AreaOpacity)) : 1;
                VisualFrameLayout.DrawLegendPattern(builder, bounds, pattern, context, hatchOpacity, "legend-area-pattern");
            }
            if (line) {
                var outline = series.Kind == ChartSeriesKind.Radar ? series.Radar.OutlineStroke
                    : series.Kind == ChartSeriesKind.Polar ? series.Polar.OutlineStroke : null;
                var width = VisualReferenceStroke.PaintedWidth(outline, color,
                    series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth);
                var source = outline?.Color ?? color;
                var opacity = outline?.Opacity ?? 1;
                var actual = ChartColorMath.WithOpacity(source, opacity);
                var outlinePaint = outline?.Color != null ? SvgPaint.Literal(source) : paint;
                if (width > 0) builder.Line(bounds.Left, y, bounds.Right, y, actual, outline == null ? width : Math.Min(width, bounds.Height),
                    role: "legend-line", dash: outline?.ResolveDash(Array.Empty<double>()),
                    paint: VisualChartPaint.Stroke(outlinePaint.WithOpacity(actual, opacity)));
            }
            var familyDefault = connected ? !chart.Options.IsSparkline && chart.Options.LineMarkerMode != ChartLineMarkerMode.None : true;
            if (series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea)
                familyDefault = series.MarkerRadius.HasValue || series.PointColors.Any(value => value.HasValue)
                    || series.PointFillPatterns.Any(value => value.HasValue);
            var hasRadius = series.Kind == ChartSeriesKind.Bubble ? bubbleScale?.HasVisibleRadius ?? true : Radius(series, context, markers) > 0;
            if (!Enabled(series, familyDefault, markers) || !hasRadius) return;
            var strokeWidth = FamilyStrokeWidth(series); ChartColor? stroke = strokeWidth > 0 ? colors.Surface : null;
            SvgPaint? strokePaint = stroke.HasValue ? SvgPaint.Of(colors.Surface, SvgColorRole.Surface) : null;
            var markerFill = color; var markerPaint = paint;
            if (series.Kind == ChartSeriesKind.Bubble) {
                markerFill = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleFillOpacity);
                markerPaint = paint.WithOpacity(markerFill, ChartVisualPrimitives.BubbleFillOpacity);
                stroke = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleStrokeOpacity);
                strokePaint = paint.WithOpacity(stroke.Value, ChartVisualPrimitives.BubbleStrokeOpacity);
            }
            Draw(builder, series, point, x, y, Math.Min(bounds.Width, bounds.Height) * .4, markerFill, markerPaint,
                "legend-swatch", stroke, strokeWidth > 0 ? strokeWidth : 1, strokePaint, pattern,
                ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "legend-pattern",
                quantitativeOutline: QuantitativeOutline(series, colors), markerOptions: markers);
        };
    }

    /// <summary>Uses the same endpoint overrides as native comparisons while retaining the existing bounded legend swatch.</summary>
    private static Action<VisualSceneBuilder, ChartRect, VisualRenderContext> DumbbellLegend(Chart chart, ChartSeries series,
        ChartColor color, SvgPaint paint, int point, ChartFillPattern pattern) {
        var start = series.ResolveDumbbellMarker(start: true); var end = series.ResolveDumbbellMarker(start: false);
        return (builder, bounds, context) => {
            var colors = context.Theme.Resolve(context.ThemeMode);
            var firstRadius = Radius(series, context, start); var lastRadius = Radius(series, context, end);
            var maximum = Math.Max(firstRadius, lastRadius);
            var scale = maximum > 0 ? Math.Min(bounds.Width * .2, bounds.Height * .4) / maximum : 0;
            var firstX = bounds.Left + bounds.Width * .2; var lastX = bounds.Right - bounds.Width * .2;
            var y = bounds.Top + bounds.Height / 2;
            var connector = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.DumbbellConnectorOpacity);
            var width = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.DumbbellConnectorStrokeWidth;
            builder.Line(firstX, y, lastX, y, connector, Math.Min(width, bounds.Height / 3), role: "legend-dumbbell-connector",
                paint: VisualChartPaint.Stroke(paint.WithOpacity(connector, ChartVisualPrimitives.DumbbellConnectorOpacity)));
            Draw(builder, series, point, firstX, y, firstRadius * scale, colors.MutedForeground,
                SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text), "legend-dumbbell-start", markerOptions: start);
            Draw(builder, series, point, lastX, y, lastRadius * scale, color, paint, "legend-dumbbell-end",
                pattern: pattern, backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), markerOptions: end);
        };
    }

}
