using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a tile map whose series owns the immutable tile geometry and canonical values.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The immutable tile-map geometry definition.</param>
    /// <param name="regions">Values to normalize. Duplicate codes, names or aliases are summed.</param>
    /// <param name="color">An optional high-intensity tile color.</param>
    /// <param name="id">Optional stable identity assigned before attachment.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTileMap(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null, ChartSeriesId? id = null) {
        var series = id.HasValue ? ChartSeries.CreateTileMap(id.Value, name, definition, regions, color) : ChartSeries.CreateTileMap(name, definition, regions, color);
        return AddSeries(series);
    }

    /// <summary>Adds a tile map with an optional chart-owned numeric color scale.</summary>
    public Chart AddTileHeatmap(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColorScale? scale = null, ChartSeriesId? id = null) {
        AddTileMap(name, definition, regions, id: id);
        if (scale != null) Options.MapColorScale = scale;
        return this;
    }
}
