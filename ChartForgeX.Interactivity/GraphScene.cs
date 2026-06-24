using System;
using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a host-neutral graph exploration scene that adapters can render with SVG, Canvas, WebGL, desktop, or native controls.
/// </summary>
public sealed class GraphScene {
    private const GraphSceneFeatures KnownFeatures = GraphSceneFeatures.Selection | GraphSceneFeatures.MultiSelection | GraphSceneFeatures.Search | GraphSceneFeatures.Filtering | GraphSceneFeatures.Viewport | GraphSceneFeatures.DragNodes | GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.Clustering | GraphSceneFeatures.LevelOfDetail | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export | GraphSceneFeatures.NeighborhoodFocus | GraphSceneFeatures.PerformanceTelemetry;

    private string _id = "graph";
    private string _title = "Graph";

    /// <summary>Gets or sets the stable scene identifier used for host events and persisted state.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Graph scene ids"); }

    /// <summary>Gets or sets the human-readable scene title.</summary>
    public string Title { get => _title; set => _title = ChartInteractionText.RequiredText(value, nameof(value), "Graph scene titles"); }

    /// <summary>Gets or sets an optional scene subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets graph nodes keyed by stable node id.</summary>
    public List<GraphSceneNode> Nodes { get; } = new();

    /// <summary>Gets graph edges keyed by stable edge id.</summary>
    public List<GraphSceneEdge> Edges { get; } = new();

    /// <summary>Gets semantic or computed clusters that an adapter can collapse, expand, or summarize.</summary>
    public List<GraphSceneCluster> Clusters { get; } = new();

    /// <summary>Gets reusable graph exploration settings and capability hints.</summary>
    public GraphSceneOptions Options { get; } = new();

    /// <summary>Gets arbitrary scene-level metadata for host adapters and diagnostics.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>
    /// Creates a new graph scene.
    /// </summary>
    /// <param name="id">Stable scene identifier.</param>
    /// <param name="title">Human-readable scene title.</param>
    /// <returns>A new graph scene.</returns>
    public static GraphScene Create(string id, string title) {
        return new GraphScene {
            Id = id,
            Title = title
        };
    }

    /// <summary>
    /// Validates node, edge, and cluster references before an adapter renders or mutates the scene.
    /// </summary>
    public void Validate() {
        ValidateOptions();
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var explicitNodeClusters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in Nodes) {
            ValidateRequiredId(node.Id, "node");
            ValidateRequiredText(node.Label, "node");
            ValidateFiniteValue(node.Size, node.Id, "size");
            if (node.HasExplicitPosition) {
                ValidateFiniteCoordinate(node.X, node.Id, "x");
                ValidateFiniteCoordinate(node.Y, node.Id, "y");
            }
            if (!nodeIds.Add(node.Id)) throw new InvalidOperationException("Graph scene contains a duplicate node id: " + node.Id);
            if (!string.IsNullOrWhiteSpace(node.ClusterId)) explicitNodeClusters[node.Id] = node.ClusterId!;
        }

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in Edges) {
            ValidateRequiredId(edge.Id, "edge");
            ValidateRequiredId(edge.SourceNodeId, "edge source node");
            ValidateRequiredId(edge.TargetNodeId, "edge target node");
            ValidateFiniteValue(edge.Weight, edge.Id, "weight");
            ValidateFiniteValue(edge.Length, edge.Id, "length");
            ValidateFiniteValue(edge.Curvature, edge.Id, "curvature");
            if (edge.Weight <= 0) throw new InvalidOperationException("Graph scene edge weight must be greater than zero: " + edge.Id);
            if (edge.Length < 0) throw new InvalidOperationException("Graph scene edge length must not be negative: " + edge.Id);
            if (!edgeIds.Add(edge.Id)) throw new InvalidOperationException("Graph scene contains a duplicate edge id: " + edge.Id);
            if (!nodeIds.Contains(edge.SourceNodeId)) throw new InvalidOperationException("Graph scene edge references a missing source node: " + edge.Id);
            if (!nodeIds.Contains(edge.TargetNodeId)) throw new InvalidOperationException("Graph scene edge references a missing target node: " + edge.Id);
        }

        var clusterIds = new HashSet<string>(StringComparer.Ordinal);
        var declaredNodeClusters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cluster in Clusters) {
            ValidateRequiredId(cluster.Id, "cluster");
            ValidateRequiredText(cluster.Label, "cluster");
            if (!clusterIds.Add(cluster.Id)) throw new InvalidOperationException("Graph scene contains a duplicate cluster id: " + cluster.Id);
            foreach (var nodeId in cluster.NodeIds) {
                ValidateRequiredId(nodeId, "cluster member node");
                if (!nodeIds.Contains(nodeId)) throw new InvalidOperationException("Graph scene cluster references a missing node: " + cluster.Id + " -> " + nodeId);
                if (declaredNodeClusters.TryGetValue(nodeId, out var declaredCluster) && !string.Equals(declaredCluster, cluster.Id, StringComparison.Ordinal)) throw new InvalidOperationException("Graph scene node is assigned to multiple declared clusters: " + nodeId);
                declaredNodeClusters[nodeId] = cluster.Id;
            }
        }

        foreach (var pair in explicitNodeClusters) {
            if (!clusterIds.Contains(pair.Value)) throw new InvalidOperationException("Graph scene node references a missing cluster: " + pair.Key + " -> " + pair.Value);
            if (declaredNodeClusters.TryGetValue(pair.Key, out var declaredCluster) && !string.Equals(declaredCluster, pair.Value, StringComparison.Ordinal)) throw new InvalidOperationException("Graph scene node cluster id conflicts with declared cluster membership: " + pair.Key);
        }
    }

    private void ValidateOptions() {
        if ((Options.Features & ~KnownFeatures) != GraphSceneFeatures.None) throw new InvalidOperationException("Graph scene features contain unsupported flags: " + (Options.Features & ~KnownFeatures));
        if (!Enum.IsDefined(typeof(GraphPhysicsSolver), Options.Physics.Solver)) throw new InvalidOperationException("Graph scene physics solver is unsupported: " + Options.Physics.Solver);
        if (Options.Physics.StabilizationIterations <= 0) throw new InvalidOperationException("Graph scene physics stabilization iterations must be greater than zero.");
        ValidatePositiveFinite(Options.Physics.MinVelocity, "physics min velocity");
        ValidatePositiveFinite(Options.Physics.MaxVelocity, "physics max velocity");
        if (Options.Physics.MaxVelocity < Options.Physics.MinVelocity) throw new InvalidOperationException("Graph scene physics max velocity must be greater than or equal to min velocity.");
        ValidatePositiveFinite(Options.Physics.Damping, "physics damping");
        if (Options.Physics.Damping >= 1) throw new InvalidOperationException("Graph scene physics damping must be less than one.");
        ValidatePositiveFinite(Options.Physics.LinkDistance, "physics link distance");
        ValidateNonNegativeFinite(Options.Physics.Repulsion, "physics repulsion");
        ValidateNonNegativeFinite(Options.Physics.CenterGravity, "physics center gravity");
        if (Options.LevelOfDetail.ClusterNodeThreshold < 0 || Options.LevelOfDetail.HideEdgeLabelsThreshold < 0 || Options.LevelOfDetail.CompactNodeThreshold < 0 || Options.LevelOfDetail.CanvasPreferredNodeThreshold < 0) throw new InvalidOperationException("Graph scene level-of-detail thresholds must not be negative.");
        if (Options.Performance.FrameBudgetMilliseconds <= 0) throw new InvalidOperationException("Graph scene performance frame budget must be greater than zero.");
        if (Options.Performance.MaxInteractiveSvgNodes < 0 || Options.Performance.MaxInteractiveSvgEdges < 0 || Options.Performance.MaxInteractiveCanvasNodes < 0 || Options.Performance.MaxInteractiveCanvasEdges < 0) throw new InvalidOperationException("Graph scene performance limits must not be negative.");
        if (Options.Performance.TelemetrySampleInterval <= 0) throw new InvalidOperationException("Graph scene performance telemetry interval must be greater than zero.");
    }

    private static void ValidateRequiredId(string? id, string itemKind) {
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Graph scene contains a blank " + itemKind + " id.");
    }

    private static void ValidateRequiredText(string? text, string itemKind) {
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Graph scene contains a blank " + itemKind + " label.");
    }

    private static void ValidateFiniteCoordinate(double value, string nodeId, string axis) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Graph scene node has a non-finite " + axis + "-coordinate: " + nodeId);
    }

    private static void ValidateFiniteValue(double value, string id, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Graph scene item has a non-finite " + name + ": " + id);
    }

    private static void ValidatePositiveFinite(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new InvalidOperationException("Graph scene option must be finite and greater than zero: " + name);
    }

    private static void ValidateNonNegativeFinite(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new InvalidOperationException("Graph scene option must be finite and non-negative: " + name);
    }
}
