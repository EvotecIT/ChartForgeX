using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Sets the color scale used by region and tile maps.
    /// </summary>
    /// <param name="scale">The map color scale, or null to use the default map coloring.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapColorScale(ChartColorScale? scale) {
        Options.MapColorScale = scale;
        return this;
    }

    /// <summary>
    /// Sets a two-color sequential scale used by region and tile maps.
    /// </summary>
    /// <param name="lowColor">The low-value color.</param>
    /// <param name="highColor">The high-value color.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapColorScale(ChartColor lowColor, ChartColor highColor) =>
        WithMapColorScale(ChartColorScale.Sequential(lowColor, highColor));

    /// <summary>
    /// Sets a three-color diverging scale used by region and tile maps.
    /// </summary>
    /// <param name="lowColor">The low-value color.</param>
    /// <param name="midpointColor">The midpoint color.</param>
    /// <param name="highColor">The high-value color.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapColorScale(ChartColor lowColor, ChartColor midpointColor, ChartColor highColor, double? midpointValue = null) =>
        WithMapColorScale(ChartColorScale.Diverging(lowColor, midpointColor, highColor, midpointValue));

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
        if (scale != null) Options.MapColorScale = scale;
        return this;
    }
}
