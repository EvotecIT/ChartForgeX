using System;

namespace ChartForgeX.Core;

/// <summary>A node whose stable identity is independent of its display label.</summary>
public readonly struct ChartNode {
    /// <summary>Gets the authored identity used by links, states, and interactive targets.</summary>
    public string Id { get; }

    /// <summary>Gets the display label. Different nodes may have the same label.</summary>
    public string Label { get; }

    /// <summary>Creates a node with an explicit identity and display label.</summary>
    public ChartNode(string id, string label) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Node ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Node label must not be empty.", nameof(label));
        Id = id;
        Label = label;
    }
}
