using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Creates map definitions from GeoJSON polygon features.
/// </summary>
public static class ChartMapGeoJson {
    /// <summary>
    /// Creates a map definition from a GeoJSON FeatureCollection or Feature.
    /// </summary>
    /// <param name="id">The stable map identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="geoJson">The GeoJSON document.</param>
    /// <param name="options">Optional import settings.</param>
    /// <returns>A reusable map definition.</returns>
    public static ChartMapDefinition ToMapDefinition(string id, string name, string geoJson, ChartMapGeoJsonOptions? options = null) {
        if (string.IsNullOrWhiteSpace(geoJson)) throw new ArgumentException("GeoJSON must not be empty.", nameof(geoJson));
        options ??= new ChartMapGeoJsonOptions();
        options.Validate();
        var root = GeoJsonValue.Parse(geoJson).AsObject("GeoJSON root");
        var features = ReadFeatures(root);
        var regions = new List<ChartMapRegion>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bounds = EmptyBounds();
        var index = 1;
        foreach (var feature in features) {
            if (!options.IncludesFeature(feature)) continue;
            if (!TryReadPath(feature, options, out var path, out var label, out var featureBounds, out var coordinates)) {
                if (options.SkipUnsupportedGeometries) continue;
                throw new ArgumentException("Unsupported GeoJSON geometry type: " + (feature.GeometryType ?? "missing") + ".");
            }

            if (!options.IncludesCoordinateBounds(coordinates.MinLongitude, coordinates.MaxLongitude, coordinates.MinLatitude, coordinates.MaxLatitude)) continue;
            Include(ref bounds, featureBounds);
            var code = options.FindCode(feature, index, usedCodes);
            usedCodes.Add(code);
            var regionName = options.FindName(feature, code);
            regions.Add(new ChartMapRegion(code, regionName, path, label, options.FindAliases(feature, code, regionName)));
            index++;
        }

        if (regions.Count == 0) throw new ArgumentException("GeoJSON did not contain any polygon or multipolygon features matching the import options.", nameof(geoJson));
        return new ChartMapDefinition(id, name, MaterializeBounds(bounds), regions);
    }

    private static List<GeoJsonFeature> ReadFeatures(Dictionary<string, GeoJsonValue> root) {
        var type = root.OptionalString("type");
        var features = new List<GeoJsonFeature>();
        if (string.Equals(type, "FeatureCollection", StringComparison.OrdinalIgnoreCase)) {
            foreach (var item in root.RequiredArray("features")) features.Add(GeoJsonFeature.FromValue(item));
            return features;
        }

        if (string.Equals(type, "Feature", StringComparison.OrdinalIgnoreCase)) {
            features.Add(GeoJsonFeature.FromObject(root));
            return features;
        }

        throw new ArgumentException("GeoJSON root must be a FeatureCollection or Feature.");
    }

    private static bool TryReadPath(GeoJsonFeature feature, ChartMapGeoJsonOptions options, out string path, out ChartPoint label, out Bounds bounds, out CoordinateBounds coordinates) {
        path = string.Empty;
        label = default;
        bounds = EmptyBounds();
        coordinates = EmptyCoordinateBounds();
        if (feature.Geometry == null) return false;
        var geometry = feature.Geometry;
        var type = geometry.OptionalString("type");
        if (!string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase)) return false;
        var geometryCoordinates = geometry.RequiredArray("coordinates");
        var sb = new StringBuilder();
        var labelPoints = new List<ChartPoint>();

        if (string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase)) {
            AppendPolygon(sb, geometryCoordinates, options, labelPoints, ref bounds, ref coordinates);
        } else {
            foreach (var polygon in geometryCoordinates) AppendPolygon(sb, polygon.AsArray("MultiPolygon polygon"), options, labelPoints, ref bounds, ref coordinates);
        }

        path = sb.ToString();
        label = Average(labelPoints);
        return path.Length > 0;
    }

    private static void AppendPolygon(StringBuilder sb, List<GeoJsonValue> rings, ChartMapGeoJsonOptions options, List<ChartPoint> labelPoints, ref Bounds bounds, ref CoordinateBounds coordinates) {
        for (var ringIndex = 0; ringIndex < rings.Count; ringIndex++) {
            var positions = rings[ringIndex].AsArray("Polygon ring");
            var first = true;
            var outerRingPoints = ringIndex == 0 ? labelPoints : null;
            foreach (var positionValue in positions) {
                var position = positionValue.AsArray("GeoJSON position");
                if (position.Count < 2) throw new ArgumentException("GeoJSON positions must contain at least longitude and latitude.");
                var lon = position[0].AsNumber("longitude");
                var lat = position[1].AsNumber("latitude");
                var point = Project(lon, lat, options.Projection);
                Include(ref bounds, point);
                Include(ref coordinates, lon, lat);
                if (outerRingPoints != null) outerRingPoints.Add(point);
                sb.Append(first ? 'M' : 'L').Append(Format(point.X, options.CoordinatePrecision)).Append(' ').Append(Format(point.Y, options.CoordinatePrecision));
                first = false;
            }

            if (!first) sb.Append('Z');
        }
    }

    private static ChartPoint Project(double lon, double lat, ChartMapGeoJsonProjection projection) {
        ChartGuards.Finite(lon, nameof(lon));
        ChartGuards.Finite(lat, nameof(lat));
        if (projection == ChartMapGeoJsonProjection.WebMercator) {
            var clipped = Math.Max(-85.05112878, Math.Min(85.05112878, lat));
            var radians = clipped * Math.PI / 180.0;
            var y = Math.Log(Math.Tan(Math.PI / 4.0 + radians / 2.0)) * 180.0 / Math.PI;
            return new ChartPoint(lon, -y);
        }

        return new ChartPoint(lon, -lat);
    }

    private static ChartPoint Average(List<ChartPoint> points) {
        if (points.Count == 0) return new ChartPoint(0, 0);
        double x = 0;
        double y = 0;
        foreach (var point in points) {
            x += point.X;
            y += point.Y;
        }

        return new ChartPoint(x / points.Count, y / points.Count);
    }

    private static Bounds EmptyBounds() => new(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);

    private static CoordinateBounds EmptyCoordinateBounds() => new(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);

    private static void Include(ref Bounds bounds, ChartPoint point) {
        if (point.X < bounds.MinX) bounds.MinX = point.X;
        if (point.X > bounds.MaxX) bounds.MaxX = point.X;
        if (point.Y < bounds.MinY) bounds.MinY = point.Y;
        if (point.Y > bounds.MaxY) bounds.MaxY = point.Y;
    }

    private static void Include(ref Bounds bounds, Bounds featureBounds) {
        if (featureBounds.MinX < bounds.MinX) bounds.MinX = featureBounds.MinX;
        if (featureBounds.MaxX > bounds.MaxX) bounds.MaxX = featureBounds.MaxX;
        if (featureBounds.MinY < bounds.MinY) bounds.MinY = featureBounds.MinY;
        if (featureBounds.MaxY > bounds.MaxY) bounds.MaxY = featureBounds.MaxY;
    }

    private static void Include(ref CoordinateBounds bounds, double longitude, double latitude) {
        if (longitude < bounds.MinLongitude) bounds.MinLongitude = longitude;
        if (longitude > bounds.MaxLongitude) bounds.MaxLongitude = longitude;
        if (latitude < bounds.MinLatitude) bounds.MinLatitude = latitude;
        if (latitude > bounds.MaxLatitude) bounds.MaxLatitude = latitude;
    }

    private static ChartRect MaterializeBounds(Bounds bounds) {
        if (double.IsInfinity(bounds.MinX) || double.IsInfinity(bounds.MinY) || double.IsInfinity(bounds.MaxX) || double.IsInfinity(bounds.MaxY)) throw new ArgumentException("GeoJSON bounds could not be determined.");
        return new ChartRect(bounds.MinX, bounds.MinY, Math.Max(0.000001, bounds.MaxX - bounds.MinX), Math.Max(0.000001, bounds.MaxY - bounds.MinY));
    }

    private static string Format(double value, int precision) => value.ToString("0." + new string('#', precision), CultureInfo.InvariantCulture);

    private struct Bounds {
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;

        public Bounds(double minX, double minY, double maxX, double maxY) {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    private struct CoordinateBounds {
        public double MinLongitude;
        public double MinLatitude;
        public double MaxLongitude;
        public double MaxLatitude;

        public CoordinateBounds(double minLongitude, double minLatitude, double maxLongitude, double maxLatitude) {
            MinLongitude = minLongitude;
            MinLatitude = minLatitude;
            MaxLongitude = maxLongitude;
            MaxLatitude = maxLatitude;
        }
    }
}
