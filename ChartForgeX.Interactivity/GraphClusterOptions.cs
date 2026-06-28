using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes reusable clustering behavior for graph adapters that can collapse, summarize, or derive large-scene communities.
/// </summary>
public sealed class GraphClusterOptions {
    /// <summary>Gets or sets how adapters should interpret clusters when the scene is rendered.</summary>
    public GraphClusterMode Mode { get; set; } = GraphClusterMode.Explicit;

    /// <summary>Gets or sets whether adapters may create runtime clusters from graph structure when explicit clusters are not enough.</summary>
    public bool Adaptive { get; set; }

    /// <summary>Gets or sets whether cluster summaries should start collapsed when supported by the adapter.</summary>
    public bool CollapseOnLoad { get; set; }

    /// <summary>Gets or sets the minimum member count before an adapter should present a cluster as collapsible.</summary>
    public int MinimumClusterSize { get; set; } = 2;

    /// <summary>Gets or sets the preferred maximum number of visible members before adaptive clustering should summarize a community.</summary>
    public int TargetClusterSize { get; set; } = 120;

    internal void Validate() {
        if (!Enum.IsDefined(typeof(GraphClusterMode), Mode)) throw new InvalidOperationException("Graph scene cluster mode is unsupported: " + Mode);
        if (MinimumClusterSize < 1) throw new InvalidOperationException("Graph scene cluster minimum size must be at least one.");
        if (TargetClusterSize < MinimumClusterSize) throw new InvalidOperationException("Graph scene cluster target size must be greater than or equal to the minimum size.");
    }
}

/// <summary>
/// Names reusable clustering strategies that adapters can map to Canvas, WebGL, desktop, or native graph controls.
/// </summary>
public enum GraphClusterMode {
    /// <summary>Use only clusters declared on the scene.</summary>
    Explicit,

    /// <summary>Derive cluster membership from node group ids while preserving explicit clusters that callers supplied.</summary>
    ByGroup,

    /// <summary>Let the adapter derive communities from graph structure and level-of-detail budgets.</summary>
    Adaptive,

    /// <summary>Use declared clusters first, then allow adaptive summaries for oversized communities.</summary>
    Hybrid
}
