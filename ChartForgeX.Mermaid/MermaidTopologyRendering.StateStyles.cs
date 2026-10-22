using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    /// <summary>Uses the shared safe paint cascade without styling synthetic regions or attached-note surfaces.</summary>
    private static void ApplyStateStyles(TopologyChart chart, MermaidStateDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var state in document.States) {
            var group = chart.Groups.Find(candidate => candidate.Id == state.Id);
            if (group != null) {
                var styles = MermaidStyleParser.Resolve(definitions, state.Classes, state.Styles, state.Span, includeDefault: false);
                if (state.Classes.Count > 0) group.Metadata["mermaid.classes"] = string.Join(",", state.Classes);
                foreach (var entry in styles) group.Metadata["mermaid.style." + entry.Key] = entry.Value;
                if (Paint("fill") is { } fill) group.BackgroundColor = fill;
                if (Paint("stroke") is { } stroke) group.BorderColor = stroke;
                if (Paint("color") is { } color) group.LabelColor = color;

                string? Paint(string property) => styles.TryGetValue(property, out var value) && MermaidStyleValues.IsColor(value) ? value : null;
            } else {
                var node = chart.Nodes.Find(candidate => candidate.Id == state.Id);
                if (node != null) ApplyNodeStyle(node, state.Classes,
                    MermaidStyleParser.Resolve(definitions, state.Classes, state.Styles, state.Span, includeDefault: !IsPseudoState(state)));
            }
        }
    }
}
