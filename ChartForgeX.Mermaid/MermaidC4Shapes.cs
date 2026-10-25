using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Finite shape tokens accepted by the pinned C4 source adapter, lowered to existing native surfaces.</summary>
internal static class MermaidC4Shapes {
    internal static bool IsKeyword(string? value) => Shape(value).HasValue;
    internal static TopologyNodeShape? Resolve(string? shape, string? sprite, string? tags) {
        var explicitShape = Shape(shape) ?? Shape(sprite); if (explicitShape.HasValue) return explicitShape;
        if (tags != null) foreach (var tag in tags.Split(',')) { var tagged = Shape(tag.Trim()); if (tagged.HasValue) return tagged; }
        return null;
    }
    private static TopologyNodeShape? Shape(string? value) => value?.ToLowerInvariant() switch {
        "person" => TopologyNodeShape.Actor,
        "box" or "rounded" => TopologyNodeShape.Rounded,
        "cylinder" or "database" or "db" => TopologyNodeShape.Cylinder,
        "queue" or "pipe" => TopologyNodeShape.HorizontalCylinder,
        "component" => TopologyNodeShape.Subroutine,
        _ => null
    };
}
