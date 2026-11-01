using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private Func<ChartSeries, ChartSeriesInfo, int, ChartItemStyle?>? _relationshipItemStyler;
    private IReadOnlyList<ChartItemStyle?>? _preparedRelationshipStyles;

    // Node and link callbacks coexist; their source ordinals and typed data are independent.
    private bool TrySetRelationshipItemStyler<TDatum>(Func<ChartItemStyleContext<TDatum>, ChartItemStyle?>? styler) {
        if (!(Kind is ChartSeriesKind.Sankey or ChartSeriesKind.Chord && typeof(TDatum) == typeof(ChartFlowLink)
            || Kind == ChartSeriesKind.Tree && typeof(TDatum) == typeof(ChartTreeLink))) return false;
        _relationshipItemStyler = styler == null ? null : (series, info, index) => styler(new ChartItemStyleContext<TDatum>(
            (TDatum)series.RelationshipDatum(index), info, index, series.MarkOptions, series.RelationshipSemanticState(index)));
        return true;
    }

    internal int RelationshipObservationCount => Kind == ChartSeriesKind.Tree ? TreeLinks.Count : FlowLinks.Count;
    internal object RelationshipDatum(int index) => Kind == ChartSeriesKind.Tree ? (object)TreeLinks[index] : FlowLinks[index];

    private ChartSeriesState RelationshipSemanticState(int index) {
        if (Kind != ChartSeriesKind.Tree && FlowStates.TryGetValue(FlowLinks[index].Id, out var state)) return state;
        var sourceId = Kind == ChartSeriesKind.Tree ? TreeLinks[index].ParentId : FlowLinks[index].SourceId;
        return Relationships!.TryNodeIndex(sourceId, out var source) ? ChartRelationshipPaint.State(this, source) : StateRole;
    }

    private void ResolveRelationshipItemStyles(ChartSeries source, ChartSeriesInfo info) {
        _relationshipItemStyler = null;
        if (source._relationshipItemStyler == null) return;
        var styles = new ChartItemStyle?[source.RelationshipObservationCount];
        for (var index = 0; index < styles.Length; index++) {
            var style = source._relationshipItemStyler(source, info, index);
            if (style?.MarkerSnapshot != null) throw new InvalidOperationException("Relationship links do not have point markers.");
            styles[index] = style;
        }
        _preparedRelationshipStyles = Array.AsReadOnly(styles);
    }

    internal ChartItemStyle? PreparedRelationshipStyle(int index) => index >= 0 && _preparedRelationshipStyles != null && index < _preparedRelationshipStyles.Count
        ? _preparedRelationshipStyles[index] : null;
}
