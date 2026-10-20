using System;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>Validates the shared coordinate contract before geographic dataset composition.</summary>
internal static class ChartMapComposition {
    internal static bool IsGeographic(ChartSeriesKind kind) => kind is ChartSeriesKind.RegionMap or ChartSeriesKind.DottedMap;

    internal static ChartMapGeoJsonProjection Projection(Chart chart) {
        ChartMapGeoJsonProjection? projection = null;
        foreach (var series in chart.Series.Where(series => series.Kind == ChartSeriesKind.RegionMap)) {
            var declared = series.RegionMapDefinition?.GeographicProjection;
            if (!declared.HasValue) {
                if (chart.Series.Count == 1) continue;
                throw new InvalidOperationException("Composed geographic map datasets require an explicit projection on every region definition; arbitrary-coordinate maps remain standalone.");
            }
            if (projection.HasValue && projection.Value != declared.Value)
                throw new InvalidOperationException("Composed geographic map datasets must declare the same projection.");
            projection = declared;
        }
        var resolved = projection ?? ChartMapGeoJsonProjection.Equirectangular;
        foreach (var layer in chart.Options.MapBaseLayers.Concat(chart.Options.MapOverlayLayers))
            if (layer.Definition.GeographicProjection.HasValue && layer.Definition.GeographicProjection.Value != resolved)
                throw new InvalidOperationException("Geographic context layers must use the same projection as their data layers.");
        return resolved;
    }

    internal static bool ValidateMultiple(Chart chart) {
        if (chart.Series.Count < 2 || !chart.Series.All(series => IsGeographic(series.Kind))) return false;
        _ = Projection(chart);
        return true;
    }
}
