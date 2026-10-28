using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Immutable optional display permutations for captured Chord nodes and directed endpoint slots.</summary>
/// <remarks>These sequences never reorder authored facts, paint traversal or source indexes. Each nonnull sequence
/// must name every captured node or flow exactly once, including zero flows and isolated nodes. Null uses source order.
/// Incoming and outgoing sequences apply independently at each node; a self flow retains two separate slots.</remarks>
public sealed class ChartChordOrdering {
    /// <summary>Gets the complete clockwise node-ID permutation, or null for source order.</summary>
    public IReadOnlyList<string>? NodeIds { get; }
    /// <summary>Gets the complete flow-ID permutation used for outgoing slots at each source, or null.</summary>
    public IReadOnlyList<string>? OutgoingFlowIds { get; }
    /// <summary>Gets the complete flow-ID permutation used for incoming slots at each target, or null.</summary>
    public IReadOnlyList<string>? IncomingFlowIds { get; }

    /// <summary>Copies optional complete ID permutations. Unknown or missing captured IDs fail native preparation.</summary>
    /// <param name="nodeIds">Clockwise node IDs. An empty sequence is valid only for an empty captured node set.</param>
    /// <param name="outgoingFlowIds">Flow IDs in outgoing-slot order. Relative order at other sources has no geometric effect.</param>
    /// <param name="incomingFlowIds">Flow IDs in incoming-slot order. Relative order at other targets has no geometric effect.</param>
    public ChartChordOrdering(IEnumerable<string>? nodeIds = null, IEnumerable<string>? outgoingFlowIds = null,
        IEnumerable<string>? incomingFlowIds = null) {
        NodeIds = Copy(nodeIds, nameof(nodeIds));
        OutgoingFlowIds = Copy(outgoingFlowIds, nameof(outgoingFlowIds));
        IncomingFlowIds = Copy(incomingFlowIds, nameof(incomingFlowIds));
    }

    private static IReadOnlyList<string>? Copy(IEnumerable<string>? source, string parameter) {
        if (source == null) return null;
        var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in source) {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                throw new ArgumentException("Chord ordering IDs must be non-empty and unique within each sequence.", parameter);
            result.Add(id);
        }
        return result.AsReadOnly();
    }
}
