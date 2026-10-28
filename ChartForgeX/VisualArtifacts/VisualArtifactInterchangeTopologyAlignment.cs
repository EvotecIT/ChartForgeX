using System;
using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>One ordered native global row or column, retained independently of resolved geometry.</summary>
public sealed class VisualArtifactInterchangeTopologyAlignment {
    /// <summary>Gets or sets the unique portable declaration identity.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets global row/column semantics.</summary>
    public TopologyAlignmentKind Kind { get; set; }
    /// <summary>Gets ordered portable node IDs. Every member references a real node.</summary>
    public List<string> NodeIds { get; } = new();
    /// <summary>Gets bounded inert source and host facts.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}
