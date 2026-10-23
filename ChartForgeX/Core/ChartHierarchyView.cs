using System;

namespace ChartForgeX.Core;

/// <summary>Selects an existing hierarchy item as the root of a detached Treemap or Sunburst view.</summary>
/// <remarks>The source hierarchy and its identities, values and paint overrides remain unchanged. Pass null to preparation to restore the full hierarchy.</remarks>
public sealed class ChartHierarchyView {
    /// <summary>Creates a view using an exact, case-sensitive authored item identity.</summary>
    public ChartHierarchyView(string rootId) {
        if (string.IsNullOrWhiteSpace(rootId)) throw new ArgumentException("A hierarchy root identity is required.", nameof(rootId));
        RootId = rootId;
    }
    /// <summary>Gets the existing source item identity; it is never trimmed or relabeled.</summary>
    public string RootId { get; }
}
