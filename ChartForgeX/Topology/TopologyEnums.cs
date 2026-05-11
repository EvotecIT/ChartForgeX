using System;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines deterministic topology layout modes.
/// </summary>
public enum TopologyLayoutMode {
    /// <summary>Use coordinates supplied on groups and nodes.</summary>
    Manual = 0,
    /// <summary>Place groups in a deterministic grid and place unpositioned nodes inside their groups.</summary>
    GroupGrid = 1,
    /// <summary>Place a hub near the center and branch nodes around it.</summary>
    HubAndSpoke = 2,
    /// <summary>Place nodes by metadata layer in a deterministic layered flow.</summary>
    Layered = 3,
    /// <summary>Place nodes in a deterministic matrix.</summary>
    Matrix = 4,
    /// <summary>Place groups in dense deterministic panels and pack each group's hub/branch nodes inside the panel.</summary>
    DenseGrouped = 5,
    /// <summary>Project nodes and groups from longitude/latitude coordinates into the topology viewport.</summary>
    Geographic = 6,
    /// <summary>Place connected nodes with a deterministic force-directed simulation using repulsion, link attraction, and group gravity.</summary>
    ForceDirected = 7
}

/// <summary>
/// Defines deterministic layout flow direction for layout modes that support ordered layers.
/// </summary>
public enum TopologyLayoutDirection {
    /// <summary>Place layers from top to bottom.</summary>
    TopToBottom,
    /// <summary>Place layers from left to right.</summary>
    LeftToRight,
    /// <summary>Place layers from bottom to top.</summary>
    BottomToTop,
    /// <summary>Place layers from right to left.</summary>
    RightToLeft
}

/// <summary>
/// Defines how nodes inside a topology group should be arranged by dense grouped layouts.
/// </summary>
public enum TopologyGroupLayoutPolicy {
    /// <summary>Let the layout choose a deterministic default from the group contents.</summary>
    Auto,
    /// <summary>Place one hub-like node above deterministic branch rows.</summary>
    HubAndBranch,
    /// <summary>Place all group nodes in a compact deterministic grid.</summary>
    Grid,
    /// <summary>Place group nodes in paired rows, useful for replication partners or redundant controllers.</summary>
    PairRows,
    /// <summary>Place group nodes in a compact staggered mini-mesh, useful for dense replication or connectivity clusters.</summary>
    MiniMesh,
    /// <summary>Render group nodes as compact dots in a deterministic grid.</summary>
    CollapsedDots
}

/// <summary>
/// Describes topology edge layout values inferred by a layout engine instead of supplied by the caller.
/// </summary>
[Flags]
public enum TopologyEdgeLayoutInference {
    /// <summary>No edge layout values were inferred.</summary>
    None = 0,
    /// <summary>The source edge port was inferred.</summary>
    SourcePort = 1,
    /// <summary>The target edge port was inferred.</summary>
    TargetPort = 2,
    /// <summary>The edge route lane was inferred.</summary>
    RouteLane = 4
}

/// <summary>
/// Defines common topology view presets that compose render options without changing the source model.
/// </summary>
public enum TopologyViewPreset {
    /// <summary>Use normal rendering defaults.</summary>
    Default,
    /// <summary>Render group containers and labels.</summary>
    Grouped,
    /// <summary>Render nodes and edges without group containers.</summary>
    Ungrouped,
    /// <summary>Render an edge-focused connectivity view.</summary>
    Connectivity,
    /// <summary>Render a dependency-oriented view.</summary>
    Dependency,
    /// <summary>Render an offender-focused view that highlights warning and critical elements.</summary>
    Offenders,
    /// <summary>Render a compact topology suitable for dense dashboard cards.</summary>
    Compact,
    /// <summary>Render a metric-driven topology where edge metric labels are emphasized.</summary>
    MetricLabels
}

/// <summary>
/// Defines reusable topology visual treatments.
/// </summary>
public enum TopologyVisualStyle {
    /// <summary>Use normal ChartForgeX topology styling.</summary>
    Default,
    /// <summary>Use a lighter monitoring-dashboard treatment with thinner hierarchy links, softer panels, and compact status marks.</summary>
    MonitoringDashboard
}

/// <summary>
/// Defines how geographic topology land should be rendered.
/// </summary>
public enum TopologyMapBackgroundStyle {
    /// <summary>Choose the map treatment from the visual style and viewport.</summary>
    Auto,
    /// <summary>Render the point-sampled dotted land layer.</summary>
    DottedLand,
    /// <summary>Render filled land silhouettes and boundaries without the dotted land layer.</summary>
    SoftSilhouette
}

/// <summary>
/// Defines how group containers should be filled.
/// </summary>
public enum TopologyGroupSurfaceStyle {
    /// <summary>Choose the group surface from the active visual style.</summary>
    Auto,
    /// <summary>Render groups with a subtle status or identity tint.</summary>
    Tinted,
    /// <summary>Render groups as neutral cards with status or identity only on the border and title.</summary>
    Neutral
}

/// <summary>
/// Defines how topology nodes should be presented.
/// </summary>
public enum TopologyNodeDisplayMode {
    /// <summary>Render full node cards.</summary>
    Card,
    /// <summary>Render smaller node cards.</summary>
    CompactCard,
    /// <summary>Render compact icon tiles with labels below the tile.</summary>
    Tile,
    /// <summary>Render single-line pill nodes.</summary>
    Pill,
    /// <summary>Render icon-only nodes.</summary>
    Icon,
    /// <summary>Render small status dots.</summary>
    Dot,
    /// <summary>Do not render the node; keep it available as an edge anchor or route point.</summary>
    Hidden
}

/// <summary>
/// Defines renderer-owned topology icon shape hints.
/// </summary>
public enum TopologyIconShape {
    /// <summary>Choose a shape from the icon or node kind.</summary>
    Auto,
    /// <summary>Render a generic labeled badge.</summary>
    Badge,
    /// <summary>Render a server or compute stack.</summary>
    Server,
    /// <summary>Render a database cylinder.</summary>
    Database,
    /// <summary>Render storage media.</summary>
    Storage,
    /// <summary>Render a cloud mark.</summary>
    Cloud,
    /// <summary>Render a network fabric mark.</summary>
    Network,
    /// <summary>Render a network segment mark.</summary>
    NetworkSegment,
    /// <summary>Render a switch mark.</summary>
    NetworkSwitch,
    /// <summary>Render a router mark.</summary>
    Router,
    /// <summary>Render a firewall mark.</summary>
    Firewall,
    /// <summary>Render a load balancer mark.</summary>
    LoadBalancer,
    /// <summary>Render an Active Directory forest mark.</summary>
    Forest,
    /// <summary>Render a domain or tenant boundary mark.</summary>
    Domain,
    /// <summary>Render a domain controller mark.</summary>
    DomainController,
    /// <summary>Render a read-only domain controller mark.</summary>
    ReadOnlyDomainController,
    /// <summary>Render a site or location mark.</summary>
    Site,
    /// <summary>Render an application mark.</summary>
    Application,
    /// <summary>Render a service mark.</summary>
    Service,
    /// <summary>Render a certificate mark.</summary>
    Certificate,
    /// <summary>Render a desktop endpoint mark.</summary>
    Desktop,
    /// <summary>Render a laptop endpoint mark.</summary>
    Laptop,
    /// <summary>Render a person mark.</summary>
    Person,
    /// <summary>Render a team mark.</summary>
    Team
}

/// <summary>
/// Defines how card-like topology node subtitles should be rendered.
/// </summary>
public enum TopologyCardSubtitleMode {
    /// <summary>Render subtitles as plain muted text.</summary>
    Text,
    /// <summary>Render subtitles as compact chips inside the node card.</summary>
    Chip
}

/// <summary>
/// Defines how topology legends are sourced.
/// </summary>
public enum TopologyLegendMode {
    /// <summary>Use only the legend provided on the chart.</summary>
    Explicit,
    /// <summary>Infer a legend only when the chart does not provide one.</summary>
    AutoWhenMissing,
    /// <summary>Infer a legend from the chart data.</summary>
    Auto,
    /// <summary>Merge inferred legend items with explicitly provided legend items.</summary>
    Merge
}

/// <summary>
/// Describes topology health state.
/// </summary>
public enum TopologyHealthStatus {
    /// <summary>The item is healthy.</summary>
    Healthy,
    /// <summary>The item needs attention.</summary>
    Warning,
    /// <summary>The item is failing or degraded enough to be critical.</summary>
    Critical,
    /// <summary>The item health is unknown.</summary>
    Unknown,
    /// <summary>The item is disabled or intentionally muted.</summary>
    Disabled
}

/// <summary>
/// Describes topology node kind.
/// </summary>
public enum TopologyNodeKind {
    /// <summary>A generic topology node.</summary>
    Generic,
    /// <summary>A logical group or cluster node.</summary>
    Group,
    /// <summary>A location, region, site, or geography node.</summary>
    Location,
    /// <summary>A hub or central node.</summary>
    Hub,
    /// <summary>A branch, spoke, or satellite node.</summary>
    Branch,
    /// <summary>A server or compute node.</summary>
    Server,
    /// <summary>A service topology node.</summary>
    Service,
    /// <summary>An endpoint topology node.</summary>
    Endpoint,
    /// <summary>A gateway topology node.</summary>
    Gateway,
    /// <summary>A cloud topology node.</summary>
    Cloud,
    /// <summary>A database topology node.</summary>
    Database,
    /// <summary>A storage topology node.</summary>
    Storage,
    /// <summary>A network topology node.</summary>
    Network,
    /// <summary>A network segment, subnet, VLAN, or similar topology node.</summary>
    NetworkSegment,
    /// <summary>A namespace, domain, tenant, or boundary node.</summary>
    Namespace,
    /// <summary>An application topology node.</summary>
    Application,
    /// <summary>A process or workflow topology node.</summary>
    Process,
    /// <summary>A queue or stream topology node.</summary>
    Queue,
    /// <summary>A person topology node.</summary>
    Person,
    /// <summary>A team or organizational unit topology node.</summary>
    Team,
    /// <summary>A certificate topology node.</summary>
    Certificate
}

/// <summary>
/// Describes topology edge kind.
/// </summary>
public enum TopologyEdgeKind {
    /// <summary>A generic topology edge.</summary>
    Generic,
    /// <summary>A generic link or relationship edge.</summary>
    Link,
    /// <summary>A replication topology edge.</summary>
    Replication,
    /// <summary>A connectivity topology edge.</summary>
    Connectivity,
    /// <summary>A dependency topology edge.</summary>
    Dependency,
    /// <summary>A trust topology edge.</summary>
    Trust,
    /// <summary>A mapping, assignment, ownership, or membership edge.</summary>
    Mapping,
    /// <summary>An authentication-path topology edge.</summary>
    AuthenticationPath,
    /// <summary>A certificate-chain topology edge.</summary>
    CertificateChain,
    /// <summary>A data-flow topology edge.</summary>
    DataFlow,
    /// <summary>An ownership topology edge.</summary>
    Ownership,
    /// <summary>A membership topology edge.</summary>
    Membership
}

/// <summary>
/// Describes edge direction marker behavior.
/// </summary>
public enum TopologyDirection {
    /// <summary>No direction marker.</summary>
    None,
    /// <summary>Source to target direction marker.</summary>
    Forward,
    /// <summary>Target to source direction marker.</summary>
    Backward,
    /// <summary>Bidirectional markers.</summary>
    Bidirectional
}

/// <summary>
/// Describes edge path routing.
/// </summary>
public enum TopologyEdgeRouting {
    /// <summary>Route edges as straight lines.</summary>
    Straight,
    /// <summary>Route edges as cubic curves.</summary>
    Curved,
    /// <summary>Route edges as orthogonal paths.</summary>
    Orthogonal,
    /// <summary>Route edges as deterministic orthogonal paths that prefer lanes avoiding nearby topology obstacles.</summary>
    ObstacleAvoidingOrthogonal
}

/// <summary>
/// Describes explicit edge line styling.
/// </summary>
public enum TopologyEdgeLineStyle {
    /// <summary>Choose line styling from edge health status.</summary>
    Auto,
    /// <summary>Render a solid line regardless of health status.</summary>
    Solid,
    /// <summary>Render a dashed line regardless of health status.</summary>
    Dashed,
    /// <summary>Render a dotted line regardless of health status.</summary>
    Dotted
}

/// <summary>
/// Describes how prominently an edge should render.
/// </summary>
public enum TopologyEdgeEmphasis {
    /// <summary>Render the edge with standard prominence.</summary>
    Normal,
    /// <summary>Render the edge as a low-emphasis status-preserving relationship.</summary>
    Subtle,
    /// <summary>Render the edge with stronger prominence.</summary>
    Strong
}

/// <summary>
/// Describes a preferred edge attachment side on a topology node.
/// </summary>
public enum TopologyEdgePort {
    /// <summary>Choose the attachment side from node positions.</summary>
    Auto,
    /// <summary>Attach to the top side of the node.</summary>
    Top,
    /// <summary>Attach to the right side of the node.</summary>
    Right,
    /// <summary>Attach to the bottom side of the node.</summary>
    Bottom,
    /// <summary>Attach to the left side of the node.</summary>
    Left
}

/// <summary>
/// Describes the type of item shown in a topology legend.
/// </summary>
public enum TopologyLegendItemKind {
    /// <summary>A health-status legend item.</summary>
    Status,
    /// <summary>A node-kind legend item.</summary>
    Node,
    /// <summary>An edge-kind legend item.</summary>
    Edge
}

/// <summary>
/// Describes topology severity for legends and future adapters.
/// </summary>
public enum TopologySeverity {
    /// <summary>No severity.</summary>
    None,
    /// <summary>Informational severity.</summary>
    Info,
    /// <summary>Low severity.</summary>
    Low,
    /// <summary>Medium severity.</summary>
    Medium,
    /// <summary>High severity.</summary>
    High,
    /// <summary>Critical severity.</summary>
    Critical
}

/// <summary>
/// Describes reusable SVG marker kinds.
/// </summary>
public enum TopologyMarkerKind {
    /// <summary>No marker.</summary>
    None,
    /// <summary>An arrow marker.</summary>
    Arrow,
    /// <summary>A circle marker.</summary>
    Circle,
    /// <summary>A diamond marker.</summary>
    Diamond
}
