using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes reusable parent-child navigation behavior for graph explorer adapters.
/// </summary>
public sealed class GraphHierarchyOptions {
    private string? _initialRootNodeId;

    /// <summary>Gets or sets the node initially used as the root of a drill-down view. Null starts at the complete graph.</summary>
    public string? InitialRootNodeId { get => _initialRootNodeId; set => _initialRootNodeId = ChartInteractionText.OptionalToken(value, nameof(value), "Graph hierarchy root ids"); }

    /// <summary>Gets or sets the maximum descendant depth shown below the active root. Zero shows only the root.</summary>
    public int InitialDepth { get; set; } = 2;

    /// <summary>Gets or sets whether breadcrumb controls should retain the active root's ancestor chain.</summary>
    public bool IncludeAncestorBreadcrumbs { get; set; } = true;

    /// <summary>Gets or sets whether adapters should fit the visible graph after hierarchy navigation.</summary>
    public bool AutoFitOnNavigate { get; set; } = true;

    /// <summary>Gets or sets whether a node activation gesture may enter a child view.</summary>
    public bool DrillDownOnActivate { get; set; } = true;

    internal void Validate() {
        if (InitialDepth < 0) throw new InvalidOperationException("Graph scene hierarchy depth must not be negative.");
    }
}
