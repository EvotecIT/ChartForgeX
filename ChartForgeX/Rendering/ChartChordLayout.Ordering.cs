using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartChordLayout {
    /// <summary>Resolves a display permutation without changing source-indexed model collections.</summary>
    private static int[] ResolveOrder(IReadOnlyList<string>? requested, IReadOnlyList<string> source, string role) {
        if (requested == null) return Enumerable.Range(0, source.Count).ToArray();
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < source.Count; index++) indexes.Add(source[index], index);
        var result = new int[requested.Count]; var selected = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < requested.Count; index++) {
            var id = requested[index];
            if (!indexes.TryGetValue(id, out result[index]))
                throw new InvalidOperationException("Chord " + role + " order contains unknown ID '" + id + "'.");
            selected.Add(id);
        }
        foreach (var id in source) if (!selected.Contains(id))
            throw new InvalidOperationException("Chord " + role + " order is missing captured ID '" + id + "'.");
        return result;
    }

    /// <summary>Allocates source and target slots independently; source-indexed links remain unchanged.</summary>
    private static void OrderedSlots(ChartChordModel model, int[] outgoingOrder, int[] incomingOrder, double[] outgoing, double[] incoming) {
        foreach (var index in outgoingOrder) {
            var link = model.Links[index]; link.SourceStart = outgoing[link.Source]; outgoing[link.Source] += link.Sweep;
        }
        foreach (var index in incomingOrder) {
            var link = model.Links[index]; link.TargetStart = incoming[link.Target]; incoming[link.Target] += link.Sweep;
        }
    }
}
