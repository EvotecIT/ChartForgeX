using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets immutable authored relationship nodes, in input order.</summary>
    public IReadOnlyList<ChartNode> Nodes => Relationships?.Nodes ?? Array.Empty<ChartNode>();

    /// <summary>Gets the immutable authored flows of a Sankey or Chord series, in input order.</summary>
    public IReadOnlyList<ChartFlowLink> FlowLinks => Relationships?.FlowLinks ?? Array.Empty<ChartFlowLink>();

    /// <summary>Gets the immutable authored branches of a Tree series, in input order.</summary>
    public IReadOnlyList<ChartTreeLink> TreeLinks => Relationships?.TreeLinks ?? Array.Empty<ChartTreeLink>();

    /// <summary>Gets the immutable authored groups and leaves of a Sunburst or Treemap series, in input order.</summary>
    public IReadOnlyList<ChartHierarchyItem> HierarchyItems => Relationships?.HierarchyItems ?? Array.Empty<ChartHierarchyItem>();

    /// <summary>Replaces all Sankey or Chord nodes and flows after snapshotting and validating the complete graph.</summary>
    /// <remarks>Empty nodes and flows clear the data. Successful replacement clears observation, node and flow overrides;
    /// series identity and configuration remain. An invalid or interrupted input leaves the previous data and overrides intact.</remarks>
    public ChartSeries ReplaceFlowData(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links) {
        RequireObservationKind(ChartSeriesKind.Sankey, ChartSeriesKind.Chord);
        SetRelationships(Kind == ChartSeriesKind.Sankey ? ChartRelationshipIndex.Sankey(nodes, links) : ChartRelationshipIndex.Chord(nodes, links));
        return this;
    }

    /// <summary>Replaces all Tree nodes and branches after snapshotting and validating the complete hierarchy.</summary>
    /// <remarks>Empty nodes and branches clear the data. Successful replacement clears observation and node overrides;
    /// series identity and configuration remain. Nonempty trees retain their connected, single-root validation.</remarks>
    public ChartSeries ReplaceTreeData(IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links) {
        RequireObservationKind(ChartSeriesKind.Tree);
        SetRelationships(ChartRelationshipIndex.Hierarchy(nodes, links));
        return this;
    }

    /// <summary>Replaces all Sunburst or Treemap items after snapshotting and validating their complete parent relationships.</summary>
    /// <remarks>Empty items clear the data. Successful replacement clears observation and node overrides while retaining
    /// series identity and configuration. Sunburst parent-size policy is validated during chart preparation.</remarks>
    public ChartSeries ReplaceHierarchyItems(IEnumerable<ChartHierarchyItem> items) {
        RequireObservationKind(ChartSeriesKind.Sunburst, ChartSeriesKind.Treemap);
        SetRelationships(ChartRelationshipIndex.AreaHierarchy(items, allowForest: Kind == ChartSeriesKind.Treemap, requireNullGroups: Kind == ChartSeriesKind.Treemap));
        return this;
    }

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
    internal static bool IsRelationshipKind(ChartSeriesKind kind) => kind == ChartSeriesKind.Sankey || kind == ChartSeriesKind.Chord || kind == ChartSeriesKind.Tree || kind == ChartSeriesKind.Sunburst || kind == ChartSeriesKind.Treemap;
    private void SetRelationships(ChartRelationshipIndex relationships) {
        Relationships = relationships;
        ClearObservationMetadata();
        _nodeStates.Clear(); _flowStyles.Clear(); _flowStates.Clear();
        ObservationDataChanged(0);
    }

    internal void ValidateRelationships(bool preparing) {
        if (_points.Count > 0) throw new InvalidOperationException("Relationship series use typed nodes, links or hierarchy items; raw relationship points are not supported.");
        if (!preparing && Relationships == null) throw new InvalidOperationException("Relationship charts require explicit nodes and links or hierarchy items.");
    }
}
