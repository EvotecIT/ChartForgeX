using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>Captures a detached organization hierarchy's collapsed nodes, sibling tie order and optional existing layout direction.</summary>
/// <remarks>Identities are exact and case-sensitive. The source chart and parent relationships are retained. Null preparation restores the complete hierarchy.</remarks>
public sealed class TopologyHierarchyView {
    /// <summary>Creates a copied view without changing any node or assigning aggregate identities.</summary>
    /// <param name="collapsedNodeIds">Existing hierarchy owners whose descendants are hidden.</param>
    /// <param name="nodeOrder">Existing node IDs in sibling tie order. Unlisted siblings retain the existing identifier tie order.</param>
    /// <param name="layoutDirection">An optional native layered direction; no browser layout math is used.</param>
    public TopologyHierarchyView(IEnumerable<string>? collapsedNodeIds = null, IEnumerable<string>? nodeOrder = null, TopologyLayoutDirection? layoutDirection = null) {
        if (layoutDirection.HasValue) TopologyModelGuards.EnumDefined(layoutDirection.Value, nameof(layoutDirection));
        CollapsedNodeIds = Capture(collapsedNodeIds, nameof(collapsedNodeIds));
        NodeOrder = Capture(nodeOrder, nameof(nodeOrder)); LayoutDirection = layoutDirection;
    }
    /// <summary>Gets the exact collapsed source node IDs, including a collapsed child retained beneath a collapsed ancestor.</summary>
    public IReadOnlyList<string> CollapsedNodeIds { get; }
    /// <summary>Gets copied sibling tie order; parent order remains authoritative.</summary>
    public IReadOnlyList<string> NodeOrder { get; }
    /// <summary>Gets the selected native direction, or null to retain the authored direction.</summary>
    public TopologyLayoutDirection? LayoutDirection { get; }
    private static IReadOnlyList<string> Capture(IEnumerable<string>? values, string name) {
        if (values == null) return Array.Empty<string>();
        var captured = values.ToArray(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in captured)
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value)) throw new ArgumentException("Hierarchy view IDs must be nonblank and unique.", name);
        return Array.AsReadOnly(captured);
    }
}
