using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>Describes one deterministic hierarchy view of a graph scene.</summary>
public sealed class GraphSceneStage {
    internal GraphSceneStage(int index, int depth, string name, bool isFullScene, string? rootNodeId, string[] visibleNodeIds, string[] visibleEdgeIds, string[] frontierNodeIds, int totalNodeCount) {
        Index = index;
        Depth = depth;
        Name = name;
        IsFullScene = isFullScene;
        RootNodeId = rootNodeId;
        VisibleNodeIds = visibleNodeIds;
        VisibleEdgeIds = visibleEdgeIds;
        FrontierNodeIds = frontierNodeIds;
        HiddenNodeCount = totalNodeCount - visibleNodeIds.Length;
    }

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
}
