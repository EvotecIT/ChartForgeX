using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Separates a visible prepared organization projection from its complete raw authored topology.</summary>
/// <remarks>The source envelope is not prepared geometry. Its layout mode, positions, size policy and exact identities
/// remain authored facts. It cannot itself contain another hierarchy source. Existing portable limits apply cumulatively.</remarks>
public sealed class VisualArtifactInterchangeTopologyHierarchy {
    /// <summary>Gets or sets the complete raw authored graph in the existing typed topology format.</summary>
    public VisualArtifactInterchangeEnvelope Source { get; set; } = new();
    /// <summary>Gets or sets the original optional chart identifier, before portable identity projection.</summary>
    public string? SourceId { get; set; }
    /// <summary>Gets or sets the raw source viewport padding.</summary>
    public double SourcePadding { get; set; }
    /// <summary>Gets copied collapsed source node IDs.</summary>
    public List<string> CollapsedNodeIds { get; } = new();
    /// <summary>Gets copied sibling tie order; existing parent order remains authoritative.</summary>
    public List<string> NodeOrder { get; } = new();
    /// <summary>Gets or sets an optional requested direction, independently of the raw source direction.</summary>
    public TopologyLayoutDirection? LayoutDirection { get; set; }
    /// <summary>Gets raw source node identity and automatic-size/position state, in source order.</summary>
    public List<VisualArtifactInterchangeTopologySourceNode> SourceNodes { get; } = new();
    /// <summary>Gets raw source group identity and automatic-position state, in source order.</summary>
    public List<VisualArtifactInterchangeTopologySourceGroup> SourceGroups { get; } = new();
    /// <summary>Gets raw edge identities in source order.</summary>
    public List<VisualArtifactInterchangeTopologySourceIdentity> SourceEdges { get; } = new();
    /// <summary>Gets raw scenario identities in source order.</summary>
    public List<VisualArtifactInterchangeTopologySourceIdentity> SourceScenarios { get; } = new();
    /// <summary>Gets raw alignment identities in authored order; omitted from JSON when empty.</summary>
    public List<VisualArtifactInterchangeTopologySourceIdentity> SourceAlignments { get; } = new();
    /// <summary>Gets raw annotation identities in source order.</summary>
    public List<VisualArtifactInterchangeTopologySourceIdentity> SourceAnnotations { get; } = new();
}

/// <summary>Retains a raw authored identity independently of its unique portable ID.</summary>
public class VisualArtifactInterchangeTopologySourceIdentity {
    /// <summary>Gets or sets the typed portable entity ID.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact raw authored ID.</summary>
    public string SourceId { get; set; } = string.Empty;
    /// <summary>Gets or sets authored host CSS tokens; they are not prepared paint.</summary>
    public string? CssClass { get; set; }
}

/// <summary>Retains a raw node's automatic layout policy, independently of prepared geometry.</summary>
public sealed class VisualArtifactInterchangeTopologySourceNode : VisualArtifactInterchangeTopologySourceIdentity {
    /// <summary>Gets or sets whether the raw source explicitly assigned its position.</summary>
    public bool HasPositionOverride { get; set; }
    /// <summary>Gets or sets the raw display-mode sizing policy.</summary>
    public bool PreserveDisplayModeSize { get; set; }
    /// <summary>Gets or sets the nullable authored display mode, before global presentation defaults.</summary>
    public TopologyNodeDisplayMode? DisplayMode { get; set; }
    /// <summary>Gets or sets the authored status-badge flag, before presentation filtering.</summary>
    public bool ShowStatusBadge { get; set; }
    /// <summary>Gets raw port identities in source order.</summary>
    public List<string> PortIds { get; } = new();
}

/// <summary>Retains a raw group's automatic position state.</summary>
public sealed class VisualArtifactInterchangeTopologySourceGroup : VisualArtifactInterchangeTopologySourceIdentity {
    /// <summary>Gets or sets whether the raw source explicitly assigned its position.</summary>
    public bool HasPositionOverride { get; set; }
}
