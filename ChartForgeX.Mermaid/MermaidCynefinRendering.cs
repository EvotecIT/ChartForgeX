using System;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Maps complexity domains to one native domain scene with visible items, boundaries and transitions.</summary>
public static partial class MermaidCynefinRendering {
    /// <summary>Builds the fixed domains, organic fold, cliff and central confusion overlay.</summary>
    /// <remarks>The native width floor and measured all-item layout remain in force. Source dimensions are lower bounds,
    /// rather than a request to truncate authored items. Boundary geometry follows the pinned family equations.</remarks>
    public static TopologyChart ToTopologyChart(this MermaidCynefinDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var settings = document.Configuration.Cynefin;
        var width = Math.Max(960, settings?.Number("width", options.Width) ?? options.Width);
        var padding = settings?.Number("padding", options.Padding) ?? options.Padding;
        var maxItems = 0;
        foreach (var items in document.Domains.Values) maxItems = Math.Max(maxItems, items.Count);
        var centerItems = document.Domains.TryGetValue("confusion", out var confusion) ? confusion.Count : 0;
        var descriptions = settings?.ShowDomainDescriptions ?? true;
        var cardHeight = Math.Max(170, (descriptions ? 110 : 78) + maxItems * 30);
        var centerHeight = Math.Max(96, (descriptions ? 90 : 70) + centerItems * 30);
        var regionHeight = Math.Max(cardHeight / .34, centerHeight / .30);
        var height = Math.Max(settings?.Number("height", options.Height) ?? options.Height, 100 + regionHeight + padding * 2);
        regionHeight = height - 100 - padding * 2;
        // A large requested padding cannot invert the authored region or create invalid native node bounds.
        width = Math.Max(width, padding * 2 + 240);
        var regionWidth = width - padding * 2;
        var chart = TopologyChart.Create().WithId(options.Id ?? "mermaid-cynefin")
            .WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "Cynefin")).WithViewport(width, height, padding);
        MermaidPresentation.Apply(chart, document, options.Presentation);
        chart.DefaultRenderOptions!.AllowMultilineNodeLabels = true;
        if (settings?.UseMaxWidth is bool responsive) chart.DefaultRenderOptions.UseResponsiveSvg = responsive;
        var colors = chart.Theme.Resolve(chart.ThemeMode);
        settings?.ApplyTitleColor(chart);
        ApplyTitleTypography(chart, settings, options);
        var domainNames = new[] { "complex", "complicated", "chaotic", "clear", "confusion" };
        AddDomainBackgrounds(chart, settings, padding, 100 + padding, regionWidth, regionHeight);
        AddBoundaries(chart, settings, padding, 100 + padding, regionWidth, regionHeight);
        AddConfusionBackground(chart, settings, padding, 100 + padding, regionWidth, regionHeight);
        for (var i = 0; i < domainNames.Length; i++) {
            var name = domainNames[i]; var center = i == 4;
            var nodeWidth = regionWidth * (center ? .30 : .42);
            var nodeHeight = regionHeight * (center ? .30 : .34);
            var cx = padding + regionWidth * (center ? .50 : i % 2 == 0 ? .25 : .75);
            var cy = 100 + padding + regionHeight * (center ? .50 : i / 2 == 0 ? .23 : .77);
            chart.AddNode(name, char.ToUpperInvariant(name[0]) + name.Substring(1), cx - nodeWidth / 2, cy - nodeHeight / 2,
                width: nodeWidth, height: nodeHeight);
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.ShowStatusBadge = false;
            node.Shape = center ? TopologyNodeShape.Ellipse : TopologyNodeShape.Rectangle;
            node.PreserveDisplayModeSize = true;
            node.FitLabelToContent = true;
            node.Color = "transparent"; node.BackgroundColor = "transparent";
            node.LabelColor = settings?.Color("labelColor", colors.Foreground).ToHexRgba() ?? colors.Foreground.ToHexRgba();
            if (descriptions) node.Subtitle = Description(name);
            if (document.Domains.TryGetValue(name, out var items)) foreach (var item in items)
                node.Details.Add(new TopologyNodeDetail { Text = item, Color = settings?.Color("textColor", colors.Foreground).ToHexRgba() ?? colors.Foreground.ToHexRgba(),
                    BadgeStyle = new TopologyDetailBadgeStyle {
                        Fill = DomainBackground(chart, settings, name, i, .95),
                        Stroke = settings?.Color("boundaryColor", colors.Border) ?? colors.Border
                    } });
        }
        foreach (var node in chart.Nodes) ApplyDomainTypography(chart, node, settings, options);
        for (var i = 0; i < document.Transitions.Count; i++) {
            var transition = document.Transitions[i];
            chart.AddEdge("transition-" + i, transition.SourceId, transition.TargetId, transition.Label, direction: VisualLinkDirection.Forward);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.Color = settings?.Color("arrowColor", colors.Axis).ToHexRgba() ?? colors.Axis.ToHexRgba();
            edge.LabelColor = settings?.Color("textColor", colors.Foreground).ToHexRgba() ?? colors.Foreground.ToHexRgba();
            edge.StrokeWidth = settings?.Number("arrowWidth", 2) ?? 2;
            ApplyTransitionTypography(edge, settings, options);
            ApplyTransitionPath(chart, edge);
        }
        return chart;
    }

    private static string Description(string domain) => domain switch {
        "complex" => "Probe → Sense → Respond\nEmergent Practices",
        "complicated" => "Sense → Analyse → Respond\nGood Practices",
        "chaotic" => "Act → Sense → Respond\nNovel Practices",
        "clear" => "Sense → Categorise → Respond\nBest Practices",
        _ => "Disorder"
    };

    /// <summary>Produces the shared native scene and its typed version-2 portable capture.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidCynefinDocument document, MermaidTopologyRenderOptions? options = null) {
        var artifact = document.ToTopologyChart(options).ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        return MermaidPresentation.Apply(artifact, document);
    }
}
