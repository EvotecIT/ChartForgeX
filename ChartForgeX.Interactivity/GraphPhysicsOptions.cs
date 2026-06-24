namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes graph physics and stabilization settings without binding callers to a browser implementation.
/// </summary>
public sealed class GraphPhysicsOptions {
    /// <summary>Gets or sets the solver profile requested from graph adapters.</summary>
    public GraphPhysicsSolver Solver { get; set; } = GraphPhysicsSolver.StaticPrepared;

    /// <summary>Gets or sets the maximum stabilization iterations before the adapter should stop the simulation.</summary>
    public int StabilizationIterations { get; set; } = 1000;

    /// <summary>Gets or sets the velocity threshold below which an adapter may consider the graph stabilized.</summary>
    public double MinVelocity { get; set; } = 0.1;

    /// <summary>Gets or sets the maximum node velocity allowed by runtime physics adapters.</summary>
    public double MaxVelocity { get; set; } = 50;

    /// <summary>Gets or sets how much velocity is retained from one simulation tick to the next.</summary>
    public double Damping { get; set; } = 0.09;

    /// <summary>Gets or sets the default spring length for edges that do not specify a custom length.</summary>
    public double LinkDistance { get; set; } = 120;

    /// <summary>Gets or sets the repulsion strength used by force-style adapters.</summary>
    public double Repulsion { get; set; } = 4500;

    /// <summary>Gets or sets the center gravity used to keep disconnected components near the viewport.</summary>
    public double CenterGravity { get; set; } = 0.01;

    /// <summary>Gets or sets whether the adapter may change its timestep during stabilization.</summary>
    public bool AdaptiveTimestep { get; set; } = true;
}

/// <summary>
/// Names graph physics solver families that adapters may implement.
/// </summary>
public enum GraphPhysicsSolver {
    /// <summary>Do not run physics; render caller-provided or server-prepared positions.</summary>
    None,

    /// <summary>Use deterministic positions prepared by ChartForgeX before adapter rendering.</summary>
    StaticPrepared,

    /// <summary>Use a simple force-directed solver with link attraction and node repulsion.</summary>
    ForceDirected,

    /// <summary>Use a Barnes-Hut style approximation for larger relationship graphs.</summary>
    BarnesHut,

    /// <summary>Use a ForceAtlas2-style solver profile for dense relationship exploration.</summary>
    ForceAtlas2,

    /// <summary>Use a hierarchical repulsion profile for directed layered graphs.</summary>
    HierarchicalRepulsion
}
