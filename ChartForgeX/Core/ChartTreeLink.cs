using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one parent-child relationship in a tree chart.
/// </summary>
public readonly struct ChartTreeLink {
    /// <summary>
    /// Gets the parent node ID.
    /// </summary>
    public string ParentId { get; }

    /// <summary>
    /// Gets the child node ID, which also identifies its incoming branch.
    /// </summary>
    public string ChildId { get; }

    /// <summary>
    /// Gets the optional positive node weight.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new tree link.
    /// </summary>
    public ChartTreeLink(string parentId, string childId, double value = 1) {
        if (string.IsNullOrWhiteSpace(parentId)) throw new ArgumentException("Tree parent ID must not be empty.", nameof(parentId));
        if (string.IsNullOrWhiteSpace(childId)) throw new ArgumentException("Tree child ID must not be empty.", nameof(childId));
        ChartGuards.Finite(value, nameof(value));
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Tree link value must be positive.");
        ParentId = parentId;
        ChildId = childId;
        Value = value;
    }
}
