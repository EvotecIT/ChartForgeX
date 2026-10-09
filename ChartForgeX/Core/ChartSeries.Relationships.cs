using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the immutable authored node identities of a Sankey, Tree, Sunburst or Treemap series, in input order.</summary>
    public IReadOnlyList<ChartNode> Nodes => Relationships?.Nodes ?? Array.Empty<ChartNode>();

    /// <summary>Gets the immutable authored flows of a Sankey series, in input order.</summary>
    public IReadOnlyList<ChartFlowLink> FlowLinks => Relationships?.FlowLinks ?? Array.Empty<ChartFlowLink>();

    /// <summary>Gets the immutable authored branches of a Tree or Sunburst series, in input order.</summary>
    public IReadOnlyList<ChartTreeLink> TreeLinks => Relationships?.TreeLinks ?? Array.Empty<ChartTreeLink>();

    /// <summary>Gets the immutable authored groups and leaves of a Treemap series, in input order.</summary>
    public IReadOnlyList<ChartTreemapItem> TreemapItems => Relationships?.TreemapItems ?? Array.Empty<ChartTreemapItem>();

    internal ChartRelationshipIndex? Relationships { get; private set; }
    internal bool HasSourceData => Points.Count > 0 || Nodes.Count > 0;
    internal static bool IsRelationshipKind(ChartSeriesKind kind) => kind == ChartSeriesKind.Sankey || kind == ChartSeriesKind.Tree || kind == ChartSeriesKind.Sunburst || kind == ChartSeriesKind.Treemap;
    internal void SetRelationships(ChartRelationshipIndex relationships) => Relationships = relationships;

    internal void ValidateRelationships(bool preparing) {
        if (Points.Count > 0) throw new InvalidOperationException("Relationship series use typed nodes, links or treemap items; raw relationship points are not supported.");
        if (!preparing && Relationships == null) throw new InvalidOperationException("Relationship charts require explicit nodes and links.");
    }
}
