using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes deterministic graph layout preferences shared by static and interactive graph adapters.
/// </summary>
public sealed class GraphLayoutOptions {
    /// <summary>Gets or sets the prepared layout family used before adapter-side runtime physics begins.</summary>
    public GraphLayoutMode Mode { get; set; } = GraphLayoutMode.StructuredPrepared;

    /// <summary>Gets or sets the direction used by hierarchical layouts.</summary>
    public GraphLayoutDirection Direction { get; set; } = GraphLayoutDirection.TopToBottom;

    /// <summary>Gets or sets the distance between adjacent hierarchy levels in scene units.</summary>
    public double LevelSeparation { get; set; } = 120;

    /// <summary>Gets or sets the distance between nodes in the same hierarchy level in scene units.</summary>
    public double NodeSpacing { get; set; } = 92;

    /// <summary>Gets or sets additional distance between disconnected hierarchical components.</summary>
    public double ComponentSpacing { get; set; } = 120;

    /// <summary>Gets or sets whether missing hierarchy levels may be inferred from directed edges.</summary>
    public bool InferLevelsFromEdges { get; set; } = true;

    internal void Validate() {
        if (!Enum.IsDefined(typeof(GraphLayoutMode), Mode)) throw new InvalidOperationException("Graph scene layout mode is unsupported: " + Mode);
        if (!Enum.IsDefined(typeof(GraphLayoutDirection), Direction)) throw new InvalidOperationException("Graph scene layout direction is unsupported: " + Direction);
        ValidatePositiveFinite(LevelSeparation, "layout level separation");
        ValidatePositiveFinite(NodeSpacing, "layout node spacing");
        ValidateNonNegativeFinite(ComponentSpacing, "layout component spacing");
    }

    private static void ValidatePositiveFinite(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new InvalidOperationException("Graph scene option must be finite and greater than zero: " + name);
    }

    private static void ValidateNonNegativeFinite(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new InvalidOperationException("Graph scene option must be finite and non-negative: " + name);
    }
}

/// <summary>
/// Names deterministic graph layout families that adapters can share.
/// </summary>
public enum GraphLayoutMode {
    /// <summary>Use ChartForgeX's graph-aware prepared layout.</summary>
    StructuredPrepared,

    /// <summary>Place nodes in deterministic hierarchy levels, similar to vis-network hierarchical layout.</summary>
    Hierarchical
}

/// <summary>
/// Names hierarchy flow directions for layered graph layouts.
/// </summary>
public enum GraphLayoutDirection {
    /// <summary>Place parent or source levels above children.</summary>
    TopToBottom,

    /// <summary>Place parent or source levels below children.</summary>
    BottomToTop,

    /// <summary>Place parent or source levels at the left.</summary>
    LeftToRight,

    /// <summary>Place parent or source levels at the right.</summary>
    RightToLeft
}
