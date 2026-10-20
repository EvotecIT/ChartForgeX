using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Core;

public static partial class ChartMapGeoJson {
    /// <summary>Imports Point and MultiPoint features as geographic observations in feature and position order.</summary>
    /// <param name="geoJson">A GeoJSON FeatureCollection or Feature.</param>
    /// <param name="options">Optional feature, name, bounds and point-weight settings. Projection and coordinate precision do not change raw positions.</param>
    /// <returns>A detached read-only collection suitable for a dotted-map series, including an empty collection when no feature matches.</returns>
    /// <remarks>Feature names fall back to their code or ID. MultiPoint positions receive one-based part suffixes such as "Ports [1]".
    /// Feature-level bounds filtering retains complete geometries without clipping. Source properties and feature IDs are not observation metadata;
    /// assign stable observation IDs through the retained series when needed.</remarks>
    public static IReadOnlyList<ChartMapPoint> ToMapPoints(string geoJson, ChartMapGeoJsonOptions? options = null) {
        var features = ObservationFeatures(geoJson, ref options);
        var points = new List<ChartMapPoint>();
        var index = 1;
        foreach (var feature in features) {
            if (!options!.IncludesFeature(feature)) continue;
            var multiple = IsGeometry(feature, "MultiPoint");
            if (!multiple && !IsGeometry(feature, "Point")) { UnsupportedObservationGeometry(feature, options); continue; }
            var coordinates = feature.Geometry!.RequiredArray("coordinates");
            var positions = multiple ? coordinates : new List<GeoJsonValue> { GeoJsonValue.Array(coordinates) };
            var label = options.FindName(feature, options.FindCode(feature, index));
            var value = PointValue(feature, options);
            var colorValue = NumericFeatureValue(feature, options.ColorValuePropertyName);
            var featurePoints = new List<ChartMapPoint>();
            var bounds = EmptyCoordinateBounds();
            for (var part = 0; part < positions.Count; part++) {
                var point = GeographicPosition(positions[part], PartLabel(label, part, multiple), value).WithColorValue(colorValue);
                Include(ref bounds, point.Longitude, point.Latitude);
                featurePoints.Add(point);
            }
            if (featurePoints.Count > 0 && options.IncludesCoordinateBounds(bounds.MinLongitude, bounds.MaxLongitude, bounds.MinLatitude, bounds.MaxLatitude))
                points.AddRange(featurePoints);
            index++;
        }
        return points.AsReadOnly();
    }

    /// <summary>Imports LineString and MultiLineString features as ordered geographic route connectors.</summary>
    /// <param name="geoJson">A GeoJSON FeatureCollection or Feature.</param>
    /// <param name="options">Optional feature, name and bounds settings. Projection, point weights and precision do not alter route positions.</param>
    /// <returns>A detached read-only connector collection in feature and line order, including an empty collection when no feature matches.</returns>
    /// <remarks>Each line requires at least two valid positions. MultiLineString parts receive one-based label suffixes.
    /// Bounds filtering retains complete feature geometries and waypoint order without clipping or projecting them.
    /// Source properties and feature IDs are not retained as metadata by the connector primitive.</remarks>
    public static IReadOnlyList<ChartMapConnector> ToMapConnectors(string geoJson, ChartMapGeoJsonOptions? options = null) {
        var features = ObservationFeatures(geoJson, ref options);
        var connectors = new List<ChartMapConnector>();
        var index = 1;
        foreach (var feature in features) {
            if (!options!.IncludesFeature(feature)) continue;
            var multiple = IsGeometry(feature, "MultiLineString");
            if (!multiple && !IsGeometry(feature, "LineString")) { UnsupportedObservationGeometry(feature, options); continue; }
            var coordinates = feature.Geometry!.RequiredArray("coordinates");
            var lines = multiple ? coordinates : new List<GeoJsonValue> { GeoJsonValue.Array(coordinates) };
            var label = options.FindName(feature, options.FindCode(feature, index));
            var featureConnectors = new List<ChartMapConnector>();
            var bounds = EmptyCoordinateBounds();
            for (var part = 0; part < lines.Count; part++) {
                var positions = lines[part].AsArray("LineString positions");
                var lineLabel = PartLabel(label, part, multiple);
                var route = new List<ChartMapPoint>();
                for (var position = 0; position < positions.Count; position++) {
                    var point = GeographicPosition(positions[position], PartLabel(lineLabel, position, true), null);
                    Include(ref bounds, point.Longitude, point.Latitude);
                    route.Add(point);
                }
                featureConnectors.Add(new ChartMapConnector(lineLabel, route));
            }
            if (featureConnectors.Count > 0 && options.IncludesCoordinateBounds(bounds.MinLongitude, bounds.MaxLongitude, bounds.MinLatitude, bounds.MaxLatitude))
                connectors.AddRange(featureConnectors);
            index++;
        }
        return connectors.AsReadOnly();
    }

    private static List<GeoJsonFeature> ObservationFeatures(string geoJson, ref ChartMapGeoJsonOptions? options) {
        if (string.IsNullOrWhiteSpace(geoJson)) throw new ArgumentException("GeoJSON must not be empty.", nameof(geoJson));
        options ??= new ChartMapGeoJsonOptions();
        options.Validate();
        return ReadFeatures(GeoJsonValue.Parse(geoJson).AsObject("GeoJSON root"));
    }

    private static ChartMapPoint GeographicPosition(GeoJsonValue value, string label, double? weight) {
        var position = value.AsArray("GeoJSON position");
        if (position.Count < 2) throw new ArgumentException("GeoJSON positions must contain at least longitude and latitude.");
        var longitude = position[0].AsNumber("longitude");
        var latitude = position[1].AsNumber("latitude");
        return weight.HasValue ? new ChartMapPoint(label, longitude, latitude, weight.Value) : new ChartMapPoint(label, longitude, latitude);
    }

    private static double? PointValue(GeoJsonFeature feature, ChartMapGeoJsonOptions options) {
        var number = NumericFeatureValue(feature, options.ValuePropertyName);
        if (number.HasValue && number.Value < 0) throw new ArgumentOutOfRangeException(nameof(options.ValuePropertyName), number, "Map point values must be zero or greater.");
        return number;
    }

    private static double? NumericFeatureValue(GeoJsonFeature feature, string? property) {
        if (property == null || !feature.Properties.TryGetValue(property, out var value) || value.IsNull) return null;
        var number = value.AsNumber("point value property '" + property + "'");
        ChartGuards.Finite(number, nameof(property));
        return number;
    }

    private static bool IsGeometry(GeoJsonFeature feature, string type) => string.Equals(feature.GeometryType, type, StringComparison.OrdinalIgnoreCase);
    private static string PartLabel(string label, int part, bool multiple) => multiple ? label + " [" + (part + 1).ToString(CultureInfo.InvariantCulture) + "]" : label;
    private static void UnsupportedObservationGeometry(GeoJsonFeature feature, ChartMapGeoJsonOptions options) {
        if (!options.SkipUnsupportedGeometries) throw new ArgumentException("Unsupported GeoJSON geometry type: " + (feature.GeometryType ?? "missing") + ".");
    }
}
