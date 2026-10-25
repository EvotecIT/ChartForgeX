using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSankeyCompiler {
    /// <summary>Uses the shared detached gradient scene; both export backends consume the same stops and ribbon contour.</summary>
    private static void Ribbon(VisualSceneBuilder builder, ChartSeries series, ChartSankeyOptions options, double nodeWidth,
        ChartSankeyNode source, ChartSankeyNode target, ChartSankeyLayoutLink link, ChartPath path,
        ChartRelationshipFlowPaint flow, VisualThemeColors colors) {
        series.FlowStyles.TryGetValue(link.Id, out var style);
        if (options.FlowColorMode != ChartSankeyFlowColorMode.Gradient || style.Fill.HasValue) {
            builder.Path(path, flow.Fill, flow.Stroke, flow.StrokeWidth, role: "sankey-ribbon", close: true,
                dash: flow.Dash, paint: flow.Binding, dashOffset: flow.DashOffset);
            return;
        }
        var end = ChartRelationshipPaint.Flow(series, link.Id, target.Index, colors, options.RibbonOpacity,
            options.RibbonFill, patternOpacity: .6);
        // Horizontal native DAG coordinates match the shared raster/SVG user-space gradient owner.
        // Opacity, explicit point/family/series color and flow-state precedence are resolved once per endpoint.
        builder.PathGradient(path, new ChartPoint(source.X + nodeWidth, 0), new ChartPoint(target.X, 0), new[] {
            new VisualGradientStop(0, flow.Fill, flow.FillPaint), new VisualGradientStop(1, end.Fill, end.FillPaint)
        }, flow.Stroke, flow.StrokeWidth, role: "sankey-ribbon", paint: flow.Binding,
            dash: flow.Dash, dashOffset: flow.DashOffset);
    }
}
