using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>Internal display geometry retained by optional producers without owning topology layout or routing.</summary>
internal sealed class ResolvedTopologyGeometry {
    internal static ResolvedTopologyGeometry Create(TopologyChart chart, TopologyRenderOptions options,
        VisualRenderContext context, ChartRect clip, double scale, double offsetX, double offsetY, ChartColor background) {
        ChartPoint Transform(ChartPoint point) => new(offsetX + point.X * scale, offsetY + point.Y * scale);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var routes = chart.Edges.ToDictionary(edge => edge, edge => (IReadOnlyList<ChartPoint>)Array.AsReadOnly(
            TopologyResolvedRouteSamples.Sample(chart, options, edge, nodes,
                TopologyRenderPrimitives.EdgePoints(chart, edge, nodes)).Select(Transform).ToArray()));
        var centers = chart.Nodes.Where(node =>
                TopologyRenderPrimitives.EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
            .ToDictionary(node => node.Id,
                node => Transform(new ChartPoint(node.X + node.Width / 2, node.Y + node.Height / 2)), StringComparer.Ordinal);
        return new ResolvedTopologyGeometry(chart, options, context, routes, centers, clip, scale, background,
            chart.Theme ?? TopologyTheme.Light());
    }

    internal ResolvedTopologyGeometry(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context,
        IReadOnlyDictionary<TopologyEdge, IReadOnlyList<ChartPoint>> routes,
        IReadOnlyDictionary<string, ChartPoint> nodeCenters, ChartRect clip, double scale,
        ChartColor background, TopologyTheme theme) {
        Chart = chart; Options = options; Context = context; Routes = routes; NodeCenters = nodeCenters;
        Clip = clip; Scale = scale; Background = background; Theme = theme;
    }

    internal TopologyChart Chart { get; }
    internal TopologyRenderOptions Options { get; }
    internal VisualRenderContext Context { get; }
    internal IReadOnlyDictionary<TopologyEdge, IReadOnlyList<ChartPoint>> Routes { get; }
    internal IReadOnlyDictionary<string, ChartPoint> NodeCenters { get; }
    internal ChartRect Clip { get; }
    internal double Scale { get; }
    internal ChartColor Background { get; }
    internal TopologyTheme Theme { get; }

    internal ChartColor ResolveColor(string? authored, TopologyHealthStatus status) {
        var fallback = ChartColor.Parse(Theme.StatusColor(status));
        return string.IsNullOrWhiteSpace(authored) ? fallback
            : SvgRaster.SvgRasterColor.TryParse(authored, out var parsed) ? parsed
            : SvgPaint.TryCssVariable(authored, fallback, out var resolved, out _) ? resolved : ChartColor.Parse(authored!);
    }

    internal string PaintColor(ChartColor color, SvgColorRole role) =>
        Options.SvgColorVariables is SvgColorVariables variables && variables.TryPaint(color, role, out var paint)
            ? paint : color.ToCss();
}
