using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a tile map from a reusable tile-map definition.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The tile-map geometry definition.</param>
    /// <param name="regions">The region values to render. Duplicate region codes, names, or aliases are summed.</param>
    /// <param name="color">An optional high-intensity region color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTileMap(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        Options.TileMapDefinition = definition;
        return AddTileMapCore(name, definition, regions, color);
    }

    /// <summary>
    /// Adds a tile-map heatmap from a reusable tile-map definition.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The tile-map geometry definition.</param>
    /// <param name="regions">The region values to render. Each tile is colored independently from its own value.</param>
    /// <param name="scale">An optional map color scale used to color the tiles.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTileHeatmap(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartMapColorScale? scale = null) {
        if (scale != null) Options.MapColorScale = scale;
        return AddTileMap(name, definition, regions);
    }

    private Chart AddTileMapCore(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color) {
        if (regions == null) throw new ArgumentNullException(nameof(regions));
        var byRegion = new SortedDictionary<string, RegionAggregate>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in regions) {
            if (!definition.TryResolveRegion(region.Region, out var code)) throw new ArgumentException("Unknown tile-map region code or name: " + region.Region + ".", nameof(regions));
            if (!byRegion.TryGetValue(code, out var aggregate)) aggregate = new RegionAggregate();
            aggregate.Value += region.Value;
            if (region.Color.HasValue) aggregate.Color = region.Color;
            byRegion[code] = aggregate;
        }

        if (byRegion.Count == 0) throw new ArgumentException("Tile maps must contain at least one region value.", nameof(regions));
        var points = new List<ChartPoint>(byRegion.Count);
        var labels = new List<ChartAxisLabel>(byRegion.Count);
        var colors = new List<ChartColor?>(byRegion.Count);
        var index = 1;
        foreach (var entry in byRegion) {
            points.Add(new ChartPoint(index, entry.Value.Value));
            labels.Add(new ChartAxisLabel(index, entry.Key));
            colors.Add(entry.Value.Color);
            index++;
        }

        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.TileMap, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }

    private struct RegionAggregate {
        public double Value;
        public ChartColor? Color;
    }

}
