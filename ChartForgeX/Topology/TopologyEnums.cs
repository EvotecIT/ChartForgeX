namespace ChartForgeX.Topology;

/// <summary>
/// Defines deterministic topology layout modes.
/// </summary>
public enum TopologyLayoutMode {
    /// <summary>Use coordinates supplied on groups and nodes.</summary>
    Manual,
    /// <summary>Place groups in a deterministic grid and place unpositioned nodes inside their groups.</summary>
    RegionGrid,
    /// <summary>Place a hub near the center and branch nodes around it.</summary>
    HubAndSpoke,
    /// <summary>Place nodes by metadata layer in a deterministic layered flow.</summary>
    Layered,
    /// <summary>Place nodes in a deterministic matrix.</summary>
    Matrix
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
    /// <summary>A regional topology node.</summary>
    Region,
    /// <summary>A site topology node.</summary>
    Site,
    /// <summary>A hub site topology node.</summary>
    HubSite,
    /// <summary>A branch site topology node.</summary>
    BranchSite,
    /// <summary>A domain controller topology node.</summary>
    DomainController,
    /// <summary>A bridgehead server topology node.</summary>
    BridgeheadServer,
    /// <summary>A subnet topology node.</summary>
    Subnet,
    /// <summary>A subnet group topology node.</summary>
    SubnetGroup,
    /// <summary>A domain topology node.</summary>
    Domain,
    /// <summary>A forest topology node.</summary>
    Forest,
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
    /// <summary>A certificate topology node.</summary>
    Certificate
}

/// <summary>
/// Describes topology edge kind.
/// </summary>
public enum TopologyEdgeKind {
    /// <summary>A generic topology edge.</summary>
    Generic,
    /// <summary>A site-link topology edge.</summary>
    SiteLink,
    /// <summary>A replication topology edge.</summary>
    Replication,
    /// <summary>A connectivity topology edge.</summary>
    Connectivity,
    /// <summary>A dependency topology edge.</summary>
    Dependency,
    /// <summary>A trust topology edge.</summary>
    Trust,
    /// <summary>A subnet-mapping topology edge.</summary>
    SubnetMapping,
    /// <summary>An authentication-path topology edge.</summary>
    AuthenticationPath,
    /// <summary>A certificate-chain topology edge.</summary>
    CertificateChain
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
    Orthogonal
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
