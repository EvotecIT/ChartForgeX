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
            var styles = MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span, includeDefault: false);
            MermaidNativeStyles.Group(group,item.Classes,styles);
        }
        foreach (var item in document.Entities) {
            var node = chart.Nodes.Find(candidate => candidate.Id == item.Id);
            if (node != null) ApplyNodeStyle(node, item.Classes, MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span));
        }
    }

    /// <summary>Maps supported native paints once for the SVG/PNG scene; unsupported declarations remain metadata.</summary>
    internal static void ApplyNodeStyle(TopologyNode node, List<string> classes, Dictionary<string, string> styles) => MermaidNativeStyles.Node(node,classes,styles);
}
