using System;

namespace ChartForgeX.Interactivity;

/// <summary>Controls reusable graph pointer and topology-change behavior.</summary>
public sealed class GraphInteractionOptions {
    /// <summary>Gets or sets what happens to a node after it is dragged.</summary>
    public GraphNodeDragBehavior NodeDragBehavior { get; set; } = GraphNodeDragBehavior.ReleaseAndReheat;

    /// <summary>Gets or sets whether connected movable nodes continue responding while a node is held.</summary>
    public bool SimulateConnectedNodesWhileDragging { get; set; } = true;

    /// <summary>Gets or sets how much recent pointer velocity is transferred to a released node, from zero to one.</summary>
    public double DragMomentum { get; set; } = 0.18;

    /// <summary>Gets or sets whether runtime physics reheats after a node is dropped.</summary>
    public bool ReheatAfterDrag { get; set; } = true;

    /// <summary>Gets or sets whether runtime physics reheats after clusters expand or collapse.</summary>
    public bool ReheatAfterClusterChange { get; set; } = true;

    /// <summary>Gets or sets whether runtime physics reheats after hierarchy navigation changes visible nodes.</summary>
    public bool ReheatAfterHierarchyChange { get; set; } = true;

    /// <summary>Gets or sets whether runtime physics reheats after incremental nodes, edges, or clusters change.</summary>
    public bool ReheatAfterGraphChange { get; set; } = true;

    internal void Validate() {
        if (!Enum.IsDefined(typeof(GraphNodeDragBehavior), NodeDragBehavior)) throw new InvalidOperationException("Graph scene node drag behavior is unsupported: " + NodeDragBehavior);
        GraphPhysicsOptions.UnitInterval(DragMomentum, "drag momentum");
    }
}

/// <summary>Names supported node release policies after interactive dragging.</summary>
public enum GraphNodeDragBehavior {
    /// <summary>Keep the node fixed at its dropped position while allowing the remaining graph to settle.</summary>
    PinOnDrop,

    /// <summary>Restore the node's original fixed state and reheat the graph so springs settle into a new equilibrium.</summary>
    ReleaseAndReheat
}
