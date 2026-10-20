using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    internal static ChartDescriptionFacts DescriptionFacts(Chart chart, bool group, int seriesIndex = 0) {
        if (!group && chart.Series.Count > 1) return new ChartDescriptionFacts(ChartDescriptionKind.Series, chart.Title,
            System.Linq.Enumerable.Select(chart.Series, series => series.Name), chart.Series.Count);
        var series = chart.Series[seriesIndex];
        if (series.Kind == ChartSeriesKind.DottedMap)
            return new ChartDescriptionFacts(group ? ChartDescriptionKind.DottedMapGroup : ChartDescriptionKind.DottedMap,
                chart.Title, new[] { series.Name }, series.RenderPoints.Count);
        int count; int total; string name;
        if (series.Kind == ChartSeriesKind.RegionMap) {
            var definition = series.RegionMapDefinition ?? throw new InvalidOperationException("A region map requires a geometry definition.");
            count = Values(series, key => definition.TryResolveRegion(key, out var code) ? code : null).Count;
            total = definition.Regions.Count; name = definition.Name;
        } else {
            var definition = chart.Series[0].TileMapDefinition ?? throw new InvalidOperationException("A tile map requires a tile definition.");
            count = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null).Count;
            total = definition.Regions.Count; name = definition.Name;
        }
        var kind = series.Kind == ChartSeriesKind.RegionMap
            ? group ? ChartDescriptionKind.RegionMapGroup : ChartDescriptionKind.RegionMap
            : group ? ChartDescriptionKind.TileMapGroup : ChartDescriptionKind.TileMap;
        return new ChartDescriptionFacts(kind, chart.Title, new[] { series.Name }, count, total - count, mapName: name);
    }
}
