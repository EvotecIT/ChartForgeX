using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>Describes a deterministic hierarchy or neighborhood view of a graph scene.</summary>
public sealed class GraphSceneStage {
    internal GraphSceneStage(int index, int depth, string name, bool isFullScene, string? rootNodeId, string[] visibleNodeIds, string[] visibleEdgeIds, string[] frontierNodeIds, int totalNodeCount, int totalEdgeCount, int boundaryEdgeCount = 0, int? scopeNodeCount = null, GraphSceneStageKind kind = GraphSceneStageKind.Hierarchy) {
        Kind = kind;
        Index = index;
        Depth = depth;
        Name = name;
        IsFullScene = isFullScene;
        RootNodeId = rootNodeId;
        VisibleNodeIds = System.Array.AsReadOnly(visibleNodeIds);
        VisibleEdgeIds = System.Array.AsReadOnly(visibleEdgeIds);
        FrontierNodeIds = System.Array.AsReadOnly(frontierNodeIds);
        HiddenNodeCount = totalNodeCount - visibleNodeIds.Length;
        HiddenEdgeCount = totalEdgeCount - visibleEdgeIds.Length;
        BoundaryEdgeCount = boundaryEdgeCount;
        ScopeNodeCount = scopeNodeCount ?? totalNodeCount;
    }

    /// <summary>Gets the planning strategy that produced this view.</summary>
    public GraphSceneStageKind Kind { get; }

    /// <summary>Gets the one-based stage index used for stable file names.</summary>
    public int Index { get; }

    /// <summary>Gets the maximum descendant depth visible in this stage.</summary>
    public int Depth { get; }

    /// <summary>Gets a stable, human-readable stage name.</summary>
    public string Name { get; }

    /// <summary>Gets whether this stage contains the complete selected hierarchy scope.</summary>
    public bool IsFullScene { get; }

    /// <summary>Gets the optional root used to plan this stage.</summary>
    public string? RootNodeId { get; }

    /// <summary>Gets stable node ids visible in this stage.</summary>
    public IReadOnlyList<string> VisibleNodeIds { get; }

    /// <summary>Gets stable edge ids whose endpoints are both visible in this stage.</summary>
    public IReadOnlyList<string> VisibleEdgeIds { get; }

    /// <summary>Gets visible nodes that summarize descendants hidden by this stage depth.</summary>
    public IReadOnlyList<string> FrontierNodeIds { get; }

    /// <summary>Gets the number of nodes hidden outside this stage.</summary>
    public int HiddenNodeCount { get; }

    /// <summary>Gets the number of source relationships not included in this view, including relationships outside its scope.</summary>
    public int HiddenEdgeCount { get; }

    /// <summary>Gets the number of source relationships with exactly one endpoint in this view.</summary>
    public int BoundaryEdgeCount { get; }

    /// <summary>Gets the eligible node count before paging. Neighborhoods exclude hidden nodes and honor the hop limit; hierarchy stages report the selected subtree.</summary>
    public int ScopeNodeCount { get; }
}

/// <summary>Identifies how a graph view was selected.</summary>
public enum GraphSceneStageKind {
    /// <summary>A hierarchy selected by descendant depth.</summary>
    Hierarchy,
    /// <summary>A bounded page of nodes selected by relationship distance.</summary>
    Neighborhood
}
