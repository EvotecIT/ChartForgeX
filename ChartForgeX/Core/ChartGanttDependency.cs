using System;

namespace ChartForgeX.Core;

/// <summary>A finish-to-start visual link between two zero-based Gantt task indices.</summary>
public readonly struct ChartGanttDependency : IEquatable<ChartGanttDependency> {
    /// <summary>Creates a link between distinct, non-negative task indices.</summary>
    public ChartGanttDependency(int predecessorIndex, int successorIndex) {
        if (predecessorIndex < 0) throw new ArgumentOutOfRangeException(nameof(predecessorIndex));
        if (successorIndex < 0 || predecessorIndex == successorIndex) throw new ArgumentOutOfRangeException(nameof(successorIndex));
        PredecessorIndex = predecessorIndex;
        SuccessorIndex = successorIndex;
    }
    /// <summary>Gets the task whose finish anchors the link.</summary>
    public int PredecessorIndex { get; }
    /// <summary>Gets the task whose start anchors the link.</summary>
    public int SuccessorIndex { get; }
    /// <summary>Compares both task indices.</summary>
    public bool Equals(ChartGanttDependency other) => PredecessorIndex == other.PredecessorIndex && SuccessorIndex == other.SuccessorIndex;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChartGanttDependency other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => unchecked(PredecessorIndex * 397 ^ SuccessorIndex);
}
