using System;

namespace ChartForgeX.Interactivity;

/// <summary>Controls a bounded, deterministic neighborhood of a graph node.</summary>
public sealed class GraphSceneNeighborhoodOptions {
    /// <summary>Gets or sets the maximum undirected relationship distance from the root. Defaults to one hop.</summary>
    public int Hops { get; set; } = 1;

    /// <summary>Gets or sets the maximum number of visible nodes, including the root. Defaults to 40.</summary>
    public int MaximumNodes { get; set; } = 40;

    /// <summary>Gets or sets the maximum number of visible relationships. Defaults to 80; zero produces a node-only view.</summary>
    public int MaximumEdges { get; set; } = 80;

    /// <summary>Gets or sets how many neighbors to skip before filling the view. The root is always retained. Neighbors are ordered by distance, then ordinal id.</summary>
    public int NeighborOffset { get; set; }

    internal void Validate() {
        if (Hops < 0) throw new ArgumentOutOfRangeException(nameof(Hops));
        if (MaximumNodes < 2) throw new ArgumentOutOfRangeException(nameof(MaximumNodes), "A neighborhood needs room for its root and at least one neighbor.");
        if (MaximumEdges < 0) throw new ArgumentOutOfRangeException(nameof(MaximumEdges));
        if (NeighborOffset < 0) throw new ArgumentOutOfRangeException(nameof(NeighborOffset));
    }
}
