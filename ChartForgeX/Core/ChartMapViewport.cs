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
        MaximumLatitude = maximumLatitude;
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
