using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one weighted directed flow between authored node IDs.
/// </summary>
public readonly struct ChartFlowLink {
    /// <summary>
    /// Gets the authored flow ID. Parallel flows have distinct IDs.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the source node ID.
    /// </summary>
    public string SourceId { get; }

    /// <summary>Gets the target node ID.</summary>
    public string TargetId { get; }

    /// <summary>
    /// Gets the non-negative raw flow value. Individual chart families may require positive values.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a directed flow with an explicit identity and node references.
    /// </summary>
    public ChartFlowLink(string id, string sourceId, string targetId, double value) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Flow ID must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Flow source ID must not be empty.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Flow target ID must not be empty.", nameof(targetId));
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Flow value must be non-negative.");
        Id = id;
        SourceId = sourceId;
        TargetId = targetId;
        Value = value;
    }
}
