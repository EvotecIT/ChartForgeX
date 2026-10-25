using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Primitives;

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
    Sequence,
    /// <summary>Native railroad rules and symbolic expression structure.</summary>
    Railroad,
    /// <summary>Native directory rows with stable parent identities and typed presentation.</summary>
    TreeView,
    /// <summary>One exclusive native waterfall with typed or numeric authored items and portable presentation.</summary>
    Waterfall,
    /// <summary>Ordered native journey sections, ordinal scores and actor membership.</summary>
    Journey,
    /// <summary>Ordered native categorical timeline periods and their events.</summary>
    Timeline
}

/// <summary>Identifies the stable role of an interchange group without closing the free-form kind token.</summary>
public enum VisualArtifactInterchangeGroupRole {
    /// <summary>No well-known role is assigned.</summary>
    Unspecified,
    /// <summary>A topology group or container.</summary>
    TopologyGroup,
    /// <summary>A flow lane.</summary>
    FlowLane,
    /// <summary>A vertical group of sequence participants.</summary>
    SequenceParticipantGroup
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
    /// <summary>Gets or sets optional exact native viewport/frame edge insets. Null retains uniform native default padding; non-null requires version2.</summary>
    public ChartPadding? ViewportPadding { get; set; }
    internal TopologyPreparedContentClip? ContentClip { get; set; }
    /// <summary>Gets or sets immutable retained native crossing paint; null omits the version-2 policy.</summary>
    public TopologyCrossingPaint? CrossingPaint { get; set; }
    /// <summary>Gets ordered immutable native path decorations in this snapshot's coordinate domain; empty is omitted.</summary>
    /// <remarks>Nonempty decorations require version 2. Prepared snapshots contain fitted geometry; retained hierarchy sources contain authored geometry.</remarks>
    public List<TopologyPathDecoration> PathDecorations { get; } = new();
    /// <summary>Gets ordered identified row/column declarations over portable node identities; omitted from JSON when empty.</summary>
    public List<VisualArtifactInterchangeTopologyAlignment> Alignments { get; } = new();
    /// <summary>Gets or sets an optional retained raw hierarchy source and its visible projection request.</summary>
    public VisualArtifactInterchangeTopologyHierarchy? Hierarchy { get; set; }
    /// <summary>Gets or sets the exact topology layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; set; }
    /// <summary>Gets or sets the exact topology layout direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; }
}

/// <summary>Represents flow-wide semantics that do not belong to an individual entity.</summary>
public sealed class VisualArtifactInterchangeFlowArtifact {
    internal ChartPadding? ViewportPadding { get; set; }
    internal VisualArtifactExportFormat? ExportFormats { get; set; }
    /// <summary>Gets or sets the exact flow layout mode.</summary>
    public FlowArtifactLayoutMode LayoutMode { get; set; }
    /// <summary>Gets or sets the exact flow layout direction.</summary>
    public FlowArtifactDirection LayoutDirection { get; set; }
}

/// <summary>Represents sequence-wide semantics and provides an additive root for future sequence capabilities.</summary>
public sealed class VisualArtifactInterchangeSequenceArtifact {
    /// <summary>Gets or sets immutable contour-only note shadow facts.</summary>
    public ChartForgeX.Effects.VisualDropShadow? NoteShadow { get; set; }
    /// <summary>Gets or sets immutable fragment-tab shadow facts.</summary>
    public ChartForgeX.Effects.VisualDropShadow? FragmentTabShadow { get; set; }
    /// <summary>Gets or sets immutable rect-background shadow facts.</summary>
    public ChartForgeX.Effects.VisualDropShadow? BackgroundShadow { get; set; }
    /// <summary>Gets or sets the immutable native lifeline stroke, independently of source metadata and geometry.</summary>
    public ChartForgeX.Core.ChartStrokeStyle? LifelineStroke {get;set;}
    /// <summary>Gets or sets the immutable native theme captured only with a nonempty sequence presentation policy.</summary>
    public ChartForgeX.Themes.VisualTheme? Theme { get; set; }
    /// <summary>Gets or sets the paired mode accompanying the captured native theme.</summary>
    public ChartForgeX.Themes.VisualThemeMode? ThemeMode { get; set; }
    /// <summary>Gets or sets immutable optional native allocations; nonempty policies require version2.</summary>
    public SequenceLayoutPolicy? Layout { get; set; }
    /// <summary>Gets or sets captured detached role typography; nonempty roles require version2.</summary>
    public SequenceTypography? Typography { get; set; }
}

/// <summary>Contains artifact-level presentation semantics that portable hosts may preserve.</summary>
public sealed class VisualArtifactInterchangePresentation {
    /// <summary>Gets finite captured presentation steps in source-to-output application order; empty is omitted.</summary>
    /// <remarks>Nonempty effects require version2. Editable-only import rejects them until explicitly cleared
    /// on a detached source copy. ApplyCapturedEffects replays root wrappers; ApplyCapturedLayer replays a terminal capture.</remarks>
    public List<VisualArtifactInterchangeCapturedEffect> CapturedEffects { get; } = new();
    internal TopologyCaptionPresentation? TopologyCaptions { get; set; }
    internal VisualFrame? Frame { get; set; }
    internal PreparedDiagramFont? ContextFont { get; set; }
    /// <summary>Gets or sets the resolved native diagram theme.</summary>
    public VisualArtifactInterchangeTheme? Theme { get; set; }
    /// <summary>Gets or sets the geographic viewport.</summary>
    public VisualArtifactInterchangeMapViewport? MapViewport { get; set; }
    /// <summary>Gets or sets the resolved topology legend.</summary>
    public VisualArtifactInterchangeLegend? Legend { get; set; }
}

/// <summary>Represents a resolved, renderer-neutral color theme.</summary>
public sealed class VisualArtifactInterchangeTheme {
    /// <summary>Gets or sets the complete immutable paired native theme. When present it owns geometry, effects and typography; resolved legacy slots remain observations.</summary>
    public ChartForgeX.Themes.VisualTheme? Definition { get; set; }
    /// <summary>Gets or sets the selected native theme mode. Null preserves the legacy Light restoration contract; nonnull requires version2.</summary>
    public ChartForgeX.Themes.VisualThemeMode? Mode { get; set; }
    /// <summary>Gets or sets an optional positive native topology title size. Null retains the ordinary Graphite size; non-null requires version 2.</summary>
    public double? TitleSize { get; set; }
    /// <summary>Gets or sets an optional positive topology data-label size. Null retains the ordinary Graphite size; non-null requires version2.</summary>
    public double? DataLabelSize { get; set; }
    /// <summary>Gets or sets an optional positive topology frame subtitle size. Null retains the ordinary Graphite size; non-null requires version 2.</summary>
    public double? SubtitleSize { get; set; }
    /// <summary>Gets or sets an optional positive topology frame legend size. Null retains the ordinary Graphite size; non-null requires version 2.</summary>
    public double? LegendSize { get; set; }
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
public sealed partial class VisualArtifactInterchangeTopologyGroup {
    /// <summary>Gets or sets immutable safe caption runs whose plain projection equals the semantic label.</summary>
    public ChartForgeX.Typography.TextContent? LabelContent { get; set; }
    /// <summary>Gets or sets this native item's captured base caption typography.</summary>
    public ChartForgeX.Typography.TextStyleOverride? TextStyle { get; set; }
    /// <summary>Gets or sets whether the title/subtitle are painted; identity and semantic label remain available.</summary>
    public bool ShowLabel { get; set; } = true;
    /// <summary>Gets or sets whether this group is an independent container or an ordered compartment.</summary>
    public TopologyGroupPresentation Presentation { get; set; }
    /// <summary>Gets or sets the containing group id, or null for a root region.</summary>
    public string? ParentGroupId { get; set; }
    /// <summary>Gets or sets a local direction; null inherits the parent region.</summary>
    public TopologyLayoutDirection? LayoutDirection { get; set; }
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
public sealed partial class VisualArtifactInterchangeTopologyNode {
    internal TopologyPreparedNodeBasis? PreparedNodeBasis { get; set; }
    /// <summary>Gets or sets captured subtitle-role typography. Non-null requires interchange version 2.</summary>
    public ChartForgeX.Typography.TextStyleOverride? SubtitleTextStyle { get; set; }
    /// <summary>Gets or sets detached finite native table content. Non-null content requires envelope version2.</summary>
    public VisualArtifactInterchangeTableContent? TableContent { get; set; }
    /// <summary>Gets or sets optional immutable structured caption runs. Their plain projection must equal the node label.</summary>
    public ChartForgeX.Typography.TextContent? LabelContent { get; set; }
    /// <summary>Gets or sets the measured text-line width, independent of a fixed prepared node frame.</summary>
    public double? MaximumTextWidth { get; set; }
    /// <summary>Gets or sets authored additional native content padding.</summary>
    public double? ContentPadding { get; set; }
    /// <summary>Gets or sets this native item's captured base caption typography.</summary>
    public ChartForgeX.Typography.TextStyleOverride? TextStyle { get; set; }
    /// <summary>Gets or sets the exact node kind.</summary>
    public TopologyNodeKind Kind { get; set; }
    /// <summary>Gets or sets an optional nonnegative categorical slot in the active theme palette.</summary>
    public int? PaletteIndex { get; set; }
    /// <summary>Gets or sets the semantic status.</summary>
    public TopologyHealthStatus Status { get; set; }
    /// <summary>Gets or sets the effective display mode.</summary>
    public TopologyNodeDisplayMode DisplayMode { get; set; }
    /// <summary>Gets or sets the optional diagram surface.</summary>
    public TopologyNodeShape? Shape { get; set; }
    /// <summary>Gets or sets the explicit native glyph shape, independent of an optional catalog id. Non-null requires version 2.</summary>
    public TopologyIconShape? IconShape { get; set; }
    /// <summary>Gets or sets whether label-only node dimensions are minimums for prepared font fitting.</summary>
    public bool FitLabelToContent { get; set; }
    /// <summary>Gets or sets the optional longitude.</summary>
    public double? Longitude { get; set; }
    /// <summary>Gets or sets the optional latitude.</summary>
    public double? Latitude { get; set; }
    /// <summary>Gets or sets whether the status badge is shown.</summary>
    public bool ShowStatusBadge { get; set; } = true;
    /// <summary>Gets or sets whether the semantic label is painted as a visible caption.</summary>
    public bool ShowLabel { get; set; } = true;
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
    /// <summary>Gets or sets optional native relationship and endpoint backplate fill; non-null requires version two.</summary>
    public string? LabelBackgroundColor { get; set; }
    /// <summary>Gets or sets interpolation over the resolved native route. Non-null requires version2.</summary>
    public TopologyEdgeInterpolation? Interpolation { get; set; }
    /// <summary>Gets or sets the optional pre-interpolation logical corner blend, independent of native rounding.</summary>
    public double? InterpolationCornerBlendLength { get; set; }
    internal TopologyPreparedEdgePlan? PreparedEdgePlan { get; set; }
    /// <summary>Gets or sets immutable safe caption runs whose plain projection equals the semantic label.</summary>
    public ChartForgeX.Typography.TextContent? LabelContent { get; set; }
    /// <summary>Gets or sets the explicit native label center, independent of its leader anchor.</summary>
    public TopologyLabelPosition? LabelPosition { get; set; }
    /// <summary>Gets or sets optional source endpoint marker layout.</summary>
    public TopologyMarkerStyle? SourceMarkerStyle { get; set; }
    /// <summary>Gets or sets optional target endpoint marker layout.</summary>
    public TopologyMarkerStyle? TargetMarkerStyle { get; set; }
    /// <summary>Gets or sets immutable complete native route geometry. Non-null requires version 2.</summary>
    public TopologyPathGeometry? PathGeometry { get; set; }
    /// <summary>Gets or sets this native item's captured base caption typography.</summary>
    public ChartForgeX.Typography.TextStyleOverride? TextStyle { get; set; }
    /// <summary>Gets or sets the exact edge kind.</summary>
    public TopologyEdgeKind Kind { get; set; }
    /// <summary>Gets or sets an optional nonnegative categorical slot in the active theme palette.</summary>
    public int? PaletteIndex { get; set; }
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
    internal double? AuthoredWidth { get; set; }
    internal double? AuthoredHeight { get; set; }
    /// <summary>Gets or sets the exact step kind.</summary>
    public FlowArtifactStepKind Kind { get; set; }
    /// <summary>Gets or sets whether node dimensions are minima for complete measured content. Default: true.</summary>
    public bool FitLabelToContent { get; set; } = true;
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
    /// <summary>Gets or sets immutable native glyph/box surface-shadow facts, separate from captions and links.</summary>
    public ChartForgeX.Effects.VisualDropShadow? SurfaceShadow { get; set; }
    /// <summary>Gets or sets optional literal participant border ink.</summary>
    public string? Border {get;set;}
    /// <summary>Gets or sets optional literal per-participant activation fill.</summary>
    public string? ActivationFill {get;set;}
    /// <summary>Gets or sets optional literal per-participant activation border.</summary>
    public string? ActivationBorder {get;set;}
    /// <summary>Gets ordered immutable host navigation records; the collection is detached in prepared snapshots.</summary>
    public List<VisualNavigationLink> NavigationLinks { get; } = new();
    /// <summary>Gets or sets the optional literal native stereotype.</summary>
    public string? Stereotype { get; set; }
    /// <summary>Gets or sets the optional literal native symbol.</summary>
    public string? Symbol { get; set; }
    /// <summary>Gets or sets the optional positive authored minimum header width.</summary>
    public double? MinimumWidth { get; set; }
    /// <summary>Gets or sets an optional literal resolved native participant fill.</summary>
    public string? Fill { get; set; }
    /// <summary>Gets or sets the exact participant kind.</summary>
    public SequenceArtifactParticipantKind Kind { get; set; }
    /// <summary>Gets or sets the participant order.</summary>
    public int Order { get; set; }
    /// <summary>Gets or sets whether the participant was inferred by the parser.</summary>
    public bool IsImplicit { get; set; }
    /// <summary>Gets or sets the index of the message creating the participant; null denotes an initial participant.</summary>
    public int? CreatedAtMessageIndex { get; set; }
    /// <summary>Gets or sets the index of the message destroying the participant; null denotes an open lifetime.</summary>
    public int? DestroyedAtMessageIndex { get; set; }
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
