using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a connector line between two longitude/latitude positions on a map chart.
/// </summary>
public readonly struct ChartMapConnector {
    private readonly IReadOnlyList<ChartMapPoint>? _routePoints;
    /// <summary>
    /// Gets the connector label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the source longitude in degrees.
    /// </summary>
    public readonly double FromLongitude;

    /// <summary>
    /// Gets the source latitude in degrees.
    /// </summary>
    public readonly double FromLatitude;

    /// <summary>
    /// Gets the target longitude in degrees.
    /// </summary>
    public readonly double ToLongitude;

    /// <summary>
    /// Gets the target latitude in degrees.
    /// </summary>
    public readonly double ToLatitude;

    /// <summary>
    /// Gets the optional connector color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>Gets optional immutable feature metadata, independent of series observation IDs.</summary>
    public ChartMapFeature? Feature { get; }

    /// <summary>Gets optional additional endpoint clearance in logical scene units. Null retains the legacy route geometry.</summary>
    /// <remarks>An explicit value crops the original native curve against the actual endpoint marks in all composed DottedMap datasets.
    /// The route's source coordinates remain unchanged. Overlapping endpoint disks can leave no painted route.</remarks>
    public double? EndpointClearance { get; }

    /// <summary>
    /// Gets the optional ordered route points. When empty, renderers use the source and target coordinates as a simple connector.
    /// </summary>
    public IReadOnlyList<ChartMapPoint> RoutePoints => _routePoints ?? Array.Empty<ChartMapPoint>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapConnector"/> struct.
    /// </summary>
    public ChartMapConnector(string label, double fromLongitude, double fromLatitude, double toLongitude, double toLatitude, ChartColor? color = null) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Map connector labels must not be empty.", nameof(label));
        ValidateCoordinate(fromLongitude, fromLatitude, nameof(fromLongitude), nameof(fromLatitude));
        ValidateCoordinate(toLongitude, toLatitude, nameof(toLongitude), nameof(toLatitude));
        Label = label.Trim();
        FromLongitude = fromLongitude;
        FromLatitude = fromLatitude;
        ToLongitude = toLongitude;
        ToLatitude = toLatitude;
        Color = color;
        Feature = null; EndpointClearance = null;
        _routePoints = Array.Empty<ChartMapPoint>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapConnector"/> struct with ordered longitude/latitude route points.
    /// </summary>
    public ChartMapConnector(string label, IEnumerable<ChartMapPoint> routePoints, ChartColor? color = null) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Map connector labels must not be empty.", nameof(label));
        if (routePoints == null) throw new ArgumentNullException(nameof(routePoints));
        var points = routePoints.Select(point => point.ValidatedCopy()).ToArray();
        if (points.Length < 2) throw new ArgumentException("Map route connectors require at least two route points.", nameof(routePoints));
        Label = label.Trim();
        FromLongitude = points[0].Longitude;
        FromLatitude = points[0].Latitude;
        ToLongitude = points[points.Length - 1].Longitude;
        ToLatitude = points[points.Length - 1].Latitude;
        Color = color;
        Feature = null; EndpointClearance = null;
        _routePoints = Array.AsReadOnly(points);
    }

    private ChartMapConnector(ChartMapConnector source, ChartMapFeature? feature, double? endpointClearance) {
        Label = source.Label; FromLongitude = source.FromLongitude; FromLatitude = source.FromLatitude;
        ToLongitude = source.ToLongitude; ToLatitude = source.ToLatitude; Color = source.Color;
        _routePoints = source._routePoints; Feature = feature; EndpointClearance = endpointClearance;
    }

    /// <summary>Returns the same route and paint with replacement immutable feature metadata.</summary>
    /// <param name="feature">The snapshot, or null to clear it. No series observation ID is assigned.</param>
    public ChartMapConnector WithFeature(ChartMapFeature? feature) => new(this, feature, EndpointClearance);

    /// <summary>Returns the same source route with opt-in endpoint-disk clipping of its native line and halo.</summary>
    /// <param name="gap">Nonnegative finite additional gap in logical units, or null to restore legacy geometry.</param>
    /// <returns>A detached route retaining its complete source/feature facts.</returns>
    public ChartMapConnector WithEndpointClearance(double? gap) {
        if (gap.HasValue) {
            ChartGuards.Finite(gap.Value, nameof(gap));
            if (gap.Value < 0) throw new ArgumentOutOfRangeException(nameof(gap));
        }
        return new ChartMapConnector(this, Feature, gap);
    }

    internal ChartMapConnector ValidatedCopy() => (RoutePoints.Count > 0
        ? new ChartMapConnector(Label, RoutePoints, Color)
        : new ChartMapConnector(Label, FromLongitude, FromLatitude, ToLongitude, ToLatitude, Color)).WithFeature(Feature).WithEndpointClearance(EndpointClearance);

    private static void ValidateCoordinate(double longitude, double latitude, string longitudeName, string latitudeName) {
        ChartGuards.Finite(longitude, longitudeName);
        ChartGuards.Finite(latitude, latitudeName);
        if (longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(longitudeName, longitude, "Longitude must be between -180 and 180 degrees.");
        if (latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(latitudeName, latitude, "Latitude must be between -90 and 90 degrees.");
    }
}
