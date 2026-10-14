using System;

namespace ChartForgeX.Core;

/// <summary>A detached finish-to-start relationship between stable Gantt series identities.</summary>
public readonly struct ChartGanttDependency : IEquatable<ChartGanttDependency> {
    /// <summary>Creates a relationship snapshot between two distinct series identities.</summary>
    public ChartGanttDependency(ChartSeriesId predecessorId, ChartSeriesId successorId) {
        predecessorId.Validate(); successorId.Validate();
        if (predecessorId == successorId) throw new ArgumentException("A Gantt dependency requires two distinct series.", nameof(successorId));
        PredecessorId = predecessorId;
        SuccessorId = successorId;
    }
    /// <summary>Gets the stable identity of the task whose finish anchors the link.</summary>
    public ChartSeriesId PredecessorId { get; }
    /// <summary>Gets the stable identity of the task whose start anchors the link.</summary>
    public ChartSeriesId SuccessorId { get; }
    /// <summary>Compares both stable series identities.</summary>
    public bool Equals(ChartGanttDependency other) => PredecessorId == other.PredecessorId && SuccessorId == other.SuccessorId;
    /// <summary>Compares both stable series identities when <paramref name="obj"/> is a <see cref="ChartGanttDependency"/>.</summary>
    public override bool Equals(object? obj) => obj is ChartGanttDependency other && Equals(other);
    /// <summary>Returns a hash code built from both stable series identities.</summary>
    public override int GetHashCode() => unchecked(PredecessorId.GetHashCode() * 397 ^ SuccessorId.GetHashCode());
}

// Ordinals describe the current rendered scene only; they never own an authored relationship.
internal readonly struct ChartGanttOrdinalDependency {
    internal ChartGanttOrdinalDependency(int predecessorIndex, int successorIndex) { PredecessorIndex = predecessorIndex; SuccessorIndex = successorIndex; }
    internal int PredecessorIndex { get; }
    internal int SuccessorIndex { get; }
}
