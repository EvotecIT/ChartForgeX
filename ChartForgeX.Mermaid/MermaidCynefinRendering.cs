using System;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Maps complexity domains to a deterministic domain diagram with visible items and transitions.</summary>
public static class MermaidCynefinRendering {
    /// <summary>Builds the four fixed domains and the central confusion region.</summary>
    public static TopologyChart ToTopologyChart(this MermaidCynefinDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = TopologyChart.Create().WithId(options.Id ?? "mermaid-cynefin")
            .WithTitle(options.Title ?? document.Title ?? "Cynefin").WithViewport(options.Width, options.Height, options.Padding);
        var domainNames = new[] { "complex", "complicated", "chaotic", "clear", "confusion" };
        var width = Math.Max(960, options.Width);
        var maxItems = 0;
        foreach (var items in document.Domains.Values) maxItems = Math.Max(maxItems, items.Count);
        var cardHeight = Math.Max(170, 78 + maxItems * 18);
        var centerItems = document.Domains.TryGetValue("confusion", out var confusion) ? confusion.Count : 0;
        var centerHeight = Math.Max(96, 70 + centerItems * 18);
        var gap = centerHeight + 24;
        var height = Math.Max(options.Height, 140 + cardHeight * 2 + gap);
        chart.WithViewport(width, height, options.Padding);
        for (var i = 0; i < domainNames.Length; i++) {
            var name = domainNames[i];
            var center = i == 4;
            var nodeWidth = center ? 260 : (width - 100) / 2;
            var x = center ? (width - nodeWidth) / 2 : i % 2 == 0 ? 32 : width / 2 + 18;
            var y = center ? 112 + cardHeight : 100 + (i / 2) * (cardHeight + gap);
            chart.AddNode(name, char.ToUpperInvariant(name[0]) + name.Substring(1), x, y, width: nodeWidth, height: center ? centerHeight : cardHeight);
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.ShowStatusBadge = false;
            node.Shape = center ? TopologyNodeShape.Ellipse : TopologyNodeShape.Rectangle;
            node.PreserveDisplayModeSize = true;
            if (document.Domains.TryGetValue(name, out var items)) foreach (var item in items) node.Details.Add(new TopologyNodeDetail { Text = item });
        }
        for (var i = 0; i < document.Transitions.Count; i++) {
            var transition = document.Transitions[i];
            chart.AddEdge("transition-" + i, transition.SourceId, transition.TargetId, transition.Label, direction: VisualLinkDirection.Forward);
        }
        return MermaidPresentation.Apply(chart, document);
    }

    /// <summary>Produces a static domain-map artifact.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidCynefinDocument document, MermaidTopologyRenderOptions? options = null) {
        var artifact = document.ToTopologyChart(options).ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        return MermaidPresentation.Apply(artifact, document);
    }
}
