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
    public Chart WithMapColorScale(ChartMapColorScale? scale) {
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
        WithMapColorScale(ChartMapColorScale.Sequential(lowColor, highColor));

    /// <summary>
    /// Sets a three-color diverging scale used by region and tile maps.
    /// </summary>
    /// <param name="lowColor">The low-value color.</param>
    /// <param name="midpointColor">The midpoint color.</param>
    /// <param name="highColor">The high-value color.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapColorScale(ChartColor lowColor, ChartColor midpointColor, ChartColor highColor, double? midpointValue = null) =>
        WithMapColorScale(ChartMapColorScale.Diverging(lowColor, midpointColor, highColor, midpointValue));

    /// <summary>
    /// Adds a geographic region map from a reusable map definition.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The map geometry definition.</param>
    /// <param name="regions">The region values to render. Duplicate region codes, names, or aliases are summed.</param>
    /// <param name="color">An optional high-intensity region color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRegionMap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        Options.RegionMapDefinition = definition;
        return AddRegionMap(name, definition, regions, ChartSeriesKind.RegionMap, "Region maps", color);
    }

    /// <summary>
    /// Adds a geographic region heatmap from a reusable map definition.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="definition">The map geometry definition.</param>
    /// <param name="regions">The region values to render. Each map region is colored independently from its own value.</param>
    /// <param name="scale">An optional map color scale used to color the regions.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRegionHeatmap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartMapColorScale? scale = null) {
        if (scale != null) Options.MapColorScale = scale;
        return AddRegionMap(name, definition, regions);
    }

    private Chart AddRegionMap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartSeriesKind kind, string chartName, ChartColor? color) {
        if (regions == null) throw new ArgumentNullException(nameof(regions));
        var byRegion = new SortedDictionary<string, RegionAggregate>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in regions) {
            if (!definition.TryResolveRegion(region.Region, out var code)) throw new ArgumentException("Unknown map region code or name: " + region.Region + ".", nameof(regions));
            if (!byRegion.TryGetValue(code, out var aggregate)) aggregate = new RegionAggregate();
            aggregate.Value += region.Value;
            if (region.Color.HasValue) aggregate.Color = region.Color;
            byRegion[code] = aggregate;
        }

        if (byRegion.Count == 0) throw new ArgumentException(chartName + " must contain at least one region value.", nameof(regions));
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
        Add(name, kind, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }
}
