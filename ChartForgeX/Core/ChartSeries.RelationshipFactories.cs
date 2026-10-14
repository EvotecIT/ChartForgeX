using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates a configured Sankey source with independent series, node and flow identities.</summary>
    public static ChartSeries CreateSankey(ChartSeriesId id, string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color = null) =>
        RelationshipSeries(new ChartSeries(id, name, ChartSeriesKind.Sankey, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Sankey(nodes, links), color);

    /// <summary>Creates directed weighted Chord data, retaining parallel, reciprocal and self flow identities.</summary>
    public static ChartSeries CreateChord(ChartSeriesId id, string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color = null) =>
        RelationshipSeries(new ChartSeries(id, name, ChartSeriesKind.Chord, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Chord(nodes, links), color);

    /// <summary>Creates a connected Tree source whose node IDs remain independent of labels and ordering.</summary>
    public static ChartSeries CreateTree(ChartSeriesId id, string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, ChartColor? color = null) =>
        RelationshipSeries(new ChartSeries(id, name, ChartSeriesKind.Tree, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Hierarchy(nodes, links), color);

    /// <summary>Creates a Treemap forest with copied authored group and leaf identities.</summary>
    public static ChartSeries CreateTreemap(ChartSeriesId id, string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color = null) =>
        RelationshipSeries(new ChartSeries(id, name, ChartSeriesKind.Treemap, Array.Empty<ChartPoint>()), ChartRelationshipIndex.AreaHierarchy(items, allowForest: true, requireNullGroups: true), color);

    /// <summary>Creates a single-root Sunburst source. Parent-size policy is validated against its chart during preparation.</summary>
    public static ChartSeries CreateSunburst(ChartSeriesId id, string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color = null) =>
        RelationshipSeries(new ChartSeries(id, name, ChartSeriesKind.Sunburst, Array.Empty<ChartPoint>()), ChartRelationshipIndex.AreaHierarchy(items, allowForest: false, requireNullGroups: false), color);

    internal static ChartSeries CreateSankey(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color) =>
        RelationshipSeries(new ChartSeries(name, ChartSeriesKind.Sankey, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Sankey(nodes, links), color);
    internal static ChartSeries CreateChord(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color) =>
        RelationshipSeries(new ChartSeries(name, ChartSeriesKind.Chord, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Chord(nodes, links), color);
    internal static ChartSeries CreateTree(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, ChartColor? color) =>
        RelationshipSeries(new ChartSeries(name, ChartSeriesKind.Tree, Array.Empty<ChartPoint>()), ChartRelationshipIndex.Hierarchy(nodes, links), color);
    internal static ChartSeries CreateTreemap(string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color) =>
        RelationshipSeries(new ChartSeries(name, ChartSeriesKind.Treemap, Array.Empty<ChartPoint>()), ChartRelationshipIndex.AreaHierarchy(items, allowForest: true, requireNullGroups: true), color);
    internal static ChartSeries CreateSunburst(string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color) =>
        RelationshipSeries(new ChartSeries(name, ChartSeriesKind.Sunburst, Array.Empty<ChartPoint>()), ChartRelationshipIndex.AreaHierarchy(items, allowForest: false, requireNullGroups: false), color);

    private static ChartSeries RelationshipSeries(ChartSeries series, ChartRelationshipIndex facts, ChartColor? color) {
        series.Color = color;
        series.SetRelationships(facts);
        return series;
    }
}
