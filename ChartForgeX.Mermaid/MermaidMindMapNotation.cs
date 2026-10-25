using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Keeps finite mindmap source notation separate from the shared native geometry and icon owners.</summary>
internal static class MermaidMindMapNotation {
    // This catalog is private and read only after construction. Host catalogs cannot execute source icon tokens.
    private static readonly TopologyIconCatalog BuiltIns = TopologyIconCatalog.Default();

    internal static string? BuiltInIcon(string token) {
        var separator = token.IndexOf(':');
        if (separator <= 0 || separator >= token.Length - 1 || token.IndexOf(':', separator + 1) >= 0) return null;
        return BuiltIns.Resolve(token)?.QualifiedId;
    }

    internal static TopologyNodeShape Shape(string? source) => source switch {
        "square" => TopologyNodeShape.Rectangle,
        "rounded" => TopologyNodeShape.Rounded,
        "circle" => TopologyNodeShape.Circle,
        "hexagon" => TopologyNodeShape.Hexagon,
        "cloud" => TopologyNodeShape.Cloud,
        "bang" => TopologyNodeShape.Burst,
        _ => TopologyNodeShape.Rounded
    };
}
