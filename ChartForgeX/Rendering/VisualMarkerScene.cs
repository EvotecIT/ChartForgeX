using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Resolves marker paint once and emits the shared native geometry consumed by both static exporters.</summary>
internal static partial class VisualMarkerScene {
    internal static bool Enabled(ChartSeries series, bool familyDefault = true, ChartMarkerOptions? markerOptions = null) => (markerOptions ?? series.Markers).Enabled ?? familyDefault;

    internal static double Radius(ChartSeries series, VisualRenderContext context, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        if (markers.Radius.HasValue) return markers.Radius.Value;
        var radius = context.Theme.MarkerRadius;
        return series.Kind switch {
            ChartSeriesKind.ErrorBar => System.Math.Max(ChartVisualPrimitives.ErrorBarMarkerMinRadius, radius + ChartVisualPrimitives.ErrorBarMarkerRadiusExtra),
            ChartSeriesKind.Dumbbell => System.Math.Max(ChartVisualPrimitives.DumbbellMarkerMinRadius, radius + ChartVisualPrimitives.DumbbellMarkerRadiusExtra),
            ChartSeriesKind.Lollipop => System.Math.Max(4, radius + 2.25),
            ChartSeriesKind.Slope => System.Math.Max(ChartVisualPrimitives.SlopeMarkerMinRadius, radius + ChartVisualPrimitives.SlopeMarkerRadiusExtra),
            _ => radius
        };
    }

    internal static double FamilyStrokeWidth(ChartSeries series) => series.Kind switch {
        ChartSeriesKind.Bubble => series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BubbleStrokeWidth,
        ChartSeriesKind.Scatter when UsesQuantitativeOutline(series) => 1,
        ChartSeriesKind.Lollipop => ChartVisualPrimitives.LollipopMarkerStrokeWidth,
        ChartSeriesKind.Radar or ChartSeriesKind.Polar => 1,
        _ => 0
    };

    internal static bool UsesQuantitativeOutline(ChartSeries series) =>
        series.ColorScale != null && series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble;

    /// <summary>Shares the neutral theme outline between numeric-color marks and their marker legends.</summary>
    internal static (ChartColor Color, SvgPaint Paint)? QuantitativeOutline(ChartSeries series, VisualThemeColors colors) =>
        UsesQuantitativeOutline(series)
            ? (colors.Foreground, SvgPaint.ThemeToken(colors.Foreground, VisualThemeColorToken.Foreground, SvgColorRole.Text)) : null;

    internal static double StrokeWidth(ChartSeries series, bool familyOutline, double familyWidth = 1, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        if (!familyOutline && !markers.Stroke.HasValue && !markers.StrokeWidth.HasValue && markers.OutlineStroke == null) return 0;
        return markers.OutlineStroke?.Width ?? markers.StrokeWidth ?? familyWidth;
    }

    /// <summary>Design rule 5: a default marker sits on a 2px surface ring; an authored outline (even a zero width) or a bubble does not.</summary>
    internal static bool HasSurfaceRing(ChartSeries series, ChartMarkerOptions markers, bool hasBackdrop, double width) =>
        hasBackdrop && width <= 0 && !markers.StrokeWidth.HasValue && markers.OutlineStroke == null && series.Kind != ChartSeriesKind.Bubble;

    internal static double Extent(ChartSeries series, double radius, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        if (radius <= 0 || markers.Enabled == false) return 0;
        return ChartMarkerGeometry.Extent(markers, radius) + OutlineExtent(series, markers);
    }

    internal static ChartRect Bounds(double x, double y, double extent) => new(x - extent, y - extent, extent * 2, extent * 2);

    /// <summary>Resolves the actual fill for native marks and their contained text using the same override precedence.</summary>
    internal static (ChartColor Color, SvgPaint Paint) ResolveFill(ChartSeries series, int point, ChartColor fill, SvgPaint paint, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        var authoredPoint = point >= 0 && point < series.PointColors.Count && series.PointColors[point].HasValue;
        return markers.Fill.HasValue && !authoredPoint
            ? (markers.Fill.Value, VisualChartPaint.Series(series, markers.Fill.Value, explicitOverride: true)) : (fill, paint);
    }

    internal static void Draw(VisualSceneBuilder builder, ChartSeries series, int point, double x, double y, double radius,
        ChartColor fill, SvgPaint fillPaint, string role, ChartColor? stroke = null, double strokeWidth = 1,
        SvgPaint? strokePaint = null, ChartFillPattern pattern = ChartFillPattern.None, ChartColor? backdrop = null,
        string? patternRole = null, (ChartColor Color, SvgPaint Paint)? quantitativeOutline = null, ChartMarkerOptions? markerOptions = null) {
        if (radius <= 0 || !Enabled(series, markerOptions: markerOptions)) return;
        ResolvePaint(series, point, fill, fillPaint, role, stroke, strokeWidth, strokePaint, pattern, backdrop, patternRole, quantitativeOutline, markerOptions).Paint(builder, x, y, radius);
    }

    internal static NativeMarkerPaint CapturePaint(ChartSeries series, int point, ChartColor fill, SvgPaint fillPaint,
        string role, ChartFillPattern pattern, ChartColor backdrop, string patternRole, ChartMarkerOptions markers) =>
        ResolvePaint(series, point, fill, fillPaint, role, null, 1, null, pattern, backdrop, patternRole, null, markers).Detached();

    private static NativeMarkerPaint ResolvePaint(ChartSeries series, int point, ChartColor fill, SvgPaint fillPaint, string role,
        ChartColor? stroke, double strokeWidth, SvgPaint? strokePaint, ChartFillPattern pattern, ChartColor? backdrop,
        string? patternRole, (ChartColor Color, SvgPaint Paint)? quantitativeOutline, ChartMarkerOptions? markerOptions) {
        var markers = markerOptions ?? series.Markers;
        (fill, fillPaint) = ResolveFill(series, point, fill, fillPaint, markers);
        if (UsesQuantitativeOutline(series) && quantitativeOutline.HasValue) {
            stroke = quantitativeOutline.Value.Color;
            strokePaint = quantitativeOutline.Value.Paint;
            strokeWidth = FamilyStrokeWidth(series);
        }
        var width = StrokeWidth(series, stroke.HasValue, strokeWidth, markers);
        var outline = markers.OutlineStroke;
        var authoredStroke = outline?.Color ?? markers.Stroke;
        if (authoredStroke.HasValue) {
            stroke = authoredStroke.Value;
            strokePaint = VisualChartPaint.Series(series, stroke.Value, explicitOverride: true);
        } else if (width > 0 && !stroke.HasValue) {
            stroke = fill; strokePaint = fillPaint;
        }
        if (stroke.HasValue && outline != null) {
            var actual = ChartColorMath.WithOpacity(stroke.Value, outline.Opacity);
            strokePaint = (strokePaint ?? SvgPaint.Literal(stroke.Value)).WithOpacity(actual, outline.Opacity);
            stroke = actual;
        }
        if (width <= 0) { stroke = null; strokePaint = null; }
        var dash = outline?.ResolveDash(System.Array.Empty<double>());
        return new NativeMarkerPaint(markers, fill, stroke, width, new VisualScenePaintBinding(fillPaint, strokePaint),
            dash, role, pattern, backdrop ?? ChartColor.White, patternRole ?? role + "-pattern", ring: HasSurfaceRing(series, markers, backdrop.HasValue, width));
    }
}
