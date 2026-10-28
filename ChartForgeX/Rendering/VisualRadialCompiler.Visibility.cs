using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    /// <summary>Copies the full native Other membership from the same slice/aggregation owner used by paint and legend.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<int>> CaptureOtherMembership(Chart chart, VisualThemeColors colors) {
        var result = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        for (var index = 0; index < chart.Series.Count; index++) {
            var other = GetSlices(chart, colors, index).SingleOrDefault(slice => slice.PointIndex < 0);
            if (other != null) result.Add(chart.Series[index].Id.Value, Array.AsReadOnly((int[])other.SourcePointIndices.Clone()));
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<int>>(result);
    }
}
