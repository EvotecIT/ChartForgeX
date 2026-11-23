using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {

    /// <summary>Adds one colour band to a striped edge. Two or more stripes replace the single edge colour; labelled stripes get a legend entry.</summary>
    public static TopologyChart AddEdgeStripe(this TopologyChart chart, string edgeId, string color, string? label = null) {
        var edge = RequiredEdge(chart, edgeId);
        edge.Stripes.Add(new TopologyEdgeStripe { Color = RequiredText(color, nameof(color), "Topology edge stripe colours"), Label = string.IsNullOrWhiteSpace(label) ? null : label!.Trim() });
        return chart;
    }

    /// <summary>Adds a small monospace value chip along an edge, such as PoE watts, replication cost or lag.</summary>
    /// <remarks>A default <paramref name="tone"/> is <see cref="Tone.Neutral"/>; pass <see cref="Tone.None"/> to follow the edge status.</remarks>
    public static TopologyChart AddEdgeChip(this TopologyChart chart, string edgeId, string text, Tone? tone = null, string? iconId = null) {
        var edge = RequiredEdge(chart, edgeId);
        edge.Chips.Add(new TopologyEdgeChip { Text = RequiredText(text, nameof(text), "Topology edge chip text"), Tone = tone ?? Tone.Neutral, IconId = string.IsNullOrWhiteSpace(iconId) ? null : iconId!.Trim() });
        return chart;
    }
}
