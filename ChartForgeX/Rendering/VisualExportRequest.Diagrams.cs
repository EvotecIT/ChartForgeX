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
        tokens.Background = ChartColor.Parse(source.Background); tokens.Surface = ChartColor.Parse(source.Card);
        tokens.ElevatedSurface = ChartColor.Parse(source.Surface); tokens.Foreground = ChartColor.Parse(source.Foreground);
        tokens.MutedForeground = ChartColor.Parse(source.MutedForeground); tokens.Border = ChartColor.Parse(source.Border);
        tokens.Accent = ChartColor.Parse(source.Accent); tokens.FontFamily = source.FontFamily;
        tokens.Status.Pass = Pair(ChartColor.Parse(source.Healthy), tokens.Status.Pass);
        tokens.Status.Medium = Pair(ChartColor.Parse(source.Warning), tokens.Status.Medium);
        tokens.Status.Critical = Pair(ChartColor.Parse(source.Critical), tokens.Status.Critical);
        tokens.Status.Neutral = Pair(ChartColor.Parse(source.Unknown), tokens.Status.Neutral);
        tokens.Status.Maintenance = Pair(ChartColor.Parse(source.Disabled), tokens.Status.Maintenance);
        var theme = new VisualTheme(tokens, tokens, new VisualTypography(source.FontFamily));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(chart.Viewport.Width, chart.Viewport.Height), chart.Viewport.Padding),
            theme, frame: new VisualFrame(showLegend: options.IncludeLegend, transparentBackground: tokens.Background.A == 0),
            font: new FontSpec { Family = source.FontFamily });
        return new VisualExportRequest(context, new VisualRenderOptions(options.PngOutputScale, options.PngSupersamplingScale));
    }

    internal static VisualExportRequest ForFlow(FlowArtifact flow) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        return new VisualExportRequest(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(flow.Width, flow.Height), flow.Padding),
            frame: new VisualFrame(showLegend: false)), new VisualRenderOptions());
    }
}
