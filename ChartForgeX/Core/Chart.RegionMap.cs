using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>Adds a region map whose series owns the immutable map geometry and canonical values.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The immutable map geometry definition.</param>
    /// <param name="regions">Values to normalize. Duplicate codes, names or aliases are summed.</param>
    /// <param name="color">An optional high-intensity region color.</param>
    /// <param name="id">Optional stable identity assigned before attachment.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRegionMap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null, ChartSeriesId? id = null) {
        var series = id.HasValue ? ChartSeries.CreateRegionMap(id.Value, name, definition, regions, color) : ChartSeries.CreateRegionMap(name, definition, regions, color);
        return AddSeries(series);
    }

    /// <summary>Adds a region map with an optional chart-owned numeric color scale.</summary>
    public Chart AddRegionHeatmap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColorScale? scale = null, ChartSeriesId? id = null) {
        AddRegionMap(name, definition, regions, id: id);
        if (scale != null) Options.Map.ColorScale = scale;
        return this;
    }
}
