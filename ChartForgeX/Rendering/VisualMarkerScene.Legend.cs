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
        var connected = series.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area
            or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBand;
        var radarArea = series.Kind == ChartSeriesKind.Radar && series.Radar.Form == ChartLineAreaForm.Area;
        // Retain ordinary area/line swatches until their marker or radar paint is explicitly configured.
        if (!series.Markers.IsConfigured && (connected || radarArea && !series.Radar.FillOpacity.HasValue)) return null;
        return (builder, bounds, context) => {
            var colors = context.Theme.Resolve(context.ThemeMode);
            var x = bounds.Left + bounds.Width / 2; var y = bounds.Top + bounds.Height / 2;
            var area = radarArea || series.Kind is ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea
                or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBand;
            var line = ChartSeriesKindTraits.IsLineLikeLegendKind(series.Kind);
            if (area) {
                var opacity = radarArea ? series.Radar.FillOpacity ?? context.Theme.AreaOpacity : context.Theme.AreaOpacity;
                var fill = ChartColorMath.WithOpacity(color, opacity);
                builder.Rect(bounds, fill, role: "legend-area", paint: VisualChartPaint.Fill(paint.WithOpacity(fill, opacity)));
                var hatchOpacity = radarArea ? Math.Min(1, opacity / Math.Max(.000001, context.Theme.AreaOpacity)) : 1;
                VisualFrameLayout.DrawLegendPattern(builder, bounds, pattern, context, hatchOpacity, "legend-area-pattern");
            }
            if (line) builder.Line(bounds.Left, y, bounds.Right, y, color,
                series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth, role: "legend-line", paint: VisualChartPaint.Stroke(paint));
            var familyDefault = connected ? !chart.Options.IsSparkline && chart.Options.LineMarkerMode != ChartLineMarkerMode.None : true;
            if (series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea)
                familyDefault = series.MarkerRadius.HasValue || series.PointColors.Any(value => value.HasValue)
                    || series.PointFillPatterns.Any(value => value.HasValue);
            var hasRadius = series.Kind == ChartSeriesKind.Bubble ? bubbleScale?.HasVisibleRadius ?? true : Radius(series, context) > 0;
            if (!Enabled(series, familyDefault) || !hasRadius) return;
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
                ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "legend-pattern");
        };
    }
}
