using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Owns the geographic transform shared by polygon import, map layers, markers and routes.</summary>
internal static class ChartGeographicProjection {
    internal static ChartPoint Project(double longitude, double latitude, ChartMapGeoJsonProjection projection) {
        ChartGuards.Finite(longitude, nameof(longitude));
        ChartGuards.Finite(latitude, nameof(latitude));
        Validate(projection);
        if (projection == ChartMapGeoJsonProjection.WebMercator) {
            var clipped = Math.Max(-85.05112878, Math.Min(85.05112878, latitude));
            var radians = clipped * Math.PI / 180;
            return new ChartPoint(longitude, -Math.Log(Math.Tan(Math.PI / 4 + radians / 2)) * 180 / Math.PI);
        }
        return new ChartPoint(longitude, -latitude);
    }

    internal static ChartRect Bounds(ChartMapViewport viewport, ChartMapGeoJsonProjection projection) {
        var topLeft = Project(viewport.MinimumLongitude, viewport.MaximumLatitude, projection);
        var bottomRight = Project(viewport.MaximumLongitude, viewport.MinimumLatitude, projection);
        return new ChartRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    internal static void Validate(ChartMapGeoJsonProjection projection) {
        if (projection != ChartMapGeoJsonProjection.Equirectangular && projection != ChartMapGeoJsonProjection.WebMercator)
            throw new ArgumentOutOfRangeException(nameof(projection));
    }
}
