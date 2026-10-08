using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    internal static ChartDescriptionFacts DescriptionFacts(Chart chart, bool group) {
        var series = chart.Series[0];
        if (series.Kind == ChartSeriesKind.DottedMap)
            return new ChartDescriptionFacts(group ? ChartDescriptionKind.DottedMapGroup : ChartDescriptionKind.DottedMap,
                chart.Title, new[] { series.Name }, series.Points.Count);
        int count; int total; string name;
        if (series.Kind == ChartSeriesKind.RegionMap) {
            var definition = chart.Options.RegionMapDefinition ?? throw new InvalidOperationException("A region map requires a geometry definition.");
            count = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null).Count;
            total = definition.Regions.Count; name = definition.Name;
        } else {
            var definition = chart.Options.TileMapDefinition ?? throw new InvalidOperationException("A tile map requires a tile definition.");
            count = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null).Count;
            total = definition.Regions.Count; name = definition.Name;
        }
        var kind = series.Kind == ChartSeriesKind.RegionMap
            ? group ? ChartDescriptionKind.RegionMapGroup : ChartDescriptionKind.RegionMap
            : group ? ChartDescriptionKind.TileMapGroup : ChartDescriptionKind.TileMap;
        return new ChartDescriptionFacts(kind, chart.Title, new[] { series.Name }, count, total - count, mapName: name);
    }
}
