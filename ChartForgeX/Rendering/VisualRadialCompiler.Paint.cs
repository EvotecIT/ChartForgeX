using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static ChartColor SlicePaintColor(Chart chart, ChartColor source) => chart.Options.PieSliceOpacity.HasValue
        ? ChartColorMath.WithOpacity(source, chart.Options.PieSliceOpacity.Value) : source;

    /// <summary>Keeps ordinary sharp paint exact; authored dash lengths reuse the existing shared path painter.</summary>
    private static void PaintSlice(Chart chart, VisualSceneBuilder builder, ChartSeries series, RadialSlice slice,
        double cx, double cy, double outer, double inner, double start, double sweep, string role, VisualThemeColors colors) {
        var policy = chart.Options.PieSliceOutline;
        if (policy == null && !chart.Options.PieSliceOpacity.HasValue) {
            builder.Slice(cx, cy, outer, inner, start, sweep, slice.Color, colors.Surface, 2, role, SliceId(slice),
                paint: new VisualScenePaintBinding(VisualChartPaint.Series(series, slice.Color, slice.PointIndex), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
            return;
        }
        var stroke = VisualReferenceStroke.Paint(policy, colors.Surface, SvgColorRole.Surface, out var sourceStroke);
        var color = SlicePaintColor(chart, sourceStroke); var fillColor = SlicePaintColor(chart, slice.Color);
        var width = VisualReferenceStroke.PaintedWidth(policy, colors.Surface, 2);
        var fill = VisualChartPaint.Series(series, slice.Color, slice.PointIndex);
        if (chart.Options.PieSliceOpacity.HasValue) {
            var opacity = chart.Options.PieSliceOpacity.Value;
            fill = fill.WithOpacity(fillColor, opacity);
            stroke = new VisualScenePaintBinding(stroke: stroke.Stroke?.WithOpacity(color, opacity));
        }
        if (policy?.Dash == null || policy.Dash.Count == 0) {
            builder.Slice(cx, cy, outer, inner, start, sweep, fillColor, color, width, role, SliceId(slice),
                paint: new VisualScenePaintBinding(fill, stroke.Stroke));
        } else {
            builder.Slice(cx, cy, outer, inner, start, sweep, fillColor, null, 0, role, SliceId(slice),
                paint: new VisualScenePaintBinding(fill));
            builder.Path(ChartSlicePathGeometry.OutlinePath(cx, cy, outer, inner, start, sweep), stroke: color,
                strokeWidth: width, role: "radial-slice-outline", close: true, dash: policy.ResolveDash(Array.Empty<double>()),
                cap: VisualStrokeCap.Butt, paint: stroke);
        }
    }

    private static void PaintOuterOutline(Chart chart, VisualSceneBuilder builder, double cx, double cy,
        double radius, VisualThemeColors colors) {
        var policy = chart.Options.PieOuterOutline;
        if (policy == null) return;
        var paint = VisualReferenceStroke.Paint(policy, colors.Surface, SvgColorRole.Surface, out var color);
        var width = VisualReferenceStroke.PaintedWidth(policy, colors.Surface, 2);
        if (policy.Dash == null || policy.Dash.Count == 0)
            builder.Ellipse(cx, cy, radius, radius, null, color, width, "radial-outer-outline", paint: paint);
        else builder.Path(ChartSlicePathGeometry.OutlinePath(cx, cy, radius, 0, -Math.PI / 2, Math.PI * 2),
            stroke: color, strokeWidth: width, role: "radial-outer-outline", close: true,
            dash: policy.ResolveDash(Array.Empty<double>()), cap: VisualStrokeCap.Butt, paint: paint);
    }
}
