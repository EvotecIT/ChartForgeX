using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private readonly List<ChartColor?> _pointStrokeColors = new();
    private IReadOnlyList<ChartColor?>? _pointStrokeColorsView;

    /// <summary>Gets ordinal border colors for Treemap/Sunburst nodes and Funnel/Pyramid stages. Null entries retain each family's normal state/theme border.</summary>
    /// <remarks>Hierarchy ordinals refer to HierarchyItems; Funnel/Pyramid ordinals refer to numeric observations. The view is read-only; use the fluent methods to change a border.</remarks>
    public IReadOnlyList<ChartColor?> PointStrokeColors => _pointStrokeColorsView ??= _pointStrokeColors.AsReadOnly();

    /// <summary>Sets one hierarchy-node or Funnel/Pyramid stage border; null restores the normal state/theme border. Hierarchy StrokeWidth or stage/partition outline options control authored borders.</summary>
    /// <param name="pointIndex">The zero-based existing source ordinal.</param>
    /// <param name="color">The native color, or null to restore the family border.</param>
    /// <returns>The current series.</returns>
    public ChartSeries PointStrokeColor(int pointIndex, ChartColor? color) {
        ValidatePointIndex(pointIndex);
        RequirePointStrokeColors();
        while (_pointStrokeColors.Count <= pointIndex) _pointStrokeColors.Add(null);
        _pointStrokeColors[pointIndex] = color;
        return this;
    }

    private void RequirePointStrokeColors() {
        if (Kind is not (ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst or ChartSeriesKind.Funnel or ChartSeriesKind.Pyramid))
            throw new NotSupportedException("Point stroke colors support Treemap/Sunburst hierarchy items and Funnel/Pyramid stages.");
    }
}
