using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Resolves marker paint once and emits the shared native geometry consumed by both static exporters.</summary>
internal static partial class VisualMarkerScene {
    internal static bool Enabled(ChartSeries series, bool familyDefault = true) => series.Markers.Enabled ?? familyDefault;

    internal static double Radius(ChartSeries series, VisualRenderContext context) {
        if (series.MarkerRadius.HasValue) return series.MarkerRadius.Value;
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
        ChartSeriesKind.Lollipop => ChartVisualPrimitives.LollipopMarkerStrokeWidth,
        ChartSeriesKind.Radar or ChartSeriesKind.Polar => 1,
        _ => 0
    };

    internal static double StrokeWidth(ChartSeries series, bool familyOutline, double familyWidth = 1) {
        if (!familyOutline && !series.Markers.Stroke.HasValue && !series.Markers.StrokeWidth.HasValue) return 0;
        return series.Markers.StrokeWidth ?? familyWidth;
    }

    internal static double Extent(ChartSeries series, double radius) {
        if (radius <= 0 || series.Markers.Enabled == false) return 0;
        var familyWidth = FamilyStrokeWidth(series);
        return radius + StrokeWidth(series, familyWidth > 0, familyWidth > 0 ? familyWidth : 1) / 2;
    }

    internal static ChartRect Bounds(double x, double y, double extent) => new(x - extent, y - extent, extent * 2, extent * 2);

    internal static void Draw(VisualSceneBuilder builder, ChartSeries series, int point, double x, double y, double radius,
        ChartColor fill, SvgPaint fillPaint, string role, ChartColor? stroke = null, double strokeWidth = 1,
        SvgPaint? strokePaint = null, ChartFillPattern pattern = ChartFillPattern.None, ChartColor? backdrop = null,
        string? patternRole = null) {
        if (radius <= 0 || !Enabled(series)) return;
        var authoredPoint = point >= 0 && point < series.PointColors.Count && series.PointColors[point].HasValue;
        if (series.Markers.Fill.HasValue && !authoredPoint) {
            fill = series.Markers.Fill.Value;
            fillPaint = VisualChartPaint.Series(series, fill, explicitOverride: true);
        }
        var width = StrokeWidth(series, stroke.HasValue, strokeWidth);
        if (series.Markers.Stroke.HasValue) {
            stroke = series.Markers.Stroke.Value;
            strokePaint = VisualChartPaint.Series(series, stroke.Value, explicitOverride: true);
        } else if (width > 0 && !stroke.HasValue) {
            stroke = fill; strokePaint = fillPaint;
        }
        if (width <= 0) { stroke = null; strokePaint = null; }
        var paint = new VisualScenePaintBinding(fillPaint, strokePaint);
        var contour = ChartMarkerGeometry.Path(series.Markers.Shape, x, y, radius);
        if (series.Markers.Shape == ChartMarkerShape.Circle)
            builder.Ellipse(x, y, radius, radius, fill, stroke, width, role, paint: paint);
        else builder.Path(contour, fill, stroke, width, role, close: true, paint: paint);
        if (pattern == ChartFillPattern.None) return;
        var background = ChartColorMath.Blend(backdrop ?? ChartColor.White, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d);
        var ink = ChartColorMath.AccessibleTextOnBackground(background).WithOpacity(ChartMarkSurface.HatchOpacity(pattern));
        builder.Pattern(contour, pattern, ink, role: patternRole ?? role + "-pattern");
    }
}
