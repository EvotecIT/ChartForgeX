using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets immutable authored relationship nodes, in input order.</summary>
    public IReadOnlyList<ChartNode> Nodes => Relationships?.Nodes ?? Array.Empty<ChartNode>();

    /// <summary>Gets the immutable authored flows of a Sankey or Chord series, in input order.</summary>
    public IReadOnlyList<ChartFlowLink> FlowLinks => Relationships?.FlowLinks ?? Array.Empty<ChartFlowLink>();

    /// <summary>Gets the immutable authored branches of a Tree or Sunburst series, in input order.</summary>
    public IReadOnlyList<ChartTreeLink> TreeLinks => Relationships?.TreeLinks ?? Array.Empty<ChartTreeLink>();

    /// <summary>Gets the immutable authored groups and leaves of a Treemap series, in input order.</summary>
    public IReadOnlyList<ChartTreemapItem> TreemapItems => Relationships?.TreemapItems ?? Array.Empty<ChartTreemapItem>();

    private readonly Dictionary<string, ChartSeriesState> _nodeStates = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, ChartSeriesState>? _nodeStatesView;

    /// <summary>Gets semantic states keyed by authored node ID, scoped to this relationship series.</summary>
    public IReadOnlyDictionary<string, ChartSeriesState> NodeStates => _nodeStatesView ??= new ReadOnlyDictionary<string, ChartSeriesState>(_nodeStates);

    /// <summary>Assigns a semantic state to an existing authored node without relying on display labels or input ordinals.</summary>
    public ChartSeries WithNodeState(string id, ChartSeriesState state) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Node ID must not be empty.", nameof(id));
        if (!Enum.IsDefined(typeof(ChartSeriesState), state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (Relationships?.ContainsNode(id) != true) throw new ArgumentException("The series has no authored node with this ID.", nameof(id));
        _nodeStates[id] = state;
        return this;
    }

    internal ChartRelationshipIndex? Relationships { get; private set; }
    internal bool HasSourceData => Points.Count > 0 || Nodes.Count > 0;
    internal static bool IsRelationshipKind(ChartSeriesKind kind) => kind == ChartSeriesKind.Sankey || kind == ChartSeriesKind.Chord || kind == ChartSeriesKind.Tree || kind == ChartSeriesKind.Sunburst || kind == ChartSeriesKind.Treemap;
    internal void SetRelationships(ChartRelationshipIndex relationships) => Relationships = relationships;

    internal void ValidateRelationships(bool preparing) {
        if (Points.Count > 0) throw new InvalidOperationException("Relationship series use typed nodes, links or treemap items; raw relationship points are not supported.");
        if (!preparing && Relationships == null) throw new InvalidOperationException("Relationship charts require explicit nodes and links.");
    }
}
