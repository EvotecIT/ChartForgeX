using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates an independently configurable dotted map with complete longitude/latitude observations and an explicit stable ID.</summary>
    public static ChartSeries CreateDottedMap(ChartSeriesId id, string name, IEnumerable<ChartMapPoint> points, ChartColor? color = null) =>
        DottedMap(new ChartSeries(id, name, ChartSeriesKind.DottedMap, Array.Empty<ChartPoint>()) { Color = color }, points);
    internal static ChartSeries CreateDottedMap(string name, IEnumerable<ChartMapPoint> points, ChartColor? color) =>
        DottedMap(new ChartSeries(name, ChartSeriesKind.DottedMap, Array.Empty<ChartPoint>()) { Color = color }, points);
    private static ChartSeries DottedMap(ChartSeries series, IEnumerable<ChartMapPoint> points) {
        series.ReplaceMapPoints(points);
        return series;
    }

    /// <summary>Creates an independently configurable calendar heatmap, aggregating duplicate dates and sorting by date.</summary>
    public static ChartSeries CreateCalendarHeatmap(ChartSeriesId id, string name, IEnumerable<ChartCalendarHeatmapItem> items, ChartColor? color = null) =>
        CalendarHeatmap(new ChartSeries(id, name, ChartSeriesKind.CalendarHeatmap, Array.Empty<ChartPoint>()) { Color = color }, items);
    internal static ChartSeries CreateCalendarHeatmap(string name, IEnumerable<ChartCalendarHeatmapItem> items, ChartColor? color) =>
        CalendarHeatmap(new ChartSeries(name, ChartSeriesKind.CalendarHeatmap, Array.Empty<ChartPoint>()) { Color = color }, items);
    private static ChartSeries CalendarHeatmap(ChartSeries series, IEnumerable<ChartCalendarHeatmapItem> items) {
        series.ReplaceCalendarItems(items);
        return series;
    }

    /// <summary>Creates a region map owning immutable geometry and canonical values. Duplicate codes, names and aliases are summed.</summary>
    public static ChartSeries CreateRegionMap(ChartSeriesId id, string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) =>
        RegionMap(new ChartSeries(id, name, ChartSeriesKind.RegionMap, Array.Empty<ChartPoint>()) { Color = color }, definition, regions);
    internal static ChartSeries CreateRegionMap(string name, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color) =>
        RegionMap(new ChartSeries(name, ChartSeriesKind.RegionMap, Array.Empty<ChartPoint>()) { Color = color }, definition, regions);
    private static ChartSeries RegionMap(ChartSeries series, ChartMapDefinition definition, IEnumerable<ChartRegionMapItem> regions) {
        series.RegionMapDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        series.ReplaceRegionValues(regions);
        return series;
    }

    /// <summary>Creates a tile map owning immutable geometry and canonical values. Duplicate codes, names and aliases are summed.</summary>
    public static ChartSeries CreateTileMap(ChartSeriesId id, string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) =>
        TileMap(new ChartSeries(id, name, ChartSeriesKind.TileMap, Array.Empty<ChartPoint>()) { Color = color }, definition, regions);
    internal static ChartSeries CreateTileMap(string name, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions, ChartColor? color) =>
        TileMap(new ChartSeries(name, ChartSeriesKind.TileMap, Array.Empty<ChartPoint>()) { Color = color }, definition, regions);
    private static ChartSeries TileMap(ChartSeries series, ChartTileMapDefinition definition, IEnumerable<ChartRegionMapItem> regions) {
        series.TileMapDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        series.ReplaceRegionValues(regions);
        return series;
    }
}
