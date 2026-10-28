using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Projects finite source contour and label roles onto the existing native paint fields.</summary>
internal static class MermaidTopologySourcePaints {
    internal static void Apply(TopologyChart chart, MermaidDocument document, MermaidPresentationOptions? host) {
        if (document.Kind is not (MermaidDiagramKind.Flowchart or MermaidDiagramKind.Swimlane or MermaidDiagramKind.Agentflow)) return;
        var inputs = document.Configuration.Preset;
        var source = inputs?.Resolve(chart.ThemeMode, host?.Theme != null, document.Configuration.Theme);
        string? Role(string name) {
            if (inputs != null && inputs.TryGetAuthored(name, out var authored)) return authored.ToHexRgba();
            // Host themes suppress preset seeds in the canonical resolver. Authored
            // literals still precede every finite resolved named role.
            return source != null && source.TryGet(name, out var color) ? color.ToHexRgba() : null;
        }
        var background = Role("mainBkg"); var border = Role("nodeBorder"); var text = Role("nodeTextColor") ?? Role("textColor");
        var nativeTheme = chart.Theme.Resolve(chart.ThemeMode);
        var markerBackground = background ?? nativeTheme.Surface.ToHexRgba();
        var markerBorder = border ?? nativeTheme.Border.ToHexRgba();
        var cluster = Role("clusterBkg"); var clusterBorder = Role("clusterBorder"); var clusterText = Role("titleColor");
        var edgeBackground = Role("edgeLabelBackground");
        var nodeStyles = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var groupStyles = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        if (document is MermaidFlowchartDocument flowchart) {
            foreach (var node in flowchart.Nodes) nodeStyles.Add(node.Id, node.Styles);
            foreach (var group in flowchart.Subgraphs) groupStyles.Add(group.Id, group.Styles);
        } else if (document is MermaidAgentflowDocument agentflow) {
            foreach (var node in agentflow.Nodes) nodeStyles.Add(node.Id, node.Styles);
            foreach (var flow in agentflow.Flows) {
                if (flow.Collapsed) nodeStyles.Add(flow.Id, flow.Styles); else groupStyles.Add(flow.Id, flow.Styles);
            }
        }
        foreach (var node in chart.Nodes) {
            if (!nodeStyles.TryGetValue(node.Id, out var styles)) continue;
            var authoredFill = MermaidNativeStyles.Paint(styles, "fill"); var authoredStroke = MermaidNativeStyles.Paint(styles, "stroke");
            if (background != null && authoredFill == null && node.Shape != TopologyNodeShape.TextBlock) node.BackgroundColor = background;
            if (authoredStroke != null || border != null) node.BorderColor = authoredStroke ?? border;
            if (text != null && MermaidNativeStyles.Paint(styles, "color") == null) node.LabelColor = text;
            if (node.Shape is TopologyNodeShape.Browser or TopologyNodeShape.Console or TopologyNodeShape.Bucket && border != null) {
                node.Color = border; node.BorderColor = authoredStroke ?? border;
            }
            if (node.Shape is TopologyNodeShape.FilledCircle or TopologyNodeShape.FramedCircle or TopologyNodeShape.SynchronizationBar) {
                // These source paths style fill/stroke independently; native accent fill and contour are separate owners.
                // The filled-circle default has a literal border-colored fill. An authored
                // style replaces that literal; omitted fill then falls back to the surface.
                node.Color = authoredFill ?? (node.Shape == TopologyNodeShape.FilledCircle && styles.Count == 0 ? markerBorder : markerBackground);
                node.BorderColor = authoredStroke ?? markerBorder;
            }
            if (node.Shape == TopologyNodeShape.TextBlock) {
                // rect.text defaults to fill:none/stroke-width:0 upstream. Explicit visible
                // source paint reuses Rectangle; the retained Mermaid shape fact stays TextBlock.
                var outline = node.StrokeWidth.HasValue && (authoredStroke != null || border != null);
                if (authoredFill != null || outline) {
                    node.Shape = TopologyNodeShape.Rectangle; node.BackgroundColor = authoredFill ?? "none";
                    node.BorderColor = outline ? authoredStroke ?? border : "none";
                }
            }
            if (node.Metadata.TryGetValue("mermaid.asset.kind", out var kind) && kind == "icon") {
                var flow = (MermaidFlowchartDocument)document; var original = flow.Nodes.Find(item => item.Id == node.Id)!;
                var framed = original.Properties.TryGetValue("form", out var form) && form is "circle" or "square" or "rounded";
                if (authoredStroke != null || border != null) node.Color = authoredStroke ?? border;
                if (framed) {
                    if (authoredFill != null || background != null) node.BorderColor = authoredFill ?? background;
                } else {
                    // The pinned placeholder always overrides its source fill/stroke to none. The glyph uses stroke as color.
                    node.BackgroundColor = "none"; node.BorderColor = "none"; node.SurfaceOpacity = 0;
                }
            }
        }
        foreach (var group in chart.Groups) {
            if (!groupStyles.TryGetValue(group.Id, out var styles)) continue;
            // The pinned Agentflow flow-cluster has a later fill:none rule; clusterBkg does not color it.
            if (document.Kind != MermaidDiagramKind.Agentflow && cluster != null && MermaidNativeStyles.Paint(styles, "fill") == null) group.BackgroundColor = cluster;
            if (clusterBorder != null && MermaidNativeStyles.Paint(styles, "stroke") == null) group.BorderColor = clusterBorder;
            if (clusterText != null && MermaidNativeStyles.Paint(styles, "color") == null) group.LabelColor = clusterText;
        }
        if (text != null) foreach (var edge in chart.Edges)
            if (!edge.Metadata.ContainsKey("mermaid.style.color")) edge.LabelColor = text;
        var line = Role("defaultLinkColor") ?? Role("lineColor");
        if (line != null) foreach (var edge in chart.Edges)
            if (!edge.Metadata.ContainsKey("mermaid.style.stroke")) edge.Color = line;
        if (edgeBackground != null) {
            // Pinned script-free labels use one fill-only rect with element opacity0.5; alpha on its own fill is equivalent.
            var color = ChartColor.Parse(edgeBackground); var fill = color.WithOpacity(color.A / 255d * .5).ToHexRgba();
            foreach (var edge in chart.Edges) edge.LabelBackgroundColor = fill;
        }
    }
}
