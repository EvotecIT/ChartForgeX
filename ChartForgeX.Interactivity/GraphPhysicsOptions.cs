using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes graph physics, solver tuning, and stabilization without binding callers to a browser implementation.
/// </summary>
public sealed class GraphPhysicsOptions {
    /// <summary>Gets or sets the solver requested from graph adapters.</summary>
    public GraphPhysicsSolver Solver { get; set; } = GraphPhysicsSolver.StaticPrepared;

    /// <summary>Gets common stabilization settings.</summary>
    public GraphPhysicsStabilizationOptions Stabilization { get; } = new();

    /// <summary>Gets or sets the velocity threshold below which a graph is considered stabilized.</summary>
    public double MinVelocity { get; set; } = 0.1;

    /// <summary>Gets or sets the maximum node velocity allowed by runtime physics adapters.</summary>
    public double MaxVelocity { get; set; } = 50;

    /// <summary>Gets or sets the minimum discrete simulation timestep.</summary>
    public double Timestep { get; set; } = 0.5;

    /// <summary>Gets or sets whether the adapter may increase its timestep while stabilizing.</summary>
    public bool AdaptiveTimestep { get; set; } = true;

    /// <summary>Gets Barnes-Hut solver settings.</summary>
    public GraphBarnesHutPhysicsOptions BarnesHut { get; } = new();

    /// <summary>Gets ForceAtlas2-style solver settings.</summary>
    public GraphForceAtlas2PhysicsOptions ForceAtlas2 { get; } = new();

    /// <summary>Gets direct repulsion solver settings.</summary>
    public GraphRepulsionPhysicsOptions Repulsion { get; } = new();

    /// <summary>Gets hierarchical repulsion solver settings.</summary>
    public GraphHierarchicalRepulsionPhysicsOptions HierarchicalRepulsion { get; } = new();

    internal void Validate() {
        if (!Enum.IsDefined(typeof(GraphPhysicsSolver), Solver)) throw new InvalidOperationException("Graph scene physics solver is unsupported: " + Solver);
        Stabilization.Validate();
        Positive(MinVelocity, "physics min velocity");
        Positive(MaxVelocity, "physics max velocity");
        if (MaxVelocity < MinVelocity) throw new InvalidOperationException("Graph scene physics max velocity must be greater than or equal to min velocity.");
        Positive(Timestep, "physics timestep");
        BarnesHut.Validate("Barnes-Hut");
        ForceAtlas2.Validate("ForceAtlas2");
        Repulsion.Validate("repulsion");
        HierarchicalRepulsion.Validate("hierarchical repulsion");
    }

    internal static void Positive(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new InvalidOperationException("Graph scene " + name + " must be finite and greater than zero.");
    }

    internal static void NonNegative(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new InvalidOperationException("Graph scene " + name + " must be finite and non-negative.");
    }

    internal static void UnitInterval(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new InvalidOperationException("Graph scene " + name + " must be between zero and one.");
    }
}

/// <summary>Controls initial and reheated graph stabilization.</summary>
public sealed class GraphPhysicsStabilizationOptions {
    /// <summary>Gets or sets whether runtime adapters should stabilize automatically on load.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the maximum simulation iterations in one stabilization run.</summary>
    public int Iterations { get; set; } = 1000;

    /// <summary>Gets or sets how often a worker reports stabilization progress.</summary>
    public int UpdateInterval { get; set; } = 4;

    /// <summary>Gets or sets whether the viewport should fit after automatic stabilization.</summary>
    public bool Fit { get; set; } = true;

    internal void Validate() {
        if (Iterations <= 0) throw new InvalidOperationException("Graph scene physics stabilization iterations must be greater than zero.");
        if (UpdateInterval <= 0) throw new InvalidOperationException("Graph scene physics stabilization update interval must be greater than zero.");
    }
}

/// <summary>Contains the spring, damping, gravity, and overlap settings shared by force solvers.</summary>
public abstract class GraphPhysicsSolverOptions {
    /// <summary>Gets or sets the attraction toward the graph center.</summary>
    public double CentralGravity { get; set; }

    /// <summary>Gets or sets the resting length of physics-enabled edges.</summary>
    public double SpringLength { get; set; }

    /// <summary>Gets or sets the strength of edge springs.</summary>
    public double SpringConstant { get; set; }

    /// <summary>Gets or sets how much velocity is discarded per simulation tick.</summary>
    public double Damping { get; set; }

    /// <summary>Gets or sets how strongly node dimensions contribute to collision avoidance, from zero to one.</summary>
    public double AvoidOverlap { get; set; }

    internal virtual void Validate(string solverName) {
        GraphPhysicsOptions.NonNegative(CentralGravity, solverName + " central gravity");
        GraphPhysicsOptions.Positive(SpringLength, solverName + " spring length");
        GraphPhysicsOptions.Positive(SpringConstant, solverName + " spring constant");
        GraphPhysicsOptions.UnitInterval(Damping, solverName + " damping");
        GraphPhysicsOptions.UnitInterval(AvoidOverlap, solverName + " overlap avoidance");
    }
}

/// <summary>Configures quadtree-accelerated Barnes-Hut gravity and springs.</summary>
public sealed class GraphBarnesHutPhysicsOptions : GraphPhysicsSolverOptions {
    /// <summary>Creates vis-network-like Barnes-Hut defaults.</summary>
    public GraphBarnesHutPhysicsOptions() {
        CentralGravity = 0.3;
        SpringLength = 95;
        SpringConstant = 0.04;
        Damping = 0.09;
    }

    /// <summary>Gets or sets the quadtree approximation threshold; larger values are faster and less precise.</summary>
    public double Theta { get; set; } = 0.5;

    /// <summary>Gets or sets the negative gravitational constant used as node repulsion.</summary>
    public double GravitationalConstant { get; set; } = -2000;

    internal override void Validate(string solverName) {
        base.Validate(solverName);
        GraphPhysicsOptions.Positive(Theta, solverName + " theta");
        if (double.IsNaN(GravitationalConstant) || double.IsInfinity(GravitationalConstant) || GravitationalConstant >= 0) throw new InvalidOperationException("Graph scene " + solverName + " gravitational constant must be finite and negative.");
    }
}

/// <summary>Configures a degree-weighted ForceAtlas2-style gravity and spring model.</summary>
public sealed class GraphForceAtlas2PhysicsOptions : GraphPhysicsSolverOptions {
    /// <summary>Creates vis-network-like ForceAtlas2 defaults.</summary>
    public GraphForceAtlas2PhysicsOptions() {
        CentralGravity = 0.01;
        SpringLength = 100;
        SpringConstant = 0.08;
        Damping = 0.4;
    }

    /// <summary>Gets or sets the quadtree approximation threshold.</summary>
    public double Theta { get; set; } = 0.5;

    /// <summary>Gets or sets the negative degree-weighted gravitational constant.</summary>
    public double GravitationalConstant { get; set; } = -50;

    internal override void Validate(string solverName) {
        base.Validate(solverName);
        GraphPhysicsOptions.Positive(Theta, solverName + " theta");
        if (double.IsNaN(GravitationalConstant) || double.IsInfinity(GravitationalConstant) || GravitationalConstant >= 0) throw new InvalidOperationException("Graph scene " + solverName + " gravitational constant must be finite and negative.");
    }
}

/// <summary>Configures direct distance-limited node repulsion and edge springs.</summary>
public sealed class GraphRepulsionPhysicsOptions : GraphPhysicsSolverOptions {
    /// <summary>Creates vis-network-like direct repulsion defaults.</summary>
    public GraphRepulsionPhysicsOptions() {
        CentralGravity = 0.2;
        SpringLength = 200;
        SpringConstant = 0.05;
        Damping = 0.09;
    }

    /// <summary>Gets or sets the effective radius of each node's repulsion field.</summary>
    public double NodeDistance { get; set; } = 100;

    /// <summary>Gets or sets a multiplier for direct repulsion forces.</summary>
    public double Strength { get; set; } = 1;

    internal override void Validate(string solverName) {
        base.Validate(solverName);
        GraphPhysicsOptions.Positive(NodeDistance, solverName + " node distance");
        GraphPhysicsOptions.Positive(Strength, solverName + " strength");
    }
}

/// <summary>Configures level-aware repulsion and springs for directed hierarchical graphs.</summary>
public sealed class GraphHierarchicalRepulsionPhysicsOptions : GraphPhysicsSolverOptions {
    /// <summary>Creates vis-network-like hierarchical repulsion defaults.</summary>
    public GraphHierarchicalRepulsionPhysicsOptions() {
        CentralGravity = 0;
        SpringLength = 100;
        SpringConstant = 0.01;
        Damping = 0.09;
    }

    /// <summary>Gets or sets the preferred distance between nodes on the same hierarchy level.</summary>
    public double NodeDistance { get; set; } = 120;

    /// <summary>Gets or sets a multiplier for level-aware repulsion forces.</summary>
    public double Strength { get; set; } = 1;

    internal override void Validate(string solverName) {
        base.Validate(solverName);
        GraphPhysicsOptions.Positive(NodeDistance, solverName + " node distance");
        GraphPhysicsOptions.Positive(Strength, solverName + " strength");
    }
}

/// <summary>Names graph physics solver families implemented by runtime adapters.</summary>
public enum GraphPhysicsSolver {
    /// <summary>Do not run physics; render caller-provided or server-prepared positions.</summary>
    None,

    /// <summary>Use deterministic positions prepared by ChartForgeX before adapter rendering.</summary>
    StaticPrepared,

    /// <summary>Use distance-limited node repulsion with edge springs.</summary>
    Repulsion,

    /// <summary>Use a Barnes-Hut quadtree for scalable inverse-square repulsion.</summary>
    BarnesHut,

    /// <summary>Use a degree-weighted ForceAtlas2-style solver accelerated by a quadtree for large scenes.</summary>
    ForceAtlas2,

    /// <summary>Use level-aware repulsion for directed layered graphs.</summary>
    HierarchicalRepulsion
}
