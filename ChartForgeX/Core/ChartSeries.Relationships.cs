using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the immutable authored nodes of a Sankey, Tree, or Sunburst series, in input order.</summary>
    public IReadOnlyList<ChartNode> Nodes => Relationships?.Nodes ?? Array.Empty<ChartNode>();

    /// <summary>Gets the immutable authored flows of a Sankey series, in input order.</summary>
    public IReadOnlyList<ChartFlowLink> FlowLinks => Relationships?.FlowLinks ?? Array.Empty<ChartFlowLink>();

    /// <summary>Gets the immutable authored branches of a Tree or Sunburst series, in input order.</summary>
    public IReadOnlyList<ChartTreeLink> TreeLinks => Relationships?.TreeLinks ?? Array.Empty<ChartTreeLink>();

    internal ChartRelationshipIndex? Relationships { get; private set; }
    internal bool HasSourceData => Points.Count > 0 || Nodes.Count > 0;
    internal static bool IsRelationshipKind(ChartSeriesKind kind) => kind == ChartSeriesKind.Sankey || kind == ChartSeriesKind.Tree || kind == ChartSeriesKind.Sunburst;
    internal void SetRelationships(ChartRelationshipIndex relationships) => Relationships = relationships;

    internal void ValidateRelationships(bool preparing) {
        if (Points.Count > 0) throw new InvalidOperationException("Sankey, Tree, and Sunburst series use typed nodes and links; raw relationship points are not supported.");
        if (!preparing && Relationships == null) throw new InvalidOperationException("Relationship charts require explicit nodes and links.");
    }
}
