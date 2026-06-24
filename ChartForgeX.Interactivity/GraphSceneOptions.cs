namespace ChartForgeX.Interactivity;

/// <summary>
/// Holds host-neutral graph exploration settings that browser, native, and static adapters can share.
/// </summary>
public sealed class GraphSceneOptions {
    /// <summary>Gets or sets the enabled graph explorer feature flags.</summary>
    public GraphSceneFeatures Features { get; set; } = GraphSceneFeatures.Explorer | GraphSceneFeatures.DragNodes;

    /// <summary>Gets graph physics settings for adapters that support runtime or prepared layouts.</summary>
    public GraphPhysicsOptions Physics { get; } = new();

    /// <summary>Gets large-graph level-of-detail thresholds.</summary>
    public GraphLevelOfDetailOptions LevelOfDetail { get; } = new();

    /// <summary>Gets graph runtime performance budgets and telemetry settings.</summary>
    public GraphPerformanceOptions Performance { get; } = new();

    /// <summary>
    /// Determines whether all bits in the requested feature set are enabled.
    /// </summary>
    /// <param name="feature">The feature or feature group to check.</param>
    /// <returns><c>true</c> when the feature is enabled; otherwise, <c>false</c>.</returns>
    public bool HasFeature(GraphSceneFeatures feature) => feature != GraphSceneFeatures.None && (Features & feature) == feature;

    /// <summary>
    /// Enables one or more graph scene features.
    /// </summary>
    /// <param name="features">The features to enable.</param>
    /// <returns>The current options instance.</returns>
    public GraphSceneOptions Enable(GraphSceneFeatures features) {
        Features |= features;
        return this;
    }

    /// <summary>
    /// Disables one or more graph scene features.
    /// </summary>
    /// <param name="features">The features to disable.</param>
    /// <returns>The current options instance.</returns>
    public GraphSceneOptions Disable(GraphSceneFeatures features) {
        Features &= ~features;
        return this;
    }
}

/// <summary>
/// Names reusable graph explorer features that adapters may support.
/// </summary>
[System.Flags]
public enum GraphSceneFeatures {
    /// <summary>No graph explorer behavior is enabled.</summary>
    None = 0,

    /// <summary>Nodes, edges, or clusters can be selected.</summary>
    Selection = 1,

    /// <summary>Multiple graph items can be selected together.</summary>
    MultiSelection = 2,

    /// <summary>Graph items can be searched by label, kind, status, and metadata.</summary>
    Search = 4,

    /// <summary>Graph items can be filtered by reusable facets.</summary>
    Filtering = 8,

    /// <summary>The adapter can pan, zoom, or fit the visible graph viewport.</summary>
    Viewport = 16,

    /// <summary>Users can drag nodes to update runtime positions.</summary>
    DragNodes = 32,

    /// <summary>The adapter can run browser or native runtime physics.</summary>
    RuntimePhysics = 64,

    /// <summary>The adapter can stabilize a graph layout over several simulation ticks.</summary>
    Stabilization = 128,

    /// <summary>The adapter can collapse, expand, or summarize graph clusters.</summary>
    Clustering = 256,

    /// <summary>The adapter can reduce labels or rendering detail for large scenes.</summary>
    LevelOfDetail = 512,

    /// <summary>The adapter can accept incremental state updates after initial render.</summary>
    IncrementalUpdates = 1024,

    /// <summary>The adapter can export current graph state or artwork.</summary>
    Export = 2048,

    /// <summary>The adapter can focus the selected node's immediate neighborhood.</summary>
    NeighborhoodFocus = 4096,

    /// <summary>The adapter can publish runtime performance telemetry.</summary>
    PerformanceTelemetry = 8192,

    /// <summary>Common graph explorer surface for dependency-free visual exploration.</summary>
    Explorer = Selection | MultiSelection | Search | Filtering | Viewport | NeighborhoodFocus | Clustering | LevelOfDetail
}
