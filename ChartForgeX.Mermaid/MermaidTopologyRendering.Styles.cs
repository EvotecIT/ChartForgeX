using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static void ApplyClassStyles(TopologyChart chart, MermaidClassDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var item in document.Classes) {
            var node = chart.Nodes.Find(candidate => candidate.Id == item.Id);
            if (node != null) ApplyNodeStyle(node, item.Classes, MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span));
        }
    }

    private static void ApplyEntityStyles(TopologyChart chart, MermaidEntityRelationshipDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var item in document.Groups) {
            var group = chart.Groups.Find(candidate => candidate.Id == item.Id);
            if (group == null) continue;
            if (item.Classes.Count > 0) group.Metadata["mermaid.classes"] = string.Join(",", item.Classes);
            var styles = MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span, includeDefault: false);
            foreach (var entry in styles) group.Metadata["mermaid.style." + entry.Key] = entry.Value;
            if (Paint("fill") is { } fill) group.BackgroundColor = fill;
            if (Paint("stroke") is { } stroke) group.BorderColor = stroke;
            if (Paint("color") is { } color) group.LabelColor = color;

            string? Paint(string property) => styles.TryGetValue(property, out var value) && MermaidStyleValues.IsColor(value) ? value : null;
        }
        foreach (var item in document.Entities) {
            var node = chart.Nodes.Find(candidate => candidate.Id == item.Id);
            if (node != null) ApplyNodeStyle(node, item.Classes, MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span));
        }
    }

    /// <summary>Maps supported native paints once for the SVG/PNG scene; unsupported declarations remain metadata.</summary>
    internal static void ApplyNodeStyle(TopologyNode node, List<string> classes, Dictionary<string, string> styles) {
        if (classes.Count > 0) node.Metadata["mermaid.classes"] = string.Join(",", classes);
        foreach (var entry in styles) node.Metadata["mermaid.style." + entry.Key] = entry.Value;
        if (Paint("fill") is { } fill) node.BackgroundColor = fill;
        if (Paint("stroke") is { } stroke) node.Color = stroke;
        if (Paint("color") is { } color) {
            node.LabelColor = color;
            foreach (var detail in node.Details) detail.Color = color;
        }

        string? Paint(string property) => styles.TryGetValue(property, out var value) && MermaidStyleValues.IsColor(value) ? value : null;
    }
}
