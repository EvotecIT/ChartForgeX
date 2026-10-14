using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualExportRequest {
    internal static VisualExportRequest ForTopology(TopologyChart chart, TopologyRenderOptions options) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var theme = chart.Theme;
        var colors = theme.Resolve(chart.ThemeMode);
        // A convenience export can grow a tiny authored canvas after canonical layout. Its initial
        // padding must still leave a positive plot; explicit Prepare(context) retains strict validation.
        var padding = Math.Min(chart.Viewport.Padding, Math.Max(0, Math.Min(chart.Viewport.Width, chart.Viewport.Height) / 2 - 1));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(chart.Viewport.Width, chart.Viewport.Height), padding),
            theme, chart.ThemeMode, frame: new VisualFrame(showLegend: options.IncludeLegend, transparentBackground: colors.Background.A == 0),
            font: theme.Typography.ToFontSpec());
        return new VisualExportRequest(context, new VisualRenderOptions(options.PngOutputScale, options.PngSupersamplingScale));
    }

    internal static VisualExportRequest ForFlow(FlowArtifact flow) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        return new VisualExportRequest(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(flow.Width, flow.Height), flow.Padding),
            frame: new VisualFrame(showLegend: false)), new VisualRenderOptions());
    }
}
