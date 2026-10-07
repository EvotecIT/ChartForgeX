using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Produces render-neutral facts for the automatic chart text alternative.</summary>
internal static class ChartAccessibleDescription {
    internal static ChartDescriptionFacts Facts(Chart chart) {
        if (chart.Series.Count == 0)
            return new ChartDescriptionFacts(ChartDescriptionKind.NoSeries, chart.Title, Array.Empty<string>(), 0);
        var calendar = ChartCalendarHeatmapModel.Build(chart);
        if (calendar != null) return calendar.DescriptionFacts();
        if (ChartSeriesKindTraits.IsSpatialMapKind(chart.Series[0].Kind))
            return VisualMapCompiler.DescriptionFacts(chart, false);
        var names = chart.Series.Where(series => series.Points.Count > 0 && series.SemanticRole != "point-callout")
            .Select(series => series.Name).ToArray();
        return new ChartDescriptionFacts(names.Length == 0 ? ChartDescriptionKind.NoPoints : ChartDescriptionKind.Series,
            chart.Title, names, names.Length);
    }
}
