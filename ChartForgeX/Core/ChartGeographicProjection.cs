using System;
using System.Collections.Generic;
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
        var bottomRight = Project(viewport.CrossesAntimeridian ? viewport.MinimumLongitude + viewport.LongitudeSpan : viewport.MaximumLongitude,
            viewport.MinimumLatitude, projection);
        return new ChartRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    /// <summary>Selects a point's equivalent bearing nearest the explicit increasing window before using the existing projection.</summary>
    internal static ChartPoint Project(double longitude, double latitude, ChartMapGeoJsonProjection projection, ChartMapViewport viewport) =>
        Project(WindowLongitude(longitude, viewport), latitude, projection);

    internal static double WindowLongitude(double longitude, ChartMapViewport viewport) {
        if (!viewport.CrossesAntimeridian) return longitude;
        return LongitudeNear(longitude, viewport.MinimumLongitude + viewport.LongitudeSpan / 2);
    }

    /// <summary>Selects one periodic longitude representative near an already resolved source center.</summary>
    internal static double LongitudeNear(double longitude, double center) => longitude + 360 * Math.Floor((center - longitude + 180) / 360);

    internal static bool SamePolygonViewport(ChartMapViewport a, ChartMapViewport b) => a.CrossesAntimeridian && b.CrossesAntimeridian
        && a.MinimumLongitude == b.MinimumLongitude && a.MaximumLongitude == b.MaximumLongitude
        && a.MinimumLatitude == b.MinimumLatitude && a.MaximumLatitude == b.MaximumLatitude && a.GeographicProjection == b.GeographicProjection;

    internal static bool Contains(ChartPoint point, ChartMapViewport viewport) {
        var longitude = WindowLongitude(point.X, viewport);
        return longitude >= viewport.MinimumLongitude && longitude <= viewport.MinimumLongitude + viewport.LongitudeSpan
            && point.Y >= viewport.MinimumLatitude && point.Y <= viewport.MaximumLatitude;
    }

    /// <summary>Unwraps consecutive route bearings without changing or filtering their captured raw facts.</summary>
    internal static ChartPoint[] ProjectRoute(IReadOnlyList<ChartPoint> source, ChartMapGeoJsonProjection projection, ChartMapViewport viewport) {
        var result = new ChartPoint[source.Count];
        if (source.Count == 0) return result;
        var longitudes = new double[source.Count]; longitudes[0] = source[0].X;
        var minimum = longitudes[0]; var maximum = minimum;
        for (var index = 1; index < source.Count; index++) {
            var delta = source[index].X - source[index - 1].X;
            if (viewport.CrossesAntimeridian) {
                if (delta > 180) delta -= 360;
                else if (delta < -180) delta += 360;
                longitudes[index] = longitudes[index - 1] + delta;
            } else longitudes[index] = source[index].X;
            minimum = Math.Min(minimum, longitudes[index]); maximum = Math.Max(maximum, longitudes[index]);
        }
        // Select one periodic copy for the complete continuous route, including exact half-turn ties.
        // Per-waypoint nearest-window wrapping would invent long opposite-meridian segments.
        var center = minimum + (maximum - minimum) / 2;
        var shift = viewport.CrossesAntimeridian ? WindowLongitude(center, viewport) - center : 0;
        for (var index = 0; index < source.Count; index++) result[index] = Project(longitudes[index] + shift, source[index].Y, projection);
        return result;
    }

    internal static void Validate(ChartMapGeoJsonProjection projection) {
        if (projection != ChartMapGeoJsonProjection.Equirectangular && projection != ChartMapGeoJsonProjection.WebMercator)
            throw new ArgumentOutOfRangeException(nameof(projection));
    }
}
