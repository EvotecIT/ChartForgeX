using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>Controls deterministic hierarchy stages used by static and interactive graph adapters.</summary>
public sealed class GraphSceneStageOptions {
    private string? _rootNodeId;

    /// <summary>Gets or sets the requested number of automatically selected stages. The planner may return fewer stages when the hierarchy has fewer distinct depths.</summary>
    public int StageCount { get; set; } = 5;

    /// <summary>Gets explicit descendant depths to render. When empty, depths are selected evenly from overview through the deepest level.</summary>
    public List<int> Depths { get; } = new();

    /// <summary>Gets or sets an optional hierarchy root. Null plans all top-level trees in the scene.</summary>
    public string? RootNodeId { get => _rootNodeId; set => _rootNodeId = ChartInteractionText.OptionalToken(value, nameof(value), "Graph stage root ids"); }

    /// <summary>Gets or sets whether the deepest complete view is always included.</summary>
    public bool IncludeFullScene { get; set; } = true;

    internal void Validate() {
        if (StageCount < 1 || StageCount > 50) throw new System.InvalidOperationException("Graph scene stage count must be between 1 and 50.");
        for (var index = 0; index < Depths.Count; index++) {
            if (Depths[index] < 0) throw new System.InvalidOperationException("Graph scene stage depths must not be negative.");
        }
    }
}
