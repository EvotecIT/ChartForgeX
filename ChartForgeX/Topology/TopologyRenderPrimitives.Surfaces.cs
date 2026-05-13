namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static bool ShouldRenderCanvasSurface(TopologyChart chart, TopologyRenderOptions options) =>
        chart.LayoutMode != TopologyLayoutMode.Geographic && options.CanvasSurfaceStyle != TopologyCanvasSurfaceStyle.Plain;

    public static double CanvasSurfaceInset(TopologyChart chart, TopologyRenderOptions options) =>
        ShouldRenderCanvasSurface(chart, options) ? 18 : 0;
}
