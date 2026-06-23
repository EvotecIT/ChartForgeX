using System;
using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a host-neutral graph exploration scene that adapters can render with SVG, Canvas, WebGL, desktop, or native controls.
/// </summary>
public sealed class GraphScene {
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
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Nodes) {
            ValidateRequiredId(node.Id, "node");
            ValidateRequiredText(node.Label, "node");
            ValidateFiniteValue(node.Size, node.Id, "size");
            if (node.HasExplicitPosition) {
                ValidateFiniteCoordinate(node.X, node.Id, "x");
                ValidateFiniteCoordinate(node.Y, node.Id, "y");
            }
            if (!nodeIds.Add(node.Id)) throw new InvalidOperationException("Graph scene contains a duplicate node id: " + node.Id);
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
        foreach (var cluster in Clusters) {
            ValidateRequiredId(cluster.Id, "cluster");
            ValidateRequiredText(cluster.Label, "cluster");
            if (!clusterIds.Add(cluster.Id)) throw new InvalidOperationException("Graph scene contains a duplicate cluster id: " + cluster.Id);
            foreach (var nodeId in cluster.NodeIds) {
                ValidateRequiredId(nodeId, "cluster member node");
                if (!nodeIds.Contains(nodeId)) throw new InvalidOperationException("Graph scene cluster references a missing node: " + cluster.Id + " -> " + nodeId);
            }
        }

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
}
