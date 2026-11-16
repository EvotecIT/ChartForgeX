using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>Records whether an edge is confirmed or assumed. Assumed edges render dashed and the text joins the tooltip.</summary>
    public static TopologyChart WithEdgeEvidence(this TopologyChart chart, string edgeId, TopologyEdgeEvidence evidence, string? text = null) {
        var edge = RequiredEdge(chart, edgeId);
        ValidateEnum(typeof(TopologyEdgeEvidence), evidence, nameof(evidence), "Topology edge evidence values");
        edge.Evidence = evidence;
        edge.EvidenceText = string.IsNullOrWhiteSpace(text) ? null : text!.Trim();
        return chart;
    }

    /// <summary>Adds one colour band to a striped edge. Two or more stripes replace the single edge colour; labelled stripes get a legend entry.</summary>
    public static TopologyChart AddEdgeStripe(this TopologyChart chart, string edgeId, string color, string? label = null) {
        var edge = RequiredEdge(chart, edgeId);
        edge.Stripes.Add(new TopologyEdgeStripe { Color = RequiredText(color, nameof(color), "Topology edge stripe colours"), Label = string.IsNullOrWhiteSpace(label) ? null : label!.Trim() });
        return chart;
    }

    /// <summary>Adds a small monospace value chip along an edge, such as PoE watts, replication cost or lag.</summary>
    public static TopologyChart AddEdgeChip(this TopologyChart chart, string edgeId, string text, TopologyTone tone = TopologyTone.Neutral, string? iconId = null) {
        var edge = RequiredEdge(chart, edgeId);
        ValidateEnum(typeof(TopologyTone), tone, nameof(tone), "Topology tones");
        edge.Chips.Add(new TopologyEdgeChip { Text = RequiredText(text, nameof(text), "Topology edge chip text"), Tone = tone, IconId = string.IsNullOrWhiteSpace(iconId) ? null : iconId!.Trim() });
        return chart;
    }

    /// <summary>Marks the child end of an edge with an uplink marker (↑) on its endpoint label.</summary>
    public static TopologyChart WithEdgeUplink(this TopologyChart chart, string edgeId, TopologyEdgeEnd childEnd) {
        var edge = RequiredEdge(chart, edgeId);
        ValidateEnum(typeof(TopologyEdgeEnd), childEnd, nameof(childEnd), "Topology edge ends");
        edge.UplinkEnd = childEnd;
        return chart;
    }

    /// <summary>Turns on sibling grouping: three or more same-kind leaves under one parent become a titled card, large cards start collapsed, and long child rows are staggered.</summary>
    public static TopologyChart WithSiblingGrouping(this TopologyChart chart, int minimumCount = 3, int staggerCount = 9, int maximumRows = 8, int collapseCount = 12, Func<TopologyNodeKind, System.Collections.Generic.IReadOnlyList<TopologyNode>, string>? titleFormatter = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.SiblingGrouping = new TopologySiblingGrouping { MinimumCount = minimumCount, StaggerCount = staggerCount, MaximumRows = maximumRows, CollapseCount = collapseCount, TitleFormatter = titleFormatter };
        return chart;
    }
}
