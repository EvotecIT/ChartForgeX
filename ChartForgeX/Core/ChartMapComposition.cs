using System;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>Validates the shared coordinate contract and explicit independent tile placements.</summary>
internal static class ChartMapComposition {
    internal static bool IsGeographic(ChartSeriesKind kind) => kind is ChartSeriesKind.RegionMap or ChartSeriesKind.DottedMap;
    internal static bool IsMap(ChartSeriesKind kind) => IsGeographic(kind) || kind == ChartSeriesKind.TileMap;

    // Null denotes a common arbitrary numeric frame; geographic points never enter that frame implicitly.
    internal static ChartMapGeoJsonProjection? Projection(Chart chart) {
        ValidateDefinitions(chart);
        ValidatePolygonViewports(chart);
        var data = chart.Series.Where(series => series.Kind != ChartSeriesKind.TileMap).ToArray();
        if (data.Length == 0) throw new InvalidOperationException("TileMap insets require a RegionMap or DottedMap peer.");
        foreach (var series in chart.Series.Where(series => series.Kind == ChartSeriesKind.TileMap))
            if (!series.MapInset.HasValue) throw new InvalidOperationException("Every composed TileMap requires an explicit normalized map inset.");
        var arbitrary = data.Any(series => series.Kind == ChartSeriesKind.RegionMap && !series.RegionMapDefinition!.GeographicProjection.HasValue);
        if (arbitrary) {
            if (data.Any(series => series.Kind != ChartSeriesKind.RegionMap || series.RegionMapDefinition!.GeographicProjection.HasValue)
                || chart.Options.HasExplicitMapViewport || chart.Options.MapConnectors.Count > 0 || data.Any(series => series.MapConnectors.Count > 0)
                || chart.Options.MapBaseLayers.Concat(chart.Options.MapOverlayLayers).Any(layer => layer.Definition.GeographicProjection.HasValue))
                throw new InvalidOperationException("Arbitrary-coordinate region datasets cannot share a frame with geographic points, routes, viewports or layers; declare the same projection on every geographic region definition.");
            return null;
        }
        ChartMapGeoJsonProjection? projection = null;
        foreach (var series in data.Where(series => series.Kind == ChartSeriesKind.RegionMap)) {
            var declared = series.RegionMapDefinition!.GeographicProjection!.Value;
            if (projection.HasValue && projection.Value != declared)
                throw new InvalidOperationException("Composed geographic map datasets must declare the same projection.");
            projection = declared;
        }
        var resolved = projection ?? chart.Options.MapViewport.GeographicProjection ?? ChartMapGeoJsonProjection.Equirectangular;
        foreach (var layer in chart.Options.MapBaseLayers.Concat(chart.Options.MapOverlayLayers))
            if (layer.Definition.GeographicProjection.HasValue && layer.Definition.GeographicProjection.Value != resolved)
                throw new InvalidOperationException("Geographic context layers must use the same projection as their data layers.");
        return resolved;
    }

    private static void ValidatePolygonViewports(Chart chart) {
        var viewport = chart.Options.MapViewport;
        if (viewport.CrossesAntimeridian && chart.Options.RegionMapBounds.HasValue)
            throw new NotSupportedException("An antimeridian viewport cannot use projected RegionMapBounds.");
        var definitions = chart.Series.Where(series => series.Kind == ChartSeriesKind.RegionMap).Select(series => series.RegionMapDefinition!)
            .Concat(chart.Options.MapBaseLayers.Concat(chart.Options.MapOverlayLayers).Select(layer => layer.Definition));
        foreach (var definition in definitions) {
            if (!viewport.CrossesAntimeridian && !definition.PolygonViewport.HasValue) continue;
            if (!definition.PolygonViewport.HasValue || !ChartGeographicProjection.SamePolygonViewport(definition.PolygonViewport.Value, viewport)
                || definition.GeographicProjection != viewport.GeographicProjection)
                throw new NotSupportedException("An antimeridian region/context definition requires the same explicit PolygonViewport and projection as the chart.");
        }
    }

    private static void ValidateDefinitions(Chart chart) {
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.RegionMap && series.RegionMapDefinition == null)
                throw new InvalidOperationException("A region map requires a geometry definition.");
            if (series.Kind == ChartSeriesKind.TileMap && series.TileMapDefinition == null)
                throw new InvalidOperationException("A tile map requires a tile definition.");
        }
    }

    internal static bool ValidateMultiple(Chart chart) {
        ValidateDefinitions(chart);
        if (chart.Series.Count < 2) {
            if (chart.Series.Count == 1 && chart.Series[0].MapInset.HasValue)
                throw new InvalidOperationException("A TileMap inset requires another map dataset.");
            return false;
        }
        if (!chart.Series.All(series => IsMap(series.Kind))) return false;
        _ = Projection(chart); return true;
    }
}
