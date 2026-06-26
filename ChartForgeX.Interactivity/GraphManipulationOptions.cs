namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes opt-in editing capabilities for graph adapters without making static rendering mutable by default.
/// </summary>
public sealed class GraphManipulationOptions {
    /// <summary>Gets or sets whether users can create nodes in the adapter.</summary>
    public bool CanAddNodes { get; set; }

    /// <summary>Gets or sets whether users can edit node labels, metadata, or visual hints in the adapter.</summary>
    public bool CanEditNodes { get; set; }

    /// <summary>Gets or sets whether users can delete nodes in the adapter.</summary>
    public bool CanDeleteNodes { get; set; }

    /// <summary>Gets or sets whether users can create edges between existing nodes in the adapter.</summary>
    public bool CanAddEdges { get; set; }

    /// <summary>Gets or sets whether users can edit edge labels, metadata, or visual hints in the adapter.</summary>
    public bool CanEditEdges { get; set; }

    /// <summary>Gets or sets whether users can delete edges in the adapter.</summary>
    public bool CanDeleteEdges { get; set; }

    /// <summary>Gets or sets whether adapters can move a collapsed group or cluster as one unit.</summary>
    public bool CanDragGroups { get; set; }

    /// <summary>Gets or sets whether adapters can export user-adjusted node positions for host persistence.</summary>
    public bool CanPersistPositions { get; set; }

    /// <summary>
    /// Enables the common edit surface used by graph explorers that support topology authoring or repair workflows.
    /// </summary>
    /// <returns>The current options instance.</returns>
    public GraphManipulationOptions EnableEditing() {
        CanAddNodes = true;
        CanEditNodes = true;
        CanDeleteNodes = true;
        CanAddEdges = true;
        CanEditEdges = true;
        CanDeleteEdges = true;
        CanDragGroups = true;
        CanPersistPositions = true;
        return this;
    }
}
