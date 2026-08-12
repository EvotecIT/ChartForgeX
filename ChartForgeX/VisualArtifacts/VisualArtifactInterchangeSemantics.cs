using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Identifies the semantic diagram family carried by an interchange envelope.</summary>
public enum VisualArtifactInterchangeFamily {
    /// <summary>No structured diagram family is available; consumers should use a rendered fallback.</summary>
    None,
    /// <summary>Topology groups, nodes, edges, presentation, and scenarios.</summary>
    Topology,
    /// <summary>Flow lanes, steps, and connectors.</summary>
    Flow,
    /// <summary>Sequence participants, messages, activations, notes, and blocks.</summary>
    Sequence
}

/// <summary>Identifies the stable role of an interchange group without closing the free-form kind token.</summary>
public enum VisualArtifactInterchangeGroupRole {
    /// <summary>No well-known role is assigned.</summary>
    Unspecified,
    /// <summary>A topology group or container.</summary>
    TopologyGroup,
    /// <summary>A flow lane.</summary>
    FlowLane
}

/// <summary>Identifies the stable role of an interchange node without closing the free-form kind token.</summary>
public enum VisualArtifactInterchangeNodeRole {
    /// <summary>No well-known role is assigned.</summary>
    Unspecified,
    /// <summary>A topology node.</summary>
    TopologyNode,
    /// <summary>A flow step.</summary>
    FlowStep,
    /// <summary>A sequence participant.</summary>
    SequenceParticipant
}

/// <summary>Identifies the stable role of an interchange edge without closing the free-form kind token.</summary>
public enum VisualArtifactInterchangeEdgeRole {
    /// <summary>No well-known role is assigned.</summary>
    Unspecified,
    /// <summary>A topology edge.</summary>
    TopologyEdge,
    /// <summary>A flow connector.</summary>
    FlowConnector,
    /// <summary>A sequence message.</summary>
    SequenceMessage
}

/// <summary>Identifies the stable role of an interchange annotation without closing the free-form kind token.</summary>
public enum VisualArtifactInterchangeAnnotationRole {
    /// <summary>No well-known role is assigned.</summary>
    Unspecified,
    /// <summary>A sequence activation span.</summary>
    SequenceActivation,
    /// <summary>A sequence note.</summary>
    SequenceNote,
    /// <summary>A sequence block.</summary>
    SequenceBlock,
    /// <summary>A branch within a sequence block.</summary>
    SequenceBranch
}

/// <summary>Represents topology-wide semantics that do not belong to an individual entity.</summary>
public sealed class VisualArtifactInterchangeTopologyArtifact {
    /// <summary>Gets or sets the exact topology layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; set; }
    /// <summary>Gets or sets the exact topology layout direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; }
}

/// <summary>Represents flow-wide semantics that do not belong to an individual entity.</summary>
public sealed class VisualArtifactInterchangeFlowArtifact {
    /// <summary>Gets or sets the exact flow layout mode.</summary>
    public FlowArtifactLayoutMode LayoutMode { get; set; }
    /// <summary>Gets or sets the exact flow layout direction.</summary>
    public FlowArtifactDirection LayoutDirection { get; set; }
}

/// <summary>Represents sequence-wide semantics and provides an additive root for future sequence capabilities.</summary>
public sealed class VisualArtifactInterchangeSequenceArtifact {
}

/// <summary>Contains artifact-level presentation semantics that portable hosts may preserve.</summary>
public sealed class VisualArtifactInterchangePresentation {
    /// <summary>Gets or sets the resolved topology theme.</summary>
    public VisualArtifactInterchangeTheme? Theme { get; set; }
    /// <summary>Gets or sets the geographic viewport.</summary>
    public VisualArtifactInterchangeMapViewport? MapViewport { get; set; }
    /// <summary>Gets or sets the resolved topology legend.</summary>
    public VisualArtifactInterchangeLegend? Legend { get; set; }
}

/// <summary>Represents a resolved, renderer-neutral color theme.</summary>
public sealed class VisualArtifactInterchangeTheme {
    /// <summary>Gets or sets the background color.</summary>
    public string Background { get; set; } = string.Empty;
    /// <summary>Gets or sets the foreground color.</summary>
    public string Foreground { get; set; } = string.Empty;
    /// <summary>Gets or sets the muted foreground color.</summary>
    public string MutedForeground { get; set; } = string.Empty;
    /// <summary>Gets or sets the card color.</summary>
    public string Card { get; set; } = string.Empty;
    /// <summary>Gets or sets the surface color.</summary>
    public string Surface { get; set; } = string.Empty;
    /// <summary>Gets or sets the border color.</summary>
    public string Border { get; set; } = string.Empty;
    /// <summary>Gets or sets the accent color.</summary>
    public string Accent { get; set; } = string.Empty;
    /// <summary>Gets or sets the healthy status color.</summary>
    public string Healthy { get; set; } = string.Empty;
    /// <summary>Gets or sets the warning status color.</summary>
    public string Warning { get; set; } = string.Empty;
    /// <summary>Gets or sets the critical status color.</summary>
    public string Critical { get; set; } = string.Empty;
    /// <summary>Gets or sets the unknown status color.</summary>
    public string Unknown { get; set; } = string.Empty;
    /// <summary>Gets or sets the disabled status color.</summary>
    public string Disabled { get; set; } = string.Empty;
    /// <summary>Gets or sets the font family.</summary>
    public string FontFamily { get; set; } = string.Empty;
}

/// <summary>Represents a geographic viewport in degrees.</summary>
public sealed class VisualArtifactInterchangeMapViewport {
    /// <summary>Gets or sets the optional viewport name.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets the product-neutral projection token.</summary>
    public string Projection { get; set; } = "Equirectangular";
    /// <summary>Gets or sets the minimum longitude.</summary>
    public double MinimumLongitude { get; set; }
    /// <summary>Gets or sets the maximum longitude.</summary>
    public double MaximumLongitude { get; set; }
    /// <summary>Gets or sets the minimum latitude.</summary>
    public double MinimumLatitude { get; set; }
    /// <summary>Gets or sets the maximum latitude.</summary>
    public double MaximumLatitude { get; set; }
}

/// <summary>Represents a resolved legend.</summary>
public sealed class VisualArtifactInterchangeLegend {
    /// <summary>Gets or sets the optional legend title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets legend items in display order.</summary>
    public List<VisualArtifactInterchangeLegendItem> Items { get; } = new();
}

/// <summary>Represents one typed topology legend item.</summary>
public sealed class VisualArtifactInterchangeLegendItem {
    /// <summary>Gets or sets the visible label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the legend item kind.</summary>
    public TopologyLegendItemKind Kind { get; set; }
    /// <summary>Gets or sets the represented status.</summary>
    public TopologyHealthStatus? Status { get; set; }
    /// <summary>Gets or sets the represented node kind.</summary>
    public TopologyNodeKind? NodeKind { get; set; }
    /// <summary>Gets or sets the represented edge kind.</summary>
    public TopologyEdgeKind? EdgeKind { get; set; }
    /// <summary>Gets or sets the optional symbol.</summary>
    public string? Symbol { get; set; }
    /// <summary>Gets or sets the optional icon id.</summary>
    public string? IconId { get; set; }
    /// <summary>Gets or sets the optional foreground color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets or sets the optional background color.</summary>
    public string? BackgroundColor { get; set; }
    /// <summary>Gets or sets the optional line style.</summary>
    public TopologyEdgeLineStyle LineStyle { get; set; }
}

/// <summary>Represents topology-specific group semantics.</summary>
public sealed class VisualArtifactInterchangeTopologyGroup {
    /// <summary>Gets or sets the semantic status.</summary>
    public TopologyHealthStatus Status { get; set; }
    /// <summary>Gets or sets the configured child layout policy.</summary>
    public TopologyGroupLayoutPolicy LayoutPolicy { get; set; }
    /// <summary>Gets or sets the layout policy applied during preparation.</summary>
    public TopologyGroupLayoutPolicy AppliedLayoutPolicy { get; set; }
    /// <summary>Gets or sets the optional longitude.</summary>
    public double? Longitude { get; set; }
    /// <summary>Gets or sets the optional latitude.</summary>
    public double? Latitude { get; set; }
    /// <summary>Gets or sets the optional icon id.</summary>
    public string? IconId { get; set; }
    /// <summary>Gets or sets the optional symbol.</summary>
    public string? Symbol { get; set; }
}

/// <summary>Represents topology-specific node semantics.</summary>
public sealed class VisualArtifactInterchangeTopologyNode {
    /// <summary>Gets or sets the exact node kind.</summary>
    public TopologyNodeKind Kind { get; set; }
    /// <summary>Gets or sets the semantic status.</summary>
    public TopologyHealthStatus Status { get; set; }
    /// <summary>Gets or sets the effective display mode.</summary>
    public TopologyNodeDisplayMode DisplayMode { get; set; }
    /// <summary>Gets or sets the optional longitude.</summary>
    public double? Longitude { get; set; }
    /// <summary>Gets or sets the optional latitude.</summary>
    public double? Latitude { get; set; }
    /// <summary>Gets or sets whether the status badge is shown.</summary>
    public bool ShowStatusBadge { get; set; } = true;
    /// <summary>Gets or sets the optional maximum label length.</summary>
    public int? MaximumLabelCharacters { get; set; }
    /// <summary>Gets or sets safe portable icon artwork.</summary>
    public VisualArtifactInterchangeArtwork? Artwork { get; set; }
}

/// <summary>Describes the availability of portable artwork.</summary>
public enum VisualArtifactInterchangeArtworkStatus {
    /// <summary>The artwork is safe and available.</summary>
    Available,
    /// <summary>The source artwork was intentionally omitted because it was unsafe.</summary>
    UnsafeOmitted
}

/// <summary>Represents safe portable topology icon artwork.</summary>
public sealed class VisualArtifactInterchangeArtwork {
    /// <summary>Gets or sets the artwork availability status.</summary>
    public VisualArtifactInterchangeArtworkStatus Status { get; set; }
    /// <summary>Gets or sets the SVG view box.</summary>
    public string? SvgViewBox { get; set; }
    /// <summary>Gets or sets the SVG preserve-aspect-ratio token.</summary>
    public string? PreserveAspectRatio { get; set; }
    /// <summary>Gets or sets the safe inline SVG fragment.</summary>
    public string? SvgBody { get; set; }
    /// <summary>Gets or sets a safe relative SVG asset path.</summary>
    public string? SvgPath { get; set; }
    /// <summary>Gets or sets a safe relative preview asset path.</summary>
    public string? PreviewPath { get; set; }
    /// <summary>Gets or sets a safe image href.</summary>
    public string? ImageHref { get; set; }
}

/// <summary>Represents one prepared topology waypoint.</summary>
public sealed class VisualArtifactInterchangePoint {
    /// <summary>Gets or sets the x-coordinate.</summary>
    public double X { get; set; }
    /// <summary>Gets or sets the y-coordinate.</summary>
    public double Y { get; set; }
}

/// <summary>Represents topology-specific edge semantics.</summary>
public sealed class VisualArtifactInterchangeTopologyEdge {
    /// <summary>Gets or sets the exact edge kind.</summary>
    public TopologyEdgeKind Kind { get; set; }
    /// <summary>Gets or sets the semantic status.</summary>
    public TopologyHealthStatus Status { get; set; }
    /// <summary>Gets or sets the exact edge direction.</summary>
    public ChartForgeX.Primitives.VisualLinkDirection Direction { get; set; }
    /// <summary>Gets or sets the source attachment side.</summary>
    public TopologyEdgePort SourcePort { get; set; }
    /// <summary>Gets or sets the target attachment side.</summary>
    public TopologyEdgePort TargetPort { get; set; }
    /// <summary>Gets or sets the exact line style.</summary>
    public TopologyEdgeLineStyle LineStyle { get; set; }
    /// <summary>Gets or sets the routing mode.</summary>
    public TopologyEdgeRouting Routing { get; set; }
    /// <summary>Gets or sets the emphasis.</summary>
    public TopologyEdgeEmphasis Emphasis { get; set; }
    /// <summary>Gets or sets the optional source marker.</summary>
    public TopologyMarkerKind? SourceMarker { get; set; }
    /// <summary>Gets or sets the optional target marker.</summary>
    public TopologyMarkerKind? TargetMarker { get; set; }
    /// <summary>Gets or sets the optional stroke width.</summary>
    public double? StrokeWidth { get; set; }
    /// <summary>Gets or sets the optional opacity.</summary>
    public double? Opacity { get; set; }
    /// <summary>Gets the dash pattern.</summary>
    public List<double> DashPattern { get; } = new();
    /// <summary>Gets prepared waypoints.</summary>
    public List<VisualArtifactInterchangePoint> Waypoints { get; } = new();
    /// <summary>Gets or sets whether the edge is muted.</summary>
    public bool IsMuted { get; set; }
    /// <summary>Gets or sets the routing priority.</summary>
    public int RoutingPriority { get; set; }
    /// <summary>Gets or sets an explicit route-lane override, including zero.</summary>
    public double? RouteLane { get; set; }
    /// <summary>Gets or sets the label x offset.</summary>
    public double LabelOffsetX { get; set; }
    /// <summary>Gets or sets the label y offset.</summary>
    public double LabelOffsetY { get; set; }
    /// <summary>Gets or sets the explicit label anchor.</summary>
    public VisualArtifactInterchangePoint? LabelAnchor { get; set; }
    /// <summary>Gets or sets the mapped label-anchor node id.</summary>
    public string? LabelAnchorNodeId { get; set; }
    /// <summary>Gets or sets the layout inference.</summary>
    public TopologyEdgeLayoutInference LayoutInference { get; set; }
    /// <summary>Gets or sets the optional preferred length.</summary>
    public double? PreferredLength { get; set; }
    /// <summary>Gets or sets the minimum rank span.</summary>
    public int MinimumRankSpan { get; set; }
}

/// <summary>Represents flow-step semantics.</summary>
public sealed class VisualArtifactInterchangeFlowNode {
    /// <summary>Gets or sets the exact step kind.</summary>
    public FlowArtifactStepKind Kind { get; set; }
}

/// <summary>Represents flow-connector semantics.</summary>
public sealed class VisualArtifactInterchangeFlowEdge {
    /// <summary>Gets or sets the exact connector kind.</summary>
    public FlowArtifactConnectorKind Kind { get; set; }
    /// <summary>Gets or sets the exact connector direction.</summary>
    public ChartForgeX.Primitives.VisualLinkDirection Direction { get; set; }
}

/// <summary>Represents sequence-participant semantics.</summary>
public sealed class VisualArtifactInterchangeSequenceNode {
    /// <summary>Gets or sets the exact participant kind.</summary>
    public SequenceArtifactParticipantKind Kind { get; set; }
    /// <summary>Gets or sets the participant order.</summary>
    public int Order { get; set; }
    /// <summary>Gets or sets whether the participant was inferred by the parser.</summary>
    public bool IsImplicit { get; set; }
}

/// <summary>Represents sequence-message semantics.</summary>
public sealed class VisualArtifactInterchangeSequenceEdge {
    /// <summary>Gets or sets the exact semantic message kind.</summary>
    public SequenceArtifactMessageKind Kind { get; set; }
    /// <summary>Gets or sets the exact message line style.</summary>
    public SequenceArtifactMessageLineStyle LineStyle { get; set; }
    /// <summary>Gets or sets whether the message activates its target.</summary>
    public bool ActivatesTarget { get; set; }
    /// <summary>Gets or sets whether the message deactivates its source.</summary>
    public bool Deactivates { get; set; }
}

/// <summary>Represents sequence-annotation semantics.</summary>
public sealed class VisualArtifactInterchangeSequenceAnnotation {
    /// <summary>Gets or sets activation state for activation annotations.</summary>
    public bool? ActivationState { get; set; }
    /// <summary>Gets or sets the optional note placement.</summary>
    public SequenceArtifactNotePlacement? NotePlacement { get; set; }
    /// <summary>Gets or sets the optional block kind.</summary>
    public SequenceArtifactBlockKind? BlockKind { get; set; }
    /// <summary>Gets or sets the optional parent block kind for a branch.</summary>
    public SequenceArtifactBlockKind? ParentBlockKind { get; set; }
    /// <summary>Gets or sets the branch kind token.</summary>
    public string? BranchKind { get; set; }
    /// <summary>Gets or sets the nesting depth.</summary>
    public int Depth { get; set; }
    /// <summary>Gets or sets whether the span contains no message steps.</summary>
    public bool IsEmpty { get; set; }
}
