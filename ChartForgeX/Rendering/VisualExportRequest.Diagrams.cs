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
        var source = chart.Theme ?? TopologyTheme.Light();
        var tokens = VisualTheme.Graphite().Resolve(VisualThemeMode.Light).ToTokens();
        tokens.Background = ParseTopologyColor(source.Background); tokens.Surface = ParseTopologyColor(source.Card);
        tokens.ElevatedSurface = ParseTopologyColor(source.Surface); tokens.Foreground = ParseTopologyColor(source.Foreground);
        tokens.MutedForeground = ParseTopologyColor(source.MutedForeground); tokens.Border = ParseTopologyColor(source.Border);
        tokens.Accent = ParseTopologyColor(source.Accent); tokens.FontFamily = source.FontFamily;
        tokens.Status.Pass = Pair(ParseTopologyColor(source.Healthy), tokens.Status.Pass);
        tokens.Status.Medium = Pair(ParseTopologyColor(source.Warning), tokens.Status.Medium);
        tokens.Status.Critical = Pair(ParseTopologyColor(source.Critical), tokens.Status.Critical);
        tokens.Status.Neutral = Pair(ParseTopologyColor(source.Unknown), tokens.Status.Neutral);
        tokens.Status.Maintenance = Pair(ParseTopologyColor(source.Disabled), tokens.Status.Maintenance);
        var theme = new VisualTheme(tokens, tokens, new VisualTypography(source.FontFamily));
        // A convenience export can grow a tiny authored canvas after canonical layout. Its initial
        // padding must still leave a positive plot; explicit Prepare(context) retains strict validation.
        var padding = Math.Min(chart.Viewport.Padding, Math.Max(0, Math.Min(chart.Viewport.Width, chart.Viewport.Height) / 2 - 1));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(chart.Viewport.Width, chart.Viewport.Height), padding),
            theme, frame: new VisualFrame(showLegend: options.IncludeLegend, transparentBackground: tokens.Background.A == 0),
            font: new FontSpec { Family = source.FontFamily });
        return new VisualExportRequest(context, new VisualRenderOptions(options.PngOutputScale, options.PngSupersamplingScale));
    }

    private static ChartColor ParseTopologyColor(string value) => ChartForgeX.SvgRaster.SvgRasterColor.TryParse(value, out var parsed)
        ? parsed : ChartColor.Parse(value);

    internal static VisualExportRequest ForFlow(FlowArtifact flow) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        return new VisualExportRequest(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(flow.Width, flow.Height), flow.Padding),
            frame: new VisualFrame(showLegend: false)), new VisualRenderOptions());
    }
}
