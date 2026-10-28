using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Native source identity, independent of the filtered model's rendered ordinal.
    private static IReadOnlyDictionary<string, string> VisibilityLegendObservationFacts(ChartSeries series, int point) {
        var source = point < series.SourcePointIndices.Count ? series.SourcePointIndices[point] : point;
        var facts = new Dictionary<string, string> { ["data-cfx-source-point"] = Number(source) };
        if (series.ObservationIds.TryGetValue(source, out var id)) facts["data-cfx-observation-id"] = id.Value;
        return facts;
    }
}
