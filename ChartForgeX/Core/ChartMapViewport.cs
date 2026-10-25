using System;

namespace ChartForgeX.Core;

/// <summary>
/// Defines the longitude and latitude window rendered by a dotted map.
/// </summary>
public readonly struct ChartMapViewport {
    /// <summary>
    /// Gets the viewport label.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Gets the minimum longitude in degrees.
    /// </summary>
    public readonly double MinimumLongitude;

    /// <summary>
    /// Gets the maximum longitude in degrees.
    /// </summary>
    public readonly double MaximumLongitude;

    /// <summary>
    /// Gets the minimum latitude in degrees.
    /// </summary>
    public readonly double MinimumLatitude;

    /// <summary>
    /// Gets the maximum latitude in degrees.
    /// </summary>
    public readonly double MaximumLatitude;

    /// <summary>Gets whether raw longitude bounds cross the antimeridian. Ordered constructor windows remain false.</summary>
    public bool CrossesAntimeridian => MaximumLongitude < MinimumLongitude;
    /// <summary>Gets the positive increasing unwrapped longitude span in degrees. Raw west/east values are unchanged.</summary>
    public double LongitudeSpan => CrossesAntimeridian ? (180 - MinimumLongitude) + (MaximumLongitude + 180) : MaximumLongitude - MinimumLongitude;
    /// <summary>Gets the explicit existing projection of a seam-crossing point/route window; null retains ordinary composition projection selection.</summary>
    public ChartMapGeoJsonProjection? GeographicProjection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapViewport"/> struct.
    /// </summary>
    public ChartMapViewport(string name, double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Map viewport names must not be empty.", nameof(name));
        ChartGuards.Finite(minimumLongitude, nameof(minimumLongitude));
        ChartGuards.Finite(maximumLongitude, nameof(maximumLongitude));
        ChartGuards.Finite(minimumLatitude, nameof(minimumLatitude));
        ChartGuards.Finite(maximumLatitude, nameof(maximumLatitude));
        if (minimumLongitude < -180 || maximumLongitude > 180 || maximumLongitude <= minimumLongitude) throw new ArgumentOutOfRangeException(nameof(maximumLongitude), maximumLongitude, "Map viewport longitudes must be ordered within -180 to 180 degrees.");
        if (minimumLatitude < -90 || maximumLatitude > 90 || maximumLatitude <= minimumLatitude) throw new ArgumentOutOfRangeException(nameof(maximumLatitude), maximumLatitude, "Map viewport latitudes must be ordered within -90 to 90 degrees.");
        Name = name.Trim();
        MinimumLongitude = minimumLongitude;
        MaximumLongitude = maximumLongitude;
        MinimumLatitude = minimumLatitude;
        MaximumLatitude = maximumLatitude; GeographicProjection = null;
    }

    private ChartMapViewport(ChartMapViewport latitudeWindow, double west, double east, ChartMapGeoJsonProjection projection) {
        Name = latitudeWindow.Name; MinimumLatitude = latitudeWindow.MinimumLatitude; MaximumLatitude = latitudeWindow.MaximumLatitude;
        MinimumLongitude = west; MaximumLongitude = east; GeographicProjection = projection;
    }

    /// <summary>Creates an explicit eastward point/route window crossing +180/-180 without rewriting authored source coordinates.</summary>
    /// <param name="name">The nonblank viewport name.</param>
    /// <param name="westLongitude">Raw western boundary in [-180,180], strictly greater than the raw eastern boundary.</param>
    /// <param name="eastLongitude">Raw eastern boundary in [-180,180]. The increasing span across the seam must be positive and less than one turn.</param>
    /// <param name="minimumLatitude">Inclusive southern raw latitude in [-90,90].</param>
    /// <param name="maximumLatitude">Inclusive northern raw latitude, greater than the southern boundary.</param>
    /// <param name="projection">One of the existing geographic projections. Web Mercator keeps its existing polar latitude clamp.</param>
    /// <returns>An immutable viewport retaining both raw longitude bounds.</returns>
    /// <remarks>Points at equivalent +180/-180 bearings remain separate source observations. Consecutive route waypoints use shortest signed
    /// longitude deltas; exact half-turn ties preserve the authored sign. Every intersecting periodic route fragment is clipped to the fitted rectangular window,
    /// while its source coordinates and feature identity are retained once. Endpoint clearance follows the matching periodic point or cluster marks.
    /// Region/context geometries without a matching declared seam and explicit projected RegionMapBounds are unsupported in this first contract.</remarks>
    public static ChartMapViewport AcrossAntimeridian(string name, double westLongitude, double eastLongitude,
        double minimumLatitude, double maximumLatitude, ChartMapGeoJsonProjection projection = ChartMapGeoJsonProjection.Equirectangular) {
        ChartGuards.Finite(westLongitude, nameof(westLongitude)); ChartGuards.Finite(eastLongitude, nameof(eastLongitude));
        if (westLongitude < -180 || westLongitude > 180) throw new ArgumentOutOfRangeException(nameof(westLongitude));
        if (eastLongitude < -180 || eastLongitude > 180 || eastLongitude >= westLongitude)
            throw new ArgumentOutOfRangeException(nameof(eastLongitude), "A seam-crossing viewport needs a raw eastern bound below its western bound.");
        var span = (180 - westLongitude) + (eastLongitude + 180);
        if (span <= 0 || span >= 360) throw new ArgumentOutOfRangeException(nameof(eastLongitude), "A seam-crossing viewport needs a positive longitude span below one full turn.");
        ChartGeographicProjection.Validate(projection);
        var latitudeWindow = new ChartMapViewport(name, -180, 180, minimumLatitude, maximumLatitude);
        return new ChartMapViewport(latitudeWindow, westLongitude, eastLongitude, projection);
    }

    /// <summary>
    /// Gets the default world map viewport.
    /// </summary>
    public static ChartMapViewport World() => new("World", -180, 180, -58, 84);

    /// <summary>
    /// Gets a Europe-focused viewport.
    /// </summary>
    public static ChartMapViewport Europe() => new("Europe", -11, 35, 36, 72);

    /// <summary>
    /// Gets a North America-focused viewport.
    /// </summary>
    public static ChartMapViewport NorthAmerica() => new("North America", -170, -50, 5, 72);

    /// <summary>
    /// Gets a South America-focused viewport.
    /// </summary>
    public static ChartMapViewport SouthAmerica() => new("South America", -86, -32, -56, 14);

    /// <summary>
    /// Gets an Africa-focused viewport.
    /// </summary>
    public static ChartMapViewport Africa() => new("Africa", -20, 55, -36, 38);

    /// <summary>
    /// Gets an Asia-focused viewport.
    /// </summary>
    public static ChartMapViewport Asia() => new("Asia", 44, 150, -10, 72);

    /// <summary>
    /// Gets an Oceania-focused viewport.
    /// </summary>
    public static ChartMapViewport Oceania() => new("Oceania", 108, 180, -48, 2);

    /// <summary>
    /// Gets a Poland-focused viewport.
    /// </summary>
    public static ChartMapViewport Poland() => new("Poland", 13.5, 24.5, 48.5, 55.2);
}
