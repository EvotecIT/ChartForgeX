using System;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private void RestoreVisibilityHierarchy(ChartVisibilitySource.Series source, double[]? values) {
        var reading = 0;
        var items = source.HierarchyItems.Select(item => new ChartHierarchyItem(item.Id, item.Label, item.ParentId,
            values != null && item.Value.HasValue ? values[reading++] : item.Value, item.ColorValue)).ToArray();
        if (values != null && reading != values.Length) throw new ArgumentException("Hierarchy readings must match the ordered authored size fields.", nameof(values));
        SetRelationships(ChartRelationshipIndex.AreaHierarchy(items, allowForest: Kind == ChartSeriesKind.Treemap, requireNullGroups: Kind == ChartSeriesKind.Treemap));
        foreach (var pair in source.NodeStates) _nodeStates.Add(pair.Key, pair.Value);
        _pointStrokeColors.AddRange(source.PointStrokes);
    }
}
